using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace NekoDex.UI;

/// <summary>A window that wears the Neko theme while the setting is on. Push happens before Begin, pop after End, every frame.</summary>
public abstract class ThemedWindow : Window
{
    private readonly ConfigurationManager config;
    private IDisposable? themeScope;

    protected ThemedWindow(ConfigurationManager config, string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        : base(name, flags)
    {
        this.config = config;
    }

    public override void PreDraw()
    {
        if (config.Current.UseNekoTheme)
        {
            themeScope = Theme.Push();
            Theme.Active = true;
        }
    }

    public override void PostDraw()
    {
        themeScope?.Dispose();
        themeScope = null;
        Theme.Active = false;
    }
}
