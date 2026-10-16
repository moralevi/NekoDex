namespace NekoDex.Core.Collections;

/// <summary>
/// One collectable thing (a mount, a minion, ...) independent of whether the current character owns it.
/// </summary>
/// <param name="Id">Row id in the game's data sheet for this collection.</param>
/// <param name="Name">Display name in the client language.</param>
/// <param name="IconId">Game icon id, 0 when unknown.</param>
/// <param name="SortOrder">Order used by the in-game collection UI, when the sheet provides one.</param>
/// <param name="Category">Optional grouping the module exposes as a column and sort key (card type, emote category).</param>
/// <param name="Rank">Optional numeric rank exposed as a column and sort key (card stars). 0 means none.</param>
public sealed record CollectionEntry(uint Id, string Name, uint IconId, int SortOrder = 0, string? Category = null, int Rank = 0);
