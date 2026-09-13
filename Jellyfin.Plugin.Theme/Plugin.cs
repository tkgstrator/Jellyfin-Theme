using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.Theme.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Theme;

/// <summary>
/// A Jellyfin 12.0 theme: Netflix-like for video, Apple Music-like for music.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// The plugin name. Jellyfin uses it verbatim as the plugin directory name, so it must
    /// not contain path separators or other characters that are invalid in file names.
    /// </summary>
    public const string PluginName = "Netflix / Apple Music Theme";

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => PluginName;

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("4a6cfff8-48b8-48eb-ba42-fc4d40a4b0bc");

    /// <inheritdoc />
    public override string Description
        => "Restyles the Jellyfin 12.0 web client: Netflix-like browsing for movies and shows, Apple Music-like browsing for music.";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Configuration.configPage.html",
                    GetType().Namespace)
            }
        ];
    }
}
