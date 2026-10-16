# FFXIV Dalamud Plugin: Collection Tracker (working name: NekoDex)

Status: Phases 0-7 gates passed 2026-10-16. Phase 8 built (theme and fresh-install gates pending; fresh install needs the GitHub repo public). Phases 9 to 14 gates passed; Phase 15 built, in-game gate pending. Remaining before a public release: Phase 8 theme-leak check, screenshots, repo public, first tag, FFXIV Collect redistribution answer. Target: friends first, public custom repo later.
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
- Gear advisor scope: simple "next upgrade by item level" vs per-job best-in-slot. Needed before Phase 7. **Decided 2026-10-16: simple.** Best-in-slot would need stat-priority tables maintained every patch; nobody has committed to that.
- Final plugin name and Neko branding. Needed before Phase 8. **Decided 2026-10-16: NekoDex, fresh plum/salmon theme, cat-face icon.**
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
- [x] Countdowns match in-game reset times across a reset boundary.
- [x] Checklist items clear at the correct reset and not before.
- [x] Time handling is tested with fake clocks, including DST edge cases for display.

---

## Phase 6: Glamour collections

Scope (two sub-tracks, deliver tracking first)
- **6a. Tracking (owned vs missing):** glamour items and sets available to the character (Glamour Dresser, Armoire, inventory as feasible), minions/orchestrion as further modules reusing the same tracker core. Verify which of these can be read from game state, and document any that cannot.
  - Verified 2026-10-16: minions (`UIState.IsCompanionUnlocked`) and orchestrion (`PlayerState.IsOrchestrionRollUnlocked`) read like mounts. Armoire contents (`UIState.Cabinet`) exist only after the player opens the Armoire in the session; the module reports itself unavailable until then. Glamour Dresser contents (`MirageManager.PrismBoxItemIds`) exist only after the dresser is opened; NekoDex stores the last seen list in its config and uses it for the outfit planner until the dresser is opened again. Bags, armoury chest and equipped gear are readable any time (`InventoryManager`). Retainer inventories are not readable without visiting the retainer and are not used.
- **6b. Outfit planner:** build a named outfit from owned items, preview as a list with icons, save and load outfits, and show which items are missing for an outfit. In-game application of outfits stays out of scope unless it can be done through supported means; planning only.

Gate
- [x] 6a: counts match the in-game collection UIs for each module delivered.
- [x] Adding a new module touches only a module class and a registration line, not the UI core.
- [x] 6b: outfits persist, and missing items are listed correctly for a test outfit.

---

## Phase 7: Gear advisor (optional)

Decide scope first (open decision above). Decided 2026-10-16: **Simple**. Where-to-get comes from game data (recipes, gil shops) with a Garland Tools link for everything else; the advisor also says when you already own the suggested piece (bags, Armoire or Glamour Dresser).
- **Simple:** per-slot "next upgrade by item level" for current job and level, with where-to-get info reusing the unlock provider.
- **Full:** per-job best-in-slot sets, requires stat priority data and per-patch maintenance. Only if you commit to maintaining it.

Gate
- [x] Scope decision written down.
- [x] Suggestions for 3 different jobs at 3 different levels are sanity-checked by hand.

---

## Phase 8: Theme, polish, release

Scope
- Neko theme: color styles, rounding, fonts/icons, applied by pushing/popping ImGui style per window so it never leaks into other plugins' UI.
- Toggle in config: Neko theme / default Dalamud styling, applied live.
- Settings cleanup, tooltips, empty states, accessibility pass (contrast, font scaling).
- README, screenshots, `DATA_SOURCES.md`, changelog.
- Custom repo: `repo.json` (pluginmaster) hosted on GitHub, with CI building releases.
- Decide on submission to the official plugin repository (its review process and rules apply; check current requirements before deciding).
  - Open as of 2026-10-16. Prerequisites known so far: public source, the FFXIV Collect redistribution answer recorded in DATA_SOURCES.md, and a review of goatcorp's current plugin rules. Not required for the friends release.

Gate
- [ ] Theme toggle switches live with no visual leakage into other windows.
- [ ] Fresh install from the custom repo works on a clean XIVLauncher setup.
- [ ] README, license and data attributions complete.

---

# Part 2: after 1.0 (planned 2026-10-16)

