using NekoDex.Core.Collections;

namespace NekoDex.UI;

/// <summary>Module-specific content drawn under the unlock lines in a collection tab's detail pane.</summary>
public interface IDetailExtension
{
    void Draw(CollectionEntry entry);
}
