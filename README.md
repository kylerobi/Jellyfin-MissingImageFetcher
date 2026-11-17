# Missing Primary Images — Jellyfin plugin

Scans your libraries for items that have **no Primary image** (poster / cover / thumbnail)
and asks Jellyfin's own image providers (TMDb, TVDb, MusicBrainz/AudioDB, embedded artwork,
screen grabber, …) to fetch one. It never replaces images or metadata you already have.

## Install (recommended: from the plugin repository)

1. Dashboard → **Plugins** → **Repositories** (on older versions: Plugins → Repositories tab) → **+**
2. Name: `Missing Primary Images`
   Repository URL:
   ```
   https://raw.githubusercontent.com/kylerobi/jellyfin-plugin-missing-primary-images/main/manifest.json
   ```
3. Go to **Catalog**, find **Missing Primary Images** (under Metadata) and install it.
4. Restart Jellyfin.

Jellyfin picks the right build automatically:

| Plugin version | Jellyfin |
|---|---|
| 2.x | 12.4 and newer |
| 1.x | 10.11.x |

Updates published to the repository show up in the dashboard like any other plugin.

### Manual install

Create a folder `MissingPrimaryImages` inside your Jellyfin `plugins` folder, unzip the matching zip
from `dist/` into it (`missing-primary-images_2.x` for 12.4+, `_1.x` for 10.11), and restart Jellyfin.

- Docker: `<your /config volume>/plugins/`
- Linux package: `/var/lib/jellyfin/plugins/`
- Windows: `%LOCALAPPDATA%\jellyfin\plugins\` (or `C:\ProgramData\Jellyfin\Server\plugins\`)

Then Dashboard → Plugins → **Missing Primary Images** to adjust settings.

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

## Build / release

Requires the .NET 10 SDK (it also builds the .NET 9 target) and `zip`.

```bash
./build.sh
```

This builds both zips into `dist/` and rewrites `manifest.json` with their checksums.
For a new release, bump the versions and changelog at the top of `build.sh`, run it, and push.