Same method: each phase has a scope and a gate; a phase starts only when the previous gate passes. Order is by value for effort; Phase 9 first because it is a latent bug. Phases 9 to 11 make 1.1; the rest are 1.2 and later unless a friend asks for one sooner.

**Decisions already made**
| Topic | Decision |
|---|---|
| Per-character data | Cache keyed by content id for dresser snapshots; still no cross-character views |
| Module additions | Only collections with a readable unlock bitfield and FFXIV Collect data |
| Localisation | CheapLoc export; translations accepted as files, English stays the fallback |

**Open decisions (resolve before the phase that needs them)**
- Which roulettes and tribal quests count as auto items, and whether auto items can be hidden individually. Needed before Phase 11. **Decided 2026-10-16:** all eleven duty roulettes offered, tribal counts as done when all 12 allowances are used, Wondrous Tails when 9 stickers are placed; every auto item can be toggled in settings.
- Whether glamour plate import should also copy dyes. Needed before Phase 13. **Decided 2026-10-16: yes, items and both dyes.**
- Whether the scheduled dataset refresh opens a pull request or commits straight to `main`. Needed before Phase 14. **Decided 2026-10-16: pull request**, because the data is embedded in the release and the generator's warnings deserve a look.

---

## Phase 9: Per-character correctness (1.1)

Scope
- Glamour Dresser snapshot stored per character: a map from content id to snapshot in the config, replacing the single list. Migrate the existing single snapshot to the current character's entry on first load (config version 2, the first real migration step).
- Outfit ownership and the Outfits footer read the snapshot of the logged-in character only; another character with no snapshot sees "not read yet".
- Armoire and inventory are already live reads and need no change; verify that logging out and into a different character shows no stale ownership anywhere (collections, outfits, advisor).
- Config migration helper in Core so old config shapes can be unit tested without Dalamud.

Gate
- [x] A v1 config file with a dresser snapshot migrates to v2 and the snapshot lands under the character that loaded it.
- [x] Two characters on the same account show their own dresser contents in the Outfits tab, and a third with no snapshot shows the "not read yet" footer.
- [x] Migration covered by a unit test; reload 5 times with no errors in `/xllog`.

---

## Phase 10: Tracker usability (1.1)

Scope
- **Obtainable filter.** A third filter axis (All / Obtainable / Not obtainable) derived from dataset categories: Premium, Event and legacy sources count as not obtainable; everything else is. The mapping lives in Core next to `UnlockCategories` and is tested. Entries without data count as obtainable.
- **Sorting.** Table headers sort by name, patch, unlock type and status, with missing-first as a toggle. Sorting is a pure function in Core over tracked entries plus their unlock source, tested.
- **Pinned entries.** Star an entry in any module; pins are stored per module in the config and shown at the top of the tab and in the mini window as "working on", with a one-time prompt to unpin once the entry becomes owned.
- **Progress summary** on the Dashboard: one row per module with owned/total and a bar, reusing `CollectionProgress`.

Gate
- [x] Obtainable filter hides every Premium and Event mount and nothing else, checked against 10 known cases.
- [x] Every sort order is stable and, combined with search and the ownership filter, still returns the right rows (unit tests plus a spot check in game).
- [x] A pinned mount appears in the mini window and leaves it when unlocked.
- [x] Dashboard summary counts match the tabs.

---

## Phase 11: Automatic checklist items (1.1)

Scope
- Verify first: which completion states ClientStructs exposes reliably this patch (duty roulette completion, tribal quest allowances, Wondrous Tails, challenge log). Record the list and the ones that cannot be read.
  - Verified 2026-10-16: readable are duty roulette completion (`InstanceContent.IsRouletteComplete`), tribal allowances left (`QuestManager.GetBeastTribeAllowance`), Wondrous Tails journal and sticker count (`PlayerState`), challenge log entries (`ContentsNote.IsContentNoteComplete`, deferred: too granular for a checklist). Not exposed: Mini Cactpot plays, Jumbo Cactpot tickets, Grand Company turn-ins, custom delivery allowances.
- A checklist source abstraction in Core: id, name, cadence, and a completion check the Data layer provides. Auto items render with an "auto" badge and cannot be ticked by hand; manual items stay as they are.
- Settings: pick which auto items to show. Default: the expert and leveling roulettes and tribal allowances.
- Mini window shows unfinished auto items too.

Gate
- [x] Each auto item flips to done within a few seconds of completing it in game and clears at the right reset.
- [x] Items the game cannot report are documented in the spec and not offered.
- [x] Reset clearing of auto items is covered by fake-clock tests.

