using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace NekoDex.UI;

/// <summary>
/// The Neko look: warm plum backgrounds, salmon accents, rounded corners. Pushed per window in PreDraw and popped in
/// PostDraw so nothing leaks into other plugins. <see cref="Active"/> is set by <see cref="ThemedWindow"/> for the duration
/// of its draw so status colours follow the theme too.
/// </summary>
internal static class Theme
{
    private static readonly Vector4 Plum = new(0.13f, 0.10f, 0.16f, 0.97f);
    private static readonly Vector4 PlumLight = new(0.20f, 0.15f, 0.24f, 1f);
    private static readonly Vector4 PlumLighter = new(0.27f, 0.20f, 0.32f, 1f);
    private static readonly Vector4 Salmon = new(0.93f, 0.55f, 0.65f, 1f);
    private static readonly Vector4 SalmonHover = new(0.98f, 0.66f, 0.74f, 1f);
    private static readonly Vector4 SalmonDim = new(0.62f, 0.36f, 0.45f, 1f);
    private static readonly Vector4 Cream = new(0.97f, 0.94f, 0.92f, 1f);
    private static readonly Vector4 CreamDim = new(0.70f, 0.63f, 0.68f, 1f);
    private static readonly Vector4 Border = new(0.42f, 0.28f, 0.40f, 0.60f);

    private static readonly Vector4 DefaultOwned = new(0.55f, 0.85f, 0.55f, 1f);
    private static readonly Vector4 DefaultMissing = new(0.85f, 0.55f, 0.55f, 1f);
    private static readonly Vector4 NekoOwned = new(0.58f, 0.90f, 0.72f, 1f);
    private static readonly Vector4 NekoMissing = new(0.99f, 0.56f, 0.56f, 1f);

    /// <summary>True while a themed window is drawing with the Neko theme on.</summary>
    public static bool Active { get; set; }

    public static Vector4 OwnedColor => Active ? NekoOwned : DefaultOwned;

    public static Vector4 MissingColor => Active ? NekoMissing : DefaultMissing;

    /// <summary>Pushes every colour and style var. Dispose to pop them all; nothing is left on the stack.</summary>
    public static IDisposable Push()
    {
        var colors = ImRaii.PushColor(ImGuiCol.WindowBg, Plum)
            .Push(ImGuiCol.ChildBg, new Vector4(0, 0, 0, 0.12f))
            .Push(ImGuiCol.PopupBg, Plum)
            .Push(ImGuiCol.Border, Border)
            .Push(ImGuiCol.FrameBg, PlumLight)
            .Push(ImGuiCol.FrameBgHovered, PlumLighter)
            .Push(ImGuiCol.FrameBgActive, SalmonDim)
            .Push(ImGuiCol.TitleBg, PlumLight)
            .Push(ImGuiCol.TitleBgActive, PlumLighter)
            .Push(ImGuiCol.TitleBgCollapsed, PlumLight)
            .Push(ImGuiCol.ScrollbarBg, Plum)
            .Push(ImGuiCol.ScrollbarGrab, PlumLighter)
            .Push(ImGuiCol.ScrollbarGrabHovered, SalmonDim)
            .Push(ImGuiCol.ScrollbarGrabActive, Salmon)
            .Push(ImGuiCol.CheckMark, Salmon)
            .Push(ImGuiCol.SliderGrab, Salmon)
            .Push(ImGuiCol.SliderGrabActive, SalmonHover)
            .Push(ImGuiCol.Button, SalmonDim)
            .Push(ImGuiCol.ButtonHovered, Salmon)
            .Push(ImGuiCol.ButtonActive, SalmonHover)
            .Push(ImGuiCol.Header, SalmonDim)
            .Push(ImGuiCol.HeaderHovered, Salmon)
            .Push(ImGuiCol.HeaderActive, SalmonHover)
            .Push(ImGuiCol.Separator, Border)
            .Push(ImGuiCol.ResizeGrip, SalmonDim)
            .Push(ImGuiCol.ResizeGripHovered, Salmon)
            .Push(ImGuiCol.ResizeGripActive, SalmonHover)
            .Push(ImGuiCol.Tab, PlumLight)
            .Push(ImGuiCol.TabHovered, Salmon)
            .Push(ImGuiCol.TabActive, SalmonDim)
            .Push(ImGuiCol.TabUnfocused, PlumLight)
            .Push(ImGuiCol.TabUnfocusedActive, SalmonDim)
            .Push(ImGuiCol.PlotHistogram, Salmon)
            .Push(ImGuiCol.TableHeaderBg, PlumLighter)
            .Push(ImGuiCol.TableBorderStrong, Border)
            .Push(ImGuiCol.TableBorderLight, new Vector4(Border.X, Border.Y, Border.Z, 0.35f))
            .Push(ImGuiCol.TableRowBg, new Vector4(0, 0, 0, 0))
            .Push(ImGuiCol.TableRowBgAlt, new Vector4(1, 1, 1, 0.03f))
            .Push(ImGuiCol.TextSelectedBg, SalmonDim)
            .Push(ImGuiCol.NavHighlight, Salmon)
            .Push(ImGuiCol.Text, Cream)
            .Push(ImGuiCol.TextDisabled, CreamDim);

        var styles = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 10f)
            .Push(ImGuiStyleVar.ChildRounding, 8f)
            .Push(ImGuiStyleVar.PopupRounding, 8f)
            .Push(ImGuiStyleVar.FrameRounding, 6f)
            .Push(ImGuiStyleVar.GrabRounding, 6f)
            .Push(ImGuiStyleVar.TabRounding, 6f)
            .Push(ImGuiStyleVar.ScrollbarRounding, 8f)
            .Push(ImGuiStyleVar.WindowBorderSize, 1f)
            .Push(ImGuiStyleVar.FramePadding, new Vector2(6f, 4f))
            .Push(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 5f))
            .Push(ImGuiStyleVar.WindowTitleAlign, new Vector2(0.5f, 0.5f));

        return new Scope(colors, styles);
    }

    private sealed class Scope(IDisposable colors, IDisposable styles) : IDisposable
    {
        public void Dispose()
        {
            styles.Dispose();
            colors.Dispose();
        }
    }
}
