using NekoDex.Core.Collections;

namespace NekoDex.Core.Unlock;

/// <summary>Answers "how do I get this?" for entries of a module. Return null when the provider has nothing; the resolver handles fallback.</summary>
public interface IUnlockSourceProvider
{
    UnlockSource? GetSource(string moduleId, CollectionEntry entry);
}
