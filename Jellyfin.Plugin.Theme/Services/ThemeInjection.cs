using System;
using System.Globalization;

namespace Jellyfin.Plugin.Theme.Services;

/// <summary>
/// The pure string half of the injection: which requests are the web app shell, what
/// gets inserted, and where. Kept free of ASP.NET types so it can be unit tested.
/// </summary>
internal static class ThemeInjection
{
    /// <summary>
    /// Opening marker of the injected block. Presence of this string means the document
    /// has already been themed, either by this middleware or by an older on-disk write.
    /// </summary>
    internal const string StartMarker = "<!-- jellyfin-theme:start -->";

    private const string EndMarker = "<!-- jellyfin-theme:end -->";

    /// <summary>
    /// Matches the web app shell however it is requested: bare "/web", "/web/" (SPA serve),
    /// and explicit "/web/index.html". EndsWith keeps this correct when Jellyfin is hosted
    /// under a base-url prefix such as "/jellyfin/web/".
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <returns><c>true</c> when the request is for the web app shell.</returns>
    internal static bool IsIndexRequest(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return path.EndsWith("/web/index.html", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/web/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/web", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds the block inserted before the closing body tag.
    /// </summary>
    /// <param name="pathBase">The base-url prefix the server is hosted under, or an empty string.</param>
    /// <param name="version">Asset version, appended as a cache buster.</param>
    /// <returns>The HTML block, marker comments included.</returns>
    internal static string BuildBlock(string pathBase, string version)
    {
        var root = pathBase.TrimEnd('/');

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}\n<link rel=\"stylesheet\" href=\"{1}/Theme/theme.css?v={2}\">\n<script src=\"{1}/Theme/theme.js?v={2}\" defer></script>\n{3}",
            StartMarker,
            root,
            Uri.EscapeDataString(version),
            EndMarker);
    }

    /// <summary>
    /// Inserts <paramref name="block"/> before the last closing body tag.
    /// </summary>
    /// <param name="html">The document to rewrite.</param>
    /// <param name="block">The block to insert.</param>
    /// <param name="result">The rewritten document, or <paramref name="html"/> unchanged.</param>
    /// <returns><c>true</c> when the document was rewritten.</returns>
    internal static bool TryInject(string html, string block, out string result)
    {
        result = html;

        if (html.Contains(StartMarker, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var bodyClose = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        if (bodyClose < 0)
        {
            return false;
        }

        result = string.Concat(html.AsSpan(0, bodyClose), block, "\n", html.AsSpan(bodyClose));
        return true;
    }
}
