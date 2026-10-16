# Changelog

## 1.3.0 (in progress)

- "New in patch X" filter on every collection tab and a Dashboard section with per-collection counts that opens the tab filtered.
- Export and import your data (outfits, pins, checklist, auto item choices, dresser snapshots) as one JSON file from settings, with merge or replace; an outfits-only export is meant for sharing with friends.
- Garland Tools fills in unlock info for entries FFXIV Collect has none for, attributed per entry. Garland links now open the item page that grants the collectable; the old mount/minion links pointed at pages that do not exist.

## 1.2.0 (in progress)

- New tabs: Emotes, Bardings, Fashion Accessories, Facewear and Triple Triad, with unlock data for all of them.
- Outfits: fill from current gear or from a glamour plate (items and dyes), two dyes per slot with swatches, and the Dresser column says whether you own the piece in that dye.
- Translations: strings load from loc/<language>.json for Dalamud's UI language, English otherwise; a weekly workflow proposes dataset refreshes as pull requests; settings can forget a character's cached data.

## 1.1.0 (in progress)

- Glamour Dresser contents are remembered per character. Config moves to v2; a snapshot saved by 1.0 is handed to the first character that logs in.
- Obtainable / not obtainable filter from the unlock data (store-only, event and legacy rewards).
- Sortable table headers (name, patch, status, unlock type) and a missing-first toggle.
- Pin entries with the star: they sit at the top of their tab and in the mini window until unlocked.
- Dashboard shows per-collection progress.
- Automatic checklist items read from the game: duty roulettes, tribal allowances and Wondrous Tails, chosen in settings; they tick themselves and show in the mini window until done.

## 1.0.0 (unreleased)

First friends release.

- Mounts, Minions, Orchestrion and Armoire tabs: owned/missing status, progress, search and filters.
- In-game unlock info for mounts, minions and orchestrion rolls from a bundled dataset generated from FFXIV Collect, with web links to FFXIV Collect or Garland Tools.
- Outfit planner: named outfits slot by slot, showing where each piece is (Glamour Dresser, Armoire, bags) or that it is missing.
- Gear advisor: per-slot next upgrade by item level for the current job and level.
- Dashboard: reset countdowns (daily, weekly, Grand Company, Jumbo Cactpot, Fashion Report) and a checklist that clears itself at reset; optional mini window.
- Neko theme with a toggle back to the default Dalamud style.
