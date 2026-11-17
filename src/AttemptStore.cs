using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MissingPrimaryImages;

/// <summary>
/// Remembers when we last tried (and failed) to find a Primary image for an item,
/// so items that genuinely have no artwork anywhere aren't re-queried every run.
/// Stored as a small JSON file of item IDs and timestamps; nothing else.
/// </summary>
internal sealed class AttemptStore
{
    private readonly string _path;
    private readonly ILogger _logger;
    private Dictionary<Guid, DateTime> _attempts = new();

    public AttemptStore(string path, ILogger logger)
    {
        _path = path;
        _logger = logger;
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                _attempts = JsonSerializer.Deserialize<Dictionary<Guid, DateTime>>(File.ReadAllText(_path))
                    ?? new Dictionary<Guid, DateTime>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read {Path}; starting with an empty attempt history", _path);
            _attempts = new Dictionary<Guid, DateTime>();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(_attempts));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write {Path}", _path);
        }
    }

    public bool ShouldSkip(Guid id, int retryAfterDays)
    {
        return retryAfterDays > 0
            && _attempts.TryGetValue(id, out var last)
            && DateTime.UtcNow - last < TimeSpan.FromDays(retryAfterDays);
    }

    public void RecordFailure(Guid id) => _attempts[id] = DateTime.UtcNow;

    public void Clear(Guid id) => _attempts.Remove(id);

    /// <summary>Drop entries for items that no longer exist.</summary>
    public void Prune(ISet<Guid> existing)
    {
        var stale = new List<Guid>();
        foreach (var id in _attempts.Keys)
        {
            if (!existing.Contains(id))
            {
                stale.Add(id);
            }
        }

        foreach (var id in stale)
        {
            _attempts.Remove(id);
        }
    }
}