---

## Phase 12: More collection modules (1.2)

Scope
- One module per collection, each a Data class plus a registration line: emotes, hairstyles, bardings, Triple Triad cards, fashion accessories, framer's kits, facewear. Verify the unlock read for each; some need the item id to unlock id mapping from the sheets.
  - Verified 2026-10-16. Delivered: emotes (`UIState.IsEmoteUnlocked`, rows with an unlock link), bardings (`CompanionInfo.IsBuddyEquipUnlocked`), Triple Triad cards (`UIState.IsTripleTriadCardUnlocked`, ordered by TripleTriadCardResident), fashion accessories (`PlayerState.IsOrnamentUnlocked`), facewear per style (`PlayerState.IsGlassesStyleUnlocked` over GlassesStyle). Deferred: hairstyles (unlock is per race via CharaMakeCustomize unlock links and there is no in-game count to gate against) and framer's kits (FFXIV Collect has no endpoint; the unlock id to sheet mapping is unverified).
- DataGen gains a module row per collection pointing at the matching FFXIV Collect endpoint; `UnlockLinks` gains the page segments. Record any endpoint that does not exist.
- Modules whose in-game guide applies extra visibility rules (emotes split by category, cards by region) keep the same ordering as the guide.

Gate
- [x] Each delivered module's owned/total matches its in-game guide on the same character.
- [x] Dataset regeneration still one command; warnings list only rows that genuinely lack data.
- [x] No module touches the UI core; the diff for each is its class, a registration line, a generator row and link segments.

---

## Phase 13: Outfit planner polish (1.2)

Scope
- **Fill from current gear** seeds all slots from the equipped items (reader exists from Phase 7).
- **Fill from glamour plate** reads the plates the game exposes after the dresser was opened and lists them by number; choosing one copies its items. Dyes per the open decision.
- **Dyes** per slot: two stain ids per item, picked from the Stain sheet with a colour swatch, stored in the outfit; the dresser snapshot already carries stain ids so "owned in this dye" can be shown as a secondary hint.
- **Item sources** for outfit items: extend the dataset with FFXIV Collect item sources where an endpoint exists; otherwise keep recipes, gil shops and the Garland link.
  - 2026-10-16: FFXIV Collect has no item endpoint, so outfit items keep recipes, gil shops and the Garland link.

Gate
- [x] Fill from gear and fill from a plate reproduce what the game shows, slot for slot, for a test outfit.
- [x] Dyes persist and display with the right swatch.
- [x] Every outfit item still resolves to text, a link or an explicit Unknown.

---

## Phase 14: Data and localisation infrastructure (1.2)

Scope
- **Scheduled dataset refresh**: a weekly GitHub Actions job that runs DataGen and opens a pull request when `unlocks.json` changed. CI has no game files, so DataGen gets a flag to skip sheet validation and take the max id from the API; the next local run re-validates. Decide PR versus direct commit (open decision).
- **Localisation**: export CheapLoc strings to `loc/en.json`, load the file for the client language at startup, document the translation workflow in the README. Ship German, French and Japanese files as empty templates.
- **Forget this character** button in settings that removes that character's cached data and nothing else.

Gate
- [ ] The scheduled job produces a reviewable change with the generator's warning summary in its body. (Runs weekly on GitHub; verify after the first scheduled or manual run.)
- [x] Switching client language loads the matching file and falls back to English for missing keys.
- [x] Forgetting a character removes its data and nothing else.

---

## Release rhythm after 1.0
- Tag per phase group: `v1.1.0.0` after Phase 11, `v1.2.0.0` after Phase 14. Patch releases in between only for breakage after a game patch (see `docs/patch-day.md`).
- Every release updates `CHANGELOG.md` and reruns DataGen.
- Public repository and the FFXIV Collect redistribution answer remain prerequisites for anything beyond the friends build.

---

# Part 3: focus round (planned 2026-10-16)

From the second Q&A: the friends collect mounts, cards and glamour; languages are English and Swedish; clear yeses were a second unlock source, tomestone-aware gear, a "new this patch" view, and export/import of user data. Swedish goes last by the user's choice. Same method as before.

