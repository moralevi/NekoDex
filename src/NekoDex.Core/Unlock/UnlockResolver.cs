using NekoDex.Core.Collections;

namespace NekoDex.Core.Unlock;

/// <summary>
/// Asks each provider in order and returns the first answer; otherwise an explicit Unknown.
/// Whatever the answer, a web link for the configured site is attached when the source has none of its own.
/// </summary>
public sealed class UnlockResolver
{
    private readonly IReadOnlyList<IUnlockSourceProvider> providers;
    private readonly Func<UnlockLinkSite> linkSite;

    public UnlockResolver(IReadOnlyList<IUnlockSourceProvider> providers, Func<UnlockLinkSite> linkSite)
    {
        this.providers = providers;
        this.linkSite = linkSite;
    }

    public UnlockSource Resolve(string moduleId, CollectionEntry entry)
    {
        UnlockSource? source = null;
        foreach (var provider in providers)
        {
            source = provider.GetSource(moduleId, entry);
            if (source is not null)
            {
                break;
            }
        }

        source ??= UnlockSource.Unknown();

        if (source.Url is null)
        {
            var url = UnlockLinks.BuildUrl(linkSite(), moduleId, entry.Id);
            if (url is not null)
            {
                source = source with { Url = url };
            }
        }

        return source;
    }
}
