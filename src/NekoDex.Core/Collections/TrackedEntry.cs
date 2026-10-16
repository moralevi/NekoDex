namespace NekoDex.Core.Collections;

/// <summary>A <see cref="CollectionEntry"/> paired with the current character's ownership of it.</summary>
public sealed record TrackedEntry(CollectionEntry Entry, bool Owned);
