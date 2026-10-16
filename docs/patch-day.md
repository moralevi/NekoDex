# Patch day checklist

Dalamud and FFXIVClientStructs change with game patches. Run through this after every game patch and every Dalamud API level bump.

1. Launch the game through XIVLauncher so the new Dalamud and its dev assemblies download. Check `%APPDATA%\XIVLauncher\addon\Hooks\dev\Dalamud.runtimeconfig.json` for the target framework.
2. Compare the `Dalamud.NET.Sdk/x.y.z` version in `src/NekoDex/NekoDex.csproj` against the current [SamplePlugin](https://github.com/goatcorp/SamplePlugin). Bump if needed.
3. `dotnet build NekoDex.slnx`. Fix compile errors from renamed or removed APIs first.
4. Load the dev build. Reload it five times and read `/xllog` for errors.
5. Re-run the gate checks of every shipped phase (counts against the in-game Mount Guide, spot checks, filters).
6. Regenerate the bundled dataset if new collectables were added (Phase 4 onward) and bump the data version.
7. Tag a release and update `repo.json` (Phase 8 onward).
