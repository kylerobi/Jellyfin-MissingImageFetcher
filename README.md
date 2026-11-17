# Missing Primary Images — Jellyfin plugin

Scans your libraries for items that have **no Primary image** (poster / cover / thumbnail)
and asks Jellyfin's own image providers (TMDb, TVDb, MusicBrainz/AudioDB, embedded artwork,
screen grabber, …) to fetch one. It never replaces images or metadata you already have.

## Install

Use the zip that matches your server version (Dashboard → About shows it):

| Your Jellyfin | Use |
|---|---|
| 12.4 or newer | `dist/MissingPrimaryImages_1.0.0.0_jellyfin-12.4.zip` |
| 10.11.x | `dist/MissingPrimaryImages_1.0.0.0_jellyfin-10.11.zip` |

1. Unzip it into your Jellyfin `plugins` folder, so you end up with
   `plugins/MissingPrimaryImages_1.0.0.0/Jellyfin.Plugin.MissingPrimaryImages.dll` + `meta.json`.
   - Docker: `<your /config volume>/plugins/`
   - Linux package: `/var/lib/jellyfin/plugins/`
   - Windows: `%LOCALAPPDATA%\jellyfin\plugins\` (or `C:\ProgramData\Jellyfin\Server\plugins\`)
2. Restart Jellyfin.
3. Dashboard → Plugins → **Missing Primary Images** to adjust settings.

## Use

Dashboard → Scheduled Tasks → Library → **Fetch Missing Primary Images**.
It runs daily at 4 AM by default; press ▶ to run it now. Results go to the server log, e.g.

```
Checked 4210 items: 37 have no Primary image, 37 will be processed this run
Got Primary image for Breaking Bad S02E05 "Breakage"
No Primary image found for Movie "Home Video 2004"
Missing Primary Images finished: 31 fixed, 6 still missing, 0 errors, 0 deferred to a later run
```

Tip: turn on **Dry run** first to see what it would touch.

## Settings

- **Item types**: movies, series, seasons, episodes, collections, music albums/artists, music videos
  are on by default; books and people are off.
- **Also fill in missing metadata**: items with no TMDb/TVDb ID can't get artwork, so this also runs a
  "search for missing metadata" refresh (existing values are kept).
- **Retry after N days**: items where no image could be found anywhere aren't re-queried every night
  (default 7). The history is kept in `<data>/missing-primary-images/attempts.json` (item IDs + dates only).
- **Max items per run** (default 500) and **pause between items** (default 250 ms) keep the
  load on metadata providers reasonable.
- **Skip locked items** (default on).

Images come only from the providers enabled on each library (Library → Manage → Image fetchers),
so if a provider is disabled there, this plugin won't use it either.

## Build

Requires the .NET 10 SDK (it can also build the .NET 9 target).

```bash
./build.sh                # both 10.11 and 12.4 packages into dist/
./build.sh 12.4.0         # one version
```
