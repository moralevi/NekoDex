# NekoDex

Dalamud plugin for FFXIV that shows what the logged-in character owns and is missing, starting with mounts, and how to unlock the rest.

Information and UI only. No automation, no input injection, read-only access to game state.

Status: Phase 4 (mount list with in-game unlock info from a bundled dataset). See [the spec](dalamud-collection-tracker-spec.md) for the roadmap and phase gates.

## Layout

```
src/NekoDex.Core/        pure logic: tracker abstractions, filters, reset-time logic. No Dalamud dependency.
src/NekoDex/             the plugin: Plugin.cs, Configuration, Data (game readers, providers), UI (windows, theme)
tools/DataGen/           generates src/NekoDex/Data/unlocks.json from FFXIV Collect + local game data
tests/NekoDex.Core.Tests xUnit tests for Core
.github/workflows/       CI: build + test on push and PR
```

The Core project exists so logic can be tested without the game. Anything that touches Dalamud, Lumina, ClientStructs or ImGui lives in `src/NekoDex`.

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

## Commands

| Command | Effect |
|---|---|
| `/nekodex` | Toggle the main window |
| `/nekodex config` | Toggle the settings window |

## License

MIT. External data sources, their terms and attributions are listed in [DATA_SOURCES.md](DATA_SOURCES.md).
