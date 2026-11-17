using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MissingPrimaryImages.Configuration;

/// <summary>
/// Settings for the plugin, editable from Dashboard → Plugins → Missing Primary Images.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    // Which kinds of items to check.
    public bool IncludeMovies { get; set; } = true;

    public bool IncludeSeries { get; set; } = true;

    public bool IncludeSeasons { get; set; } = true;

    public bool IncludeEpisodes { get; set; } = true;

    public bool IncludeCollections { get; set; } = true;

    public bool IncludeMusicAlbums { get; set; } = true;

    public bool IncludeMusicArtists { get; set; } = true;

    public bool IncludeMusicVideos { get; set; } = true;

    public bool IncludeBooks { get; set; } = false;

    // People are off by default: libraries often have tens of thousands of them.
    public bool IncludePeople { get; set; } = false;

    /// <summary>
    /// Also fill in missing metadata (provider IDs etc.) during the refresh.
    /// Existing metadata is never overwritten. This helps items that have no
    /// TMDb/TVDb ID yet, because image providers need one to find artwork.
    /// </summary>
    public bool AlsoFillMissingMetadata { get; set; } = true;

    /// <summary>Skip items whose metadata is locked.</summary>
    public bool SkipLockedItems { get; set; } = true;

    /// <summary>
    /// After an unsuccessful attempt, wait this many days before trying the
    /// same item again (0 = retry on every run).
    /// </summary>
    public int RetryAfterDays { get; set; } = 7;

    /// <summary>Maximum items to process per run (0 = unlimited).</summary>
    public int MaxItemsPerRun { get; set; } = 500;

    /// <summary>Pause between items, to go easy on remote metadata providers.</summary>
    public int DelayBetweenItemsMs { get; set; } = 250;

    /// <summary>Only log what would be refreshed; change nothing.</summary>
    public bool DryRun { get; set; } = false;
}
