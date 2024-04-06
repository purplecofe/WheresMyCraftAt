using ExileCore.Shared.Helpers;
using ImGuiNET;
using SharpDX;
using System;

namespace WheresMyCraftAt.CraftingSequence.Styling;

public class ColorBackground : IDisposable
{
    private readonly int colorCount;

    public ColorBackground(params (ImGuiCol, Color)[] styles)
    {
        if (!WheresMyCraftAt.Main.Settings.Styling.CustomMenuStyling.Value)
            return;

        foreach (var (colorEnum, colorValue) in styles)
        {
            ImGui.PushStyleColor(colorEnum, colorValue.ToImguiVec4());
        }

        colorCount = styles.Length;
    }

    public void Dispose()
    {
        if (!WheresMyCraftAt.Main.Settings.Styling.CustomMenuStyling.Value)
            return;

        ImGui.PopStyleColor(colorCount);
    }
}