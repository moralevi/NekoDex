using System.Numerics;

namespace NekoDex.UI;

/// <summary>Colours used by the windows. The Neko theme (Phase 8) will swap these and push ImGui styles per window.</summary>
internal static class Theme
{
    public static readonly Vector4 OwnedColor = new(0.55f, 0.85f, 0.55f, 1f);
    public static readonly Vector4 MissingColor = new(0.85f, 0.55f, 0.55f, 1f);
}
