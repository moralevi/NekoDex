# Patch day checklist

Dalamud and FFXIVClientStructs change with game patches. Run through this after every game patch and every Dalamud API level bump.

1. Launch the game through XIVLauncher so the new Dalamud and its dev assemblies download. Check `%APPDATA%\XIVLauncher\addon\Hooks\dev\Dalamud.runtimeconfig.json` for the target framework.
2. Compare the `Dalamud.NET.Sdk/x.y.z` version in `src/NekoDex/NekoDex.csproj` against the current [SamplePlugin](https://github.com/goatcorp/SamplePlugin). Bump if needed.
3. `dotnet build NekoDex.slnx`. Fix compile errors from renamed or removed APIs first.
4. `dotnet run --project tools/DataGen -- --sheets-only`: every module's sheet rule against the new game files, no network. A FAILED line means a sheet column moved.
5. Load the dev build. Reload it five times and read `/xllog` for errors.
6. Re-run the gate checks of every shipped phase (counts against the in-game guides, spot checks, filters).
7. Regenerate the bundled dataset (`dotnet run --project tools/DataGen`) so new collectables get their text.
8. Tag a release; the Release workflow updates `repo.json`.
