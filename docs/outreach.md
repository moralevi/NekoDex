# Permission messages for data sources

Send these before the repository goes public. Record the answers in `DATA_SOURCES.md`.

## FFXIV Collect (Discord, link on https://ffxivcollect.com)

> Hi! I'm building a small open-source Dalamud plugin called NekoDex (MIT, GitHub: moralevi/NekoDex) that shows a player which mounts, minions, orchestrion rolls, emotes, bardings, fashion accessories, facewear and Triple Triad cards their character is missing, and how to get them.
>
> For the "how to get them" part I use your public API (`/api/mounts` and friends) at build time and bundle only the `sources` text (`type` and `text`) and the patch number per entry into a JSON file that ships inside the plugin, so it works offline in game. The plugin itself never calls your API. Every entry shows "FFXIV Collect, data YYYY-MM-DD" next to the text, and the plugin links to your pages for details.
>
> Is it okay with you that I redistribute that source text inside the plugin this way? If you'd prefer different attribution, a link format, or that I don't bundle it at all, I'll follow that. Thanks for the site and the API!

## Garland Tools (Discord, link on https://www.garlandtools.org)

> Hi! I'm building a small open-source Dalamud plugin called NekoDex (MIT, GitHub: moralevi/NekoDex) that shows a player which collectables (mounts, minions, cards, ...) their character is missing and how to get them.
>
> For a handful of entries that have no source elsewhere, a build-time tool reads your item documents (`/db/doc/item/en/3/{id}.json`), one request per entry with a pause between requests, and turns the `quests`, `achievements`, `instances`, `tradeShops` and `vendors` fields into short text lines bundled inside the plugin. Those entries show "Garland Tools, data YYYY-MM-DD" in the UI, and every entry links to the Garland item page. The plugin itself never calls your site.
>
> Is that acceptable? Happy to change attribution, request behaviour, or drop the bundling if you'd rather I only link. Thanks for Garland Tools!
