using System;
using System.IO;
using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Theme.Api;

/// <summary>
/// Serves the theme's embedded stylesheet and script.
/// </summary>
/// <remarks>
/// The middleware only inserts link and script tags into index.html; the assets
/// themselves are served from here so they stay inside the plugin assembly.
/// Anonymous because the browser fetches them before the user is authenticated.
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route("Theme")]
public class ThemeAssetController : ControllerBase
{
    /// <summary>
    /// Gets the theme stylesheet.
    /// </summary>
    /// <response code="200">Stylesheet returned.</response>
    /// <returns>The stylesheet.</returns>
    [HttpGet("theme.css")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetStylesheet() => Asset("theme.css", "text/css");

    /// <summary>
    /// Gets the theme script.
    /// </summary>
    /// <response code="200">Script returned.</response>
    /// <returns>The script.</returns>
    [HttpGet("theme.js")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetScript() => Asset("theme.js", "text/javascript");

    private ActionResult Asset(string name, string contentType)
    {
        var assembly = typeof(Plugin).Assembly;
        var stream = assembly.GetManifestResourceStream($"{typeof(Plugin).Namespace}.Resources.{name}");

        if (stream is null)
        {
            // Only reachable if the resource was dropped from the csproj.
            return NotFound();
        }

        return File(stream, new ContentType(contentType).ToString());
    }
}
