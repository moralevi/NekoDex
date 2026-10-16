# Data sources

Every external source NekoDex uses, with how it is used, its terms and the attribution required. Update this file whenever a source is added, removed or used differently.

## Game data (Lumina)

- **What:** Excel sheets read from the player's own game installation through Dalamud's `IDataManager` (Mount sheet: names, icons, order).
- **Use:** entry lists and icons. Nothing is redistributed; the plugin reads whatever client the user has installed.
- **Terms:** FINAL FANTASY XIV © SQUARE ENIX CO., LTD. Names and icons are displayed in the user's own client only.

## Character state (FFXIVClientStructs)

- **What:** `PlayerState.IsMountUnlocked` read from the running game's memory.
- **Use:** owned / missing status. Read-only; nothing is written.

## FFXIV Collect (https://ffxivcollect.com)

- **Use (Phase 3):** link-out only. The "open on web" button opens `https://ffxivcollect.com/mounts/{mount row id}` in the user's browser. Page ids match the game's Mount sheet row ids (verified 2026-10-16 for ids 1 and 18). No API calls are made by the plugin.
- **Candidate use (Phase 4):** the public JSON API (`https://ffxivcollect.com/api/mounts/{id}`) returns `sources` with `type` and `text` per mount. The about page describes it as a public API for developers; no written license or rate limit was found on the site. Before bundling that data, ask the maintainer (Discord link on the site) about redistribution and record the answer here.
- **Attribution:** credit "FFXIV Collect" next to any data derived from it.

## Garland Tools (https://www.garlandtools.org)

- **Use (Phase 3):** link-out only, `https://www.garlandtools.org/db/#mount/{mount row id}`. Ids match the game's Mount sheet row ids. No API calls.
- **Attribution:** none required for links.

## Hand-verified entries

`src/NekoDex/Data/KnownUnlocks.cs` holds a few unlock sources written by hand from in-game knowledge (Company Chocobo, Magitek Armor, Behemoth). They exist to prove the in-game text path and will be replaced by the bundled dataset.
