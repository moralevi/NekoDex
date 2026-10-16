using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace NekoDex.UI;

public sealed class ConfigWindow : Window
{
    private readonly ConfigurationManager config;

    public ConfigWindow(ConfigurationManager config)
        : base("NekoDex Settings###NekoDexConfig", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.config = config;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(320, 0) };
    }

    public override void Draw()
    {
        var current = config.Current;

        var openOnLogin = current.OpenMainWindowOnLogin;
        if (ImGui.Checkbox("Open NekoDex when a character logs in", ref openOnLogin))
        {
            current.OpenMainWindowOnLogin = openOnLogin;
            config.Save();
        }

        ImGui.Spacing();
        ImGui.TextDisabled($"Config v{current.Version}");
    }
}
