using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.MissingPrimaryImages.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.MissingPrimaryImages;

/// <summary>
/// Finds library items that have no Primary image and asks Jellyfin's
/// image providers to fetch one.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "Missing Primary Images";

    public override Guid Id => Guid.Parse("8f3c2a71-5d4e-4b9a-9c61-2e7f0a4d6b15");

    public override string Description =>
        "Scans your libraries for items without a Primary image and automatically fetches one.";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Configuration.configPage.html",
                    GetType().Namespace)
            }
        };
    }
}
