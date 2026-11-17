using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.MissingPrimaryImages.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MissingPrimaryImages.Tasks;

/// <summary>
/// Scheduled task: Dashboard → Scheduled Tasks → Library → "Fetch Missing Primary Images".
/// </summary>
public class FetchMissingPrimaryImagesTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IFileSystem _fileSystem;
    private readonly IApplicationPaths _appPaths;
    private readonly ILogger<FetchMissingPrimaryImagesTask> _logger;

    public FetchMissingPrimaryImagesTask(
        ILibraryManager libraryManager,
        IFileSystem fileSystem,
        IApplicationPaths appPaths,
        ILogger<FetchMissingPrimaryImagesTask> logger)
    {
        _libraryManager = libraryManager;
        _fileSystem = fileSystem;
        _appPaths = appPaths;
        _logger = logger;
    }

    public string Name => "Fetch Missing Primary Images";

    public string Key => "MissingPrimaryImagesFetch";

    public string Description =>
        "Finds items with no Primary image and tries to download one from your enabled image providers.";

    public string Category => "Library";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // Daily at 4 AM. Change it in the dashboard if you like.
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.DailyTrigger,
            TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
        };
    }

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var kinds = GetItemKinds(config);
        if (kinds.Length == 0)
        {
            _logger.LogInformation("No item types are enabled in the plugin settings; nothing to do");
            progress.Report(100);
            return;
        }

        var store = new AttemptStore(
            Path.Combine(_appPaths.DataPath, "missing-primary-images", "attempts.json"),
            _logger);
        store.Load();

        var allItems = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = kinds,
            Recursive = true,
            IsVirtualItem = false // skip "missing episode" placeholders
        });

        store.Prune(allItems.Select(i => i.Id).ToHashSet());

        var candidates = allItems
            .Where(i => !i.HasImage(ImageType.Primary, 0))
            .Where(i => !(config.SkipLockedItems && i.IsLocked))
            .ToList();

        var due = candidates.Where(i => !store.ShouldSkip(i.Id, config.RetryAfterDays)).ToList();
        if (config.MaxItemsPerRun > 0 && due.Count > config.MaxItemsPerRun)
        {
            due = due.Take(config.MaxItemsPerRun).ToList();
        }

        _logger.LogInformation(
            "Checked {Total} items: {Missing} have no Primary image, {Due} will be processed this run{DryRun}",
            allItems.Count,
            candidates.Count,
            due.Count,
            config.DryRun ? " (dry run)" : string.Empty);

        int fixedCount = 0, stillMissing = 0, errors = 0;

        for (var n = 0; n < due.Count; n++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = due[n];
            var label = Describe(item);

            if (config.DryRun)
            {
                _logger.LogInformation("[dry run] Would fetch Primary image for {Item}", label);
            }
            else
            {
                try
                {
                    await item.RefreshMetadata(BuildRefreshOptions(config), cancellationToken).ConfigureAwait(false);

                    // Re-read the item: the refresh saves a new copy with the downloaded image.
                    var refreshed = _libraryManager.GetItemById(item.Id) ?? item;
                    if (refreshed.HasImage(ImageType.Primary, 0))
                    {
                        fixedCount++;
                        store.Clear(item.Id);
                        _logger.LogInformation("Got Primary image for {Item}", label);
                    }
                    else
                    {
                        stillMissing++;
                        store.RecordFailure(item.Id);
                        _logger.LogInformation("No Primary image found for {Item}", label);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    errors++;
                    store.RecordFailure(item.Id);
                    _logger.LogWarning(ex, "Error refreshing images for {Item}", label);
                }

                if (config.DelayBetweenItemsMs > 0)
                {
                    await Task.Delay(config.DelayBetweenItemsMs, cancellationToken).ConfigureAwait(false);
                }
            }

            progress.Report(100.0 * (n + 1) / due.Count);
        }

        if (!config.DryRun)
        {
            store.Save();
        }

        _logger.LogInformation(
            "Missing Primary Images finished: {Fixed} fixed, {Still} still missing, {Errors} errors, {Deferred} deferred to a later run",
            fixedCount,
            stillMissing,
            errors,
            candidates.Count - due.Count);

        progress.Report(100);
    }

    private MetadataRefreshOptions BuildRefreshOptions(PluginConfiguration config)
    {
        return new MetadataRefreshOptions(new DirectoryService(_fileSystem))
        {
            // Same as the web UI's "Search for missing metadata" + images:
            // fill in what's missing, never overwrite what's already there.
            MetadataRefreshMode = config.AlsoFillMissingMetadata
                ? MetadataRefreshMode.FullRefresh
                : MetadataRefreshMode.ValidationOnly,
            ImageRefreshMode = MetadataRefreshMode.FullRefresh,
            ReplaceAllMetadata = false,
            ReplaceAllImages = false,
            IsAutomated = true
        };
    }

    private static BaseItemKind[] GetItemKinds(PluginConfiguration c)
    {
        var kinds = new List<BaseItemKind>();
        if (c.IncludeMovies) { kinds.Add(BaseItemKind.Movie); }
        if (c.IncludeSeries) { kinds.Add(BaseItemKind.Series); }
        if (c.IncludeSeasons) { kinds.Add(BaseItemKind.Season); }
        if (c.IncludeEpisodes) { kinds.Add(BaseItemKind.Episode); }
        if (c.IncludeCollections) { kinds.Add(BaseItemKind.BoxSet); }
        if (c.IncludeMusicAlbums) { kinds.Add(BaseItemKind.MusicAlbum); }
        if (c.IncludeMusicArtists) { kinds.Add(BaseItemKind.MusicArtist); }
        if (c.IncludeMusicVideos) { kinds.Add(BaseItemKind.MusicVideo); }
        if (c.IncludeBooks) { kinds.Add(BaseItemKind.Book); kinds.Add(BaseItemKind.AudioBook); }
        if (c.IncludePeople) { kinds.Add(BaseItemKind.Person); }
        return kinds.ToArray();
    }

    private static string Describe(BaseItem item)
    {
        return item switch
        {
            MediaBrowser.Controller.Entities.TV.Episode e =>
                $"{e.SeriesName} S{e.ParentIndexNumber:00}E{e.IndexNumber:00} \"{e.Name}\"",
            MediaBrowser.Controller.Entities.TV.Season s => $"{s.SeriesName} – {s.Name}",
            _ => $"{item.GetType().Name} \"{item.Name}\""
                + (item.ProductionYear is int y && !item.Name.Contains(y.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal) ? $" ({y})" : string.Empty)
        };
    }
}
