# Changelog

## 1.0.0 (2026-10-16)

First release, for friends.

- Collections: Mounts, Minions, Orchestrion, Emotes, Bardings, Fashion Accessories, Facewear, Triple Triad and Hairstyles, with owned/missing status, progress, search, ownership and obtainable filters, "new in patch" filter, sortable columns and pins.
- A flag button in every detail pane copies a prefilled source report to the clipboard; settings has a manual "Check for newer data" that asks the repository once.
- Farm tab: missing mounts grouped by the duty that drops them, with difficulty, a Savage/Ultimate filter, pins, and a per-character clear counter fed by duty completions; pinned mounts in the mini window show their duty and count.
- Triple Triad: the detail pane lists the NPCs that drop a card with their zone, greys out the ones you have not unlocked and names the quest, and a button flags the NPC on the map; a "winnable from unlocked NPCs" filter; stars and type columns to sort by.
- Unlock info in game from a bundled dataset built from FFXIV Collect with Garland Tools filling gaps, attributed per entry, with links to FFXIV Collect or Garland item pages. The dataset refreshes weekly through a pull request.
- Dashboard: reset countdowns (daily, weekly, Grand Company, Jumbo Cactpot, Fashion Report) in your time zone, per-collection progress, new-this-patch rows, and a checklist whose daily and weekly items clear at reset, plus game-read auto items (duty roulettes, tribal allowances, Wondrous Tails).
- Mini window with countdowns, open to-dos and pinned entries; `/nekodex mini`, with lock (`/nekodex mini lock`), clickthrough and opacity settings.
- A toast when the daily or weekly reset passes while you play (can be turned off).
- Neko theme with a live toggle back to the default Dalamud style.
- Export and import of pins, checklist and auto item choices.
- Translations: English and Swedish, with a Language setting for languages Dalamud does not offer; German, French and Japanese templates.
- Removed during development by decision: an Armoire tab, an outfit planner and a gear advisor. Settings from them are ignored.
