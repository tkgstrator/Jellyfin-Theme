using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Theme.Services;

/// <summary>
/// Adds the theme stylesheet and script to the web client's index.html at request time.
/// </summary>
/// <remarks>
/// The web root cannot be rewritten on disk: it is often read-only under Docker, and a
/// web client update would overwrite it anyway. Instead this registers ASP.NET Core
/// middleware that rewrites the response body as it is served. See docs/design.md.
/// </remarks>
public class ThemeInjectionStartupFilter : IStartupFilter
{
    private readonly ILogger<ThemeInjectionStartupFilter> _logger;
    private int _loggedOnce;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeInjectionStartupFilter"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
    public ThemeInjectionStartupFilter(ILogger<ThemeInjectionStartupFilter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            // Registered before next(app) so this runs outermost. Only then does stripping
            // Accept-Encoding below reliably yield an uncompressed body we can rewrite.
            app.Use(InvokeAsync);
            next(app);
        };
    }

    private async Task InvokeAsync(HttpContext context, Func<Task> next)
    {
        if (!ThemeInjection.IsIndexRequest(context.Request.Path.Value)
            || !HttpMethods.IsGet(context.Request.Method)
            || Plugin.Instance?.Configuration.EnableInjection != true)
        {
            // Anything but a GET of the shell passes straight through. Buffering a
            // response with no body (HEAD, OPTIONS) would report a bogus Content-Length.
            await next().ConfigureAwait(false);
            return;
        }

        // Normalize the request so the static handler returns a complete, plain-text 200:
        // no compression, and no 206 partial (which would otherwise pass through
        // un-injected with a total length that no longer matches).
        context.Request.Headers.Remove("Accept-Encoding");
        context.Request.Headers.Remove("Range");
        context.Request.Headers.Remove("If-Range");

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next().ConfigureAwait(false);
        }
        catch
        {
            // Not ours to swallow. The buffered bytes never reached the real stream, so
            // discarding them leaves the response unstarted and the host can still render
            // its error page. Flushing here would commit a truncated 200.
            context.Response.Body = originalBody;
            throw;
        }

        context.Response.Body = originalBody;
        buffer.Seek(0, SeekOrigin.Begin);

        var isHtml = context.Response.StatusCode == 200
            && context.Response.ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true;

        if (!isHtml)
        {
            // 304, redirects, static files — unchanged.
            await buffer.CopyToAsync(originalBody).ConfigureAwait(false);
            return;
        }

        string html;
        using (var reader = new StreamReader(buffer, Encoding.UTF8, true, 1024, leaveOpen: true))
        {
            html = await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        try
        {
            var block = ThemeInjection.BuildBlock(
                context.Request.PathBase.Value ?? string.Empty,
                typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "0");

            if (ThemeInjection.TryInject(html, block, out var themed))
            {
                html = themed;

                if (Interlocked.Exchange(ref _loggedOnce, 1) == 0)
                {
                    _logger.LogInformation("Theme injected into the web client via request-time middleware.");
                }
            }
        }
        catch (Exception ex)
        {
            // Losing index.html takes the whole UI down. Serve what we have.
            _logger.LogWarning(ex, "Theme injection failed; serving the original HTML.");
        }

        var bytes = Encoding.UTF8.GetBytes(html);
        context.Response.ContentType = "text/html;charset=utf-8";
        context.Response.ContentLength = bytes.Length;

        // The body changed, so the static handler's validators no longer describe it, and
        // range requests are not supported on the rewritten document.
        context.Response.Headers.Remove("ETag");
        context.Response.Headers.Remove("Last-Modified");
        context.Response.Headers.Remove("Accept-Ranges");

        await originalBody.WriteAsync(bytes).ConfigureAwait(false);
    }
}