**Decisions already made**
| Topic | Decision |
|---|---|
| Glamour depth | Dresser capacity warning first; look-alike matching by model id parked |
| Outfit sharing | Through the export file, never through chat |
| Repo visibility | Stays private until the user says otherwise; FFXIV Collect question only matters before going public |

**Open decisions (resolve before the phase that needs them)**
- Garland Tools data: which file or endpoint, and its terms. Needed before Phase 17.
- Which tomestone currencies count (current and previous tier only, or all). Needed before Phase 18.
- Whether Dalamud lists Swedish as a UI language; if not, a plugin-level language override is required. Needed before Phase 20.

---

## Phase 15: New this patch

Scope
- Core: `LatestPatch(module)` from the dataset's patch fields; an "Added in {patch}" filter axis next to ownership and availability; patch sorting already exists.
- Dashboard: one line per module, "N new in {patch}, M of them missing", clicking it opens the tab with the filter set.
- Detail pane: highlight the patch when it is the latest.

Gate
- [ ] Filter shows exactly the entries whose patch equals the dataset's latest patch for that module (unit tests plus a spot check).
- [ ] Dashboard counts match the filtered tabs.

---

## Phase 16: Export and import of user data

Scope
- One JSON file with a schema version holding outfits, pins, manual checklist items, enabled auto items and dresser snapshots. Config-only settings (theme, link site) stay out.
- Core: serialisation model plus merge rules (replace all, or merge by id keeping local on conflict), tested.
- Settings: Export to a chosen folder (default the plugin config folder), Import with a preview of counts and the merge choice, and a "share outfits only" export for friends.

Gate
- [ ] Export then import on a clean config reproduces outfits, pins, checklist and snapshots exactly (unit test on the model, manual check in game).
- [ ] Importing an outfits-only file from another person adds their outfits without touching anything else.
- [ ] A file with an unknown schema version is refused with a clear message.

---

## Phase 17: Second unlock source

Scope
- Verify first: what Garland Tools publishes as data files, their terms, and how its ids map to the game sheets. Record it in DATA_SOURCES.md.
- DataGen gains a second source per module merged behind FFXIV Collect: an entry keeps Collect's text when present and takes Garland's otherwise; both attributions are kept per entry.
- Provider chain unchanged; the dataset carries a `source` per entry and the UI shows the right attribution.

Gate
- [ ] The number of Unknown entries per module drops, and the drop is listed in the generator output.
- [ ] Every entry shows the attribution of the source it came from.
- [ ] DATA_SOURCES.md covers Garland's data terms.

---

## Phase 18: Tomestone-aware gear advisor

Scope
- Verify first: reading tomestone counts (`InventoryManager` currency container) and the SpecialShop sheet rows that sell gear for tomestones; decide which currencies count (open decision).
- Core: price lookup per item and an "affordable now" flag; suggestions show the price and currency, and affordable pieces are marked.
- Vendor location from the shop's NPC when the sheets provide it; otherwise the existing web link.

Gate
- [ ] For three jobs, every tomestone piece the advisor marks affordable is buyable in game at that moment, and the price matches the vendor.
- [ ] Non-tomestone suggestions are unchanged.

---

## Phase 19: Dresser capacity warning

Scope
- Outfits footer and Dashboard show dresser usage against the 800-item limit from the live read or the snapshot, with a warning colour above a threshold set in settings (default 760).
- For an outfit, show how many of its missing pieces would need dresser space and whether the dresser can hold them.
- Verify the limit and whether the game exposes it rather than hard-coding 800.

Gate
- [ ] Usage matches the in-game dresser count.
- [ ] Warning appears at the threshold and not below.

---

## Phase 20: Swedish

Scope
- Verify whether Dalamud offers Swedish as a UI language. If not, add a plugin setting "Language: follow Dalamud / English / Swedish" that overrides which loc file loads.
- Ship `loc/sv.json`, translated by the user or a friend from `loc/en.json`; LocExport gains a `--check <file>` mode that lists keys missing or extra in a translation.
- Review strings with placeholders for word-order problems and allow reordering via the format arguments.

Gate
- [ ] With Swedish selected every visible string is Swedish except keys the check tool reports as missing.
- [ ] `--check` reports zero missing keys for sv.json at release.

---

## Release rhythm for Part 3
- First public tag still waits on the user's go-ahead and the FFXIV Collect answer. Until then friends install from the dev build or a private release asset.
- Tag `v1.3.0.0` after Phase 17 and `v1.4.0.0` after Phase 20.

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
