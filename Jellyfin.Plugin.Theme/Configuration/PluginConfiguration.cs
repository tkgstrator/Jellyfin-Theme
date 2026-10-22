using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Theme.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the theme is injected into the web client.
    /// </summary>
    /// <remarks>
    /// A kill switch. The injection middleware rewrites index.html, which is the one
    /// response that takes the whole web UI down with it, so it has to be possible to
    /// turn off from the dashboard without uninstalling the plugin.
    /// </remarks>
    public bool EnableInjection { get; set; } = true;
}
