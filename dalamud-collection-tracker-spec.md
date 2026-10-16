# FFXIV Dalamud Plugin: Collection Tracker (working name: NekoDex)

Status: Phases 0-4 gates passed 2026-10-16. Phase 5 built, in-game gate pending. Target: friends first, public custom repo later.
Stack: C#/.NET, Dalamud.NET.Sdk, ImGui (via Dalamud's ImGui bindings), Lumina, FFXIVClientStructs.
Method: phased, each phase has gate criteria. Do not start a phase until the previous gate passes.

---

## 0. Goals and non-goals

**Goals**
- Show what the logged-in character owns and is missing, starting with mounts.
- For anything missing, show how to unlock it.
- Reuse one tracker core for later collections (minions, glamour, orchestrion, etc.).
- Daily/weekly timers and a checklist.
- Themed UI (Neko style) with a toggle back to plain Dalamud/ImGui styling.

**Non-goals (hard rules)**
- No automation of gameplay, no input injection, no botting. Information and UI only.
- No cross-character tracking in v1 (only the character currently logged in).
- No server component in v1.

**Decisions already made**
| Topic | Decision |
|---|---|
| Audience | Friends first, public custom repo later |
| First collection | Mounts |
| Unlock info v1 | Link out (Garland Tools / wiki / FFXIV Collect) is acceptable |
| Unlock info later | Show full "how to get it" inside the game window |
| Characters | Only the logged-in character |
| Glamour | Both owned-item tracking and outfit planning |
| UI | Themed, with a toggle to default styling |

**Open decisions (resolve before the phase that needs them)**
- Gear advisor scope: simple "next upgrade by item level" vs per-job best-in-slot. Needed before Phase 7.
- Final plugin name and Neko branding. Needed before Phase 8.
- Whether any existing plugin does part of this badly (informs scope). Optional.

---

## Phase 0: Dalamud dev environment

