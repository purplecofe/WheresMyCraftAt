using ExileCore.Shared.Helpers;
using ImGuiNET;
using SharpDX;
using System;

namespace WheresMyCraftAt.CraftingSequence.Styling;

public class ColorButton : IDisposable
{
    private readonly int count;

    public ColorButton(Color normal, Color hovered, Color active)
    {
        if (!WheresMyCraftAt.Main.Settings.Styling.CustomMenuStyling.Value)
            return;

        PushStyleColor(ImGuiCol.Button, normal);
        PushStyleColor(ImGuiCol.ButtonHovered, hovered);
        PushStyleColor(ImGuiCol.ButtonActive, active);
        count = 3;
    }

    public void Dispose()
    {
        if (!WheresMyCraftAt.Main.Settings.Styling.CustomMenuStyling.Value)
            return;

        ImGui.PopStyleColor(count);
    }

    private static void PushStyleColor(ImGuiCol imguiCol, Color color)
    {
        ImGui.PushStyleColor(imguiCol, color.ToImguiVec4());
    }
}