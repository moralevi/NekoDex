# NekoDex

Dalamud plugin for FFXIV that shows what the logged-in character owns and is missing, starting with mounts, and how to unlock the rest.

Information and UI only. No automation, no input injection, read-only access to game state.

Status: 1.0.0 release candidate. See [SPEC.md](SPEC.md) for what it does and what comes next, and [CHANGELOG.md](CHANGELOG.md).

## Installing (friends build)

1. In game: `/xlsettings` → Experimental → Custom Plugin Repositories, add
   `https://raw.githubusercontent.com/moralevi/NekoDex/main/repo.json` and save.
2. `/xlplugins` → search "NekoDex" → Install.
3. `/nekodex` opens the window; the cog in its title bar opens settings.

The repository must be public for that URL and the release download to work.

## Screenshots

Screenshots of the Mounts tab, the Outfits tab and the Dashboard go here once taken in game (`docs/screenshots/`).

## Theme

The Neko theme (plum and salmon, rounded corners) is pushed and popped per window, so other plugins keep their own style. Turn it off in settings to use your Dalamud style unchanged; the change applies immediately.

## Layout

```
src/NekoDex.Core/        pure logic: tracker abstractions, filters, reset-time logic. No Dalamud dependency.
src/NekoDex/             the plugin: Plugin.cs, Configuration, Data (game readers, providers), UI (windows, theme)
tools/DataGen/           generates src/NekoDex/Data/unlocks.json from FFXIV Collect + local game data
tests/NekoDex.Core.Tests xUnit tests for Core
.github/workflows/       CI: build + test on push and PR
```

The Core project exists so logic can be tested without the game. Anything that touches Dalamud, Lumina, ClientStructs or ImGui lives in `src/NekoDex`.

## Releasing

1. Bump `<Version>` in `src/NekoDex/NekoDex.csproj` and add a section to `CHANGELOG.md`.
2. Commit to `main`, then tag and push: `git tag v1.0.0.0 && git push origin v1.0.0.0`.
3. The Release workflow builds, tests, attaches `latest.zip` to a GitHub release and updates `repo.json` on `main`. Dalamud picks the new version up from `repo.json`.

## Your data

Settings has Export and Import for pins, checklist and auto item choices. The file is JSON with a schema version. Import previews what the file contains and lets you merge (your copies win on conflicts) or replace the sections present in the file.

## Translations

Every user-visible string goes through `Strings.Get(key, english)`. `loc/en.json` lists them all in CheapLoc format and is regenerated with:

```bash
dotnet run --project tools/LocExport
```

To translate, copy `loc/en.json` to `loc/<language>.json` and replace the `message` values; `loc/sv.json` is a complete Swedish translation, `de`, `fr` and `ja` are empty templates. The plugin loads the file matching Dalamud's UI language, or the one chosen under Settings, Appearance, Language for languages Dalamud lacks; missing keys fall back to English. Check a translation for missing keys with:

```bash
dotnet run --project tools/LocExport -- --check loc/sv.json
```

## Dataset refresh in CI

The `Refresh unlock dataset` workflow runs weekly after the Tuesday reset and on demand. It regenerates `unlocks.json` from the FFXIV Collect API without game files (`--no-game`) and opens a pull request when something changed. Run DataGen locally before merging if a game patch landed, so the sheet validation runs.

## Building

Requirements: .NET 10 SDK and a Dalamud install (run the game through XIVLauncher once). The Dalamud.NET.Sdk resolves Dalamud assemblies from `%APPDATA%\XIVLauncher\addon\Hooks\dev`.

```bash
dotnet build NekoDex.slnx
```

```bash
dotnet test NekoDex.slnx
```

A Debug build is copied to `%APPDATA%\XIVLauncher\devPlugins\NekoDex\`, which Dalamud scans for dev plugins. Pass `-p:CopyToDevPlugins=false` to skip that.

Release builds via the solution produce the distributable package at `src/NekoDex/bin/x64/Release/NekoDex/latest.zip`.

## Dev loop

1. `dotnet build NekoDex.slnx`
2. In game: `/xlplugins` → Dev Tools → Installed Dev Plugins → NekoDex → load or reload
3. `/nekodex` toggles the main window, `/nekodex config` opens settings
4. `/xllog` shows plugin log output

## Unlock data

In-game "how to get it" text comes from `src/NekoDex/Data/unlocks.json`, embedded in the plugin and generated from FFXIV Collect. Regenerate it after a game patch (needs the game installed and network access):

```bash
dotnet run --project tools/DataGen
```

The generator validates every entry against the local Mount sheet and exits non-zero with warnings if the API and the game disagree or a new source category appears. Mounts newer than the dataset fall back to the web link.

## Collections

Tabs: Mounts, Minions, Orchestrion, Emotes, Bardings, Fashion Accessories, Facewear and Triple Triad, each with owned/missing status, search, filters, sorting, pins and in-game unlock info.

## Dashboard

The Dashboard tab shows countdowns to the fixed UTC resets (daily 15:00, weekly Tuesday 08:00, Grand Company 20:00, Jumbo Cactpot Saturday 19:00, Fashion Report judging Friday 08:00) with the time in your zone, plus a checklist of your own items. Daily and weekly items untick themselves at their reset. The mini window shows the same countdowns and open items in a small always-visible panel.

## Commands

| Command | Effect |
|---|---|
| `/nekodex` | Toggle the main window |
| `/nekodex config` | Toggle the settings window |
| `/nekodex mini` | Toggle the compact countdown/checklist window |

## License

MIT. External data sources, their terms and attributions are listed in [DATA_SOURCES.md](DATA_SOURCES.md).