Steps
1. Install the .NET SDK version the current Dalamud API level requires (check Dalamud's docs / SamplePlugin for the target framework; do not assume).
2. Run the game through XIVLauncher at least once so Dalamud and its dev assemblies are downloaded.
3. Clone the official `SamplePlugin` repo as a reference and build it unmodified.
4. In game: `/xlsettings` → Experimental → add your build output folder under Dev Plugin Locations (or use the `devPlugins` folder under the XIVLauncher config directory, which Dalamud scans).
5. Open `/xlplugins` → Dev Tools → Installed Dev Plugins and confirm the sample plugin loads and its window opens.
6. Learn the loop: rebuild, reload from the dev plugin list, read logs with `/xllog`.

Gate
- [x] SamplePlugin builds and loads from a dev location.
- [x] Rebuild-and-reload works without restarting the game.
- [x] `/xllog` shows plugin log output.

---

## Phase 1: Skeleton

Scope
- New repo (github.com/moralevi/...) from the SamplePlugin structure, using Dalamud.NET.Sdk.
- Manifest (`.json`) with name, author, punchline, description, repo URL placeholders.
- Slash command (e.g. `/nekodex`) toggling a main window.
- Config window and persisted `Configuration` class (versioned, with a migration hook).
- Services wired through Dalamud's injected services (`IPluginLog`, `IDataManager`, `IClientState`, `IObjectTable`, etc.), no static service access scattered around.
- Clean architecture: `Core` (tracker logic, no ImGui), `Data` (providers), `UI` (windows), so logic is testable.

Gate
- [x] Plugin loads, `/nekodex` opens and closes the main window.
- [x] Config persists across a game restart.
- [x] Unloading the plugin leaves no leftover hooks, windows or commands (reload 5 times, no errors in log).

---

## Phase 2: Mount list (read-only)

Scope
- Read the full mount list from Lumina's Mount sheet (name, icon, order, patch if available).
- Read owned state for the logged-in character from the game's unlock state (via FFXIVClientStructs / Dalamud wrappers; verify the exact API at implementation time, it changes between versions).
- Main window: table with icon, name, owned/missing, progress bar (owned / total).
- Search box and filters (All / Owned / Missing).
- Handle "not logged in" and "character data not ready yet" states cleanly.
- Introduce the generic tracker abstraction now:
  - `ICollectionModule` (id, display name, enumerate entries, owned check)
  - `CollectionEntry` (id, name, icon, optional metadata)
  - Mounts are the first module.

Gate
- [x] Owned and total counts match the in-game Mount Guide on the same character.
- [x] Spot check 10 known owned and 10 known missing mounts: all correct.
- [x] No crashes when switching characters or logging out with the window open.
- [x] Filter + search combined work correctly.

---

## Phase 3: Unlock info (v1: link out, designed for in-game later)

Scope
- Define `IUnlockSourceProvider`:
  - `GetSource(entry) → UnlockSource?` where `UnlockSource` has a type (quest, dungeon/trial/raid drop, vendor, achievement, event, store/other, unknown), a short text, and optional URL.
- v1 provider: **link provider**. Each missing mount gets a button that opens its Garland Tools / wiki / FFXIV Collect page in the browser. Verify each site's terms and any API/rate limits before relying on them.
- Include an explicit `Unknown` state, never a blank.
- Design for the later provider: a **bundled dataset provider** that ships unlock data with the plugin (generated at build time or cached on first run, with an offline fallback) so "how to get it" shows in the game window. Do not implement yet, but do not paint the design into a corner.
- Licensing check recorded in the repo (`DATA_SOURCES.md`): where each dataset comes from, its license, and attribution requirements.

Gate
- [x] Every missing mount shows either a link or an explicit "unknown".
- [x] Links open the right page for 10 spot-checked mounts.
- [x] Provider interface has a stub in-game provider implementation proving the UI can render text without links.
- [x] `DATA_SOURCES.md` exists and covers every external source used.

---

## Phase 4: In-game unlock info

Scope
- Implement the bundled dataset provider.
- Preferred approach: a small generator tool (separate project in the repo) that builds a versioned JSON data file from the chosen source plus Lumina data, committed or released with the plugin. Include a data version and patch number.
- Mount detail pane in the window: unlock type, steps, location, requirements, with an optional "open on web" button.
- Graceful staleness: show data version, and fall back to link-out for entries newer than the dataset.

Gate
- [x] 100% of missing mounts show in-game unlock text or an explicit "unknown".
- [x] Works fully offline.
- [x] Data file regeneration is one command and documented.
- [x] Entries newer than the dataset degrade to link-out, not errors.

---

## Phase 5: Daily/weekly dashboard

Scope
- Reset countdowns computed locally from UTC reset times: daily, weekly, and any other resets you actually care about (decide the list at the start of this phase).
  - Decided 2026-10-16: daily 15:00 UTC, weekly Tuesday 08:00 UTC, Grand Company missions 20:00 UTC, Jumbo Cactpot drawing Saturday 19:00 UTC, Fashion Report judging opens Friday 08:00 UTC.
- Custom checklist: user-defined items, each with a reset cadence (daily / weekly / never), checked state persisted and auto-cleared at reset.
- Optionally read in-game completion state where it is reliably available (e.g. roulettes); mark such items as "auto" vs "manual".
  - Deferred 2026-10-16: v1 of this phase ships manual items only.
- Compact always-on-top mini window option.

Gate
- [ ] Countdowns match in-game reset times across a reset boundary.
- [ ] Checklist items clear at the correct reset and not before.
- [ ] Time handling is tested with fake clocks, including DST edge cases for display.

---

## Phase 6: Glamour collections

Scope (two sub-tracks, deliver tracking first)
- **6a. Tracking (owned vs missing):** glamour items and sets available to the character (Glamour Dresser, Armoire, inventory as feasible), minions/orchestrion as further modules reusing the same tracker core. Verify which of these can be read from game state, and document any that cannot.
- **6b. Outfit planner:** build a named outfit from owned items, preview as a list with icons, save and load outfits, and show which items are missing for an outfit. In-game application of outfits stays out of scope unless it can be done through supported means; planning only.

Gate
- [ ] 6a: counts match the in-game collection UIs for each module delivered.
- [ ] Adding a new module touches only a module class and a registration line, not the UI core.
- [ ] 6b: outfits persist, and missing items are listed correctly for a test outfit.

---

## Phase 7: Gear advisor (optional)

Decide scope first (open decision above).
- **Simple:** per-slot "next upgrade by item level" for current job and level, with where-to-get info reusing the unlock provider.
- **Full:** per-job best-in-slot sets, requires stat priority data and per-patch maintenance. Only if you commit to maintaining it.

Gate
- [ ] Scope decision written down.
- [ ] Suggestions for 3 different jobs at 3 different levels are sanity-checked by hand.

---

## Phase 8: Theme, polish, release

Scope
- Neko theme: color styles, rounding, fonts/icons, applied by pushing/popping ImGui style per window so it never leaks into other plugins' UI.
- Toggle in config: Neko theme / default Dalamud styling, applied live.
- Settings cleanup, tooltips, empty states, accessibility pass (contrast, font scaling).
- README, screenshots, `DATA_SOURCES.md`, changelog.
- Custom repo: `repo.json` (pluginmaster) hosted on GitHub, with CI building releases.
- Decide on submission to the official plugin repository (its review process and rules apply; check current requirements before deciding).

Gate
- [ ] Theme toggle switches live with no visual leakage into other windows.
- [ ] Fresh install from the custom repo works on a clean XIVLauncher setup.
- [ ] README, license and data attributions complete.

---

## Cross-cutting rules
- Verify Dalamud API level and any ClientStructs usage against current docs at the start of each phase; they change with game patches.
- Never read or write game memory in ways that mutate state. Read-only.
- Handle every "game not ready" case (login screen, loading, character select).
- Keep ImGui code thin; logic lives in `Core` and is unit tested where practical.
- After a game patch: expect breakage, keep a documented "patch day" checklist.
- Log with `IPluginLog`; no console spam in release builds.

## Suggested repo layout
```
/src/NekoDex/            plugin project
  Core/                  tracker abstractions, modules, reset-time logic
  Data/                  providers (link, bundled dataset), Lumina readers
  UI/                    windows, theme
  Configuration.cs
  Plugin.cs
/tools/DataGen/          dataset generator (Phase 4)
/tests/NekoDex.Tests/
DATA_SOURCES.md
README.md
```
