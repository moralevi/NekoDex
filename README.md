# NekoDex

Dalamud plugin for FFXIV that shows what the logged-in character owns and is missing, starting with mounts, and how to unlock the rest.

Information and UI only. No automation, no input injection, read-only access to game state.

Status: Phase 3 (mount list with link-out unlock info). See [the spec](dalamud-collection-tracker-spec.md) for the roadmap and phase gates.

## Layout

```
src/NekoDex.Core/        pure logic: tracker abstractions, filters, reset-time logic. No Dalamud dependency.
src/NekoDex/             the plugin: Plugin.cs, Configuration, Data (game readers, providers), UI (windows, theme)
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

## Commands

| Command | Effect |
|---|---|
| `/nekodex` | Toggle the main window |
| `/nekodex config` | Toggle the settings window |

## License

MIT. External data sources, their terms and attributions are listed in [DATA_SOURCES.md](DATA_SOURCES.md).
