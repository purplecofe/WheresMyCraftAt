using ExileCore.Shared.Helpers;
using ImGuiNET;
using SharpDX;
using System;
using Vector4 = System.Numerics.Vector4;

namespace WheresMyCraftAt.CraftingMenu.Styling;

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

public class ErrorStyling : IDisposable
{
    private readonly int count;
    public static readonly Vector4 PastelRed = new Vector4(0.8f, 0.3f, 0.3f, 1.0f);

    public ErrorStyling()
    {
        ImGui.PushStyleColor(ImGuiCol.Border, PastelRed);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 2.0f);
        ImGui.PushStyleColor(ImGuiCol.Text, PastelRed);
        count = 2;
    }

    public void Dispose()
    {
        ImGui.PopStyleColor(count);
        ImGui.PopStyleVar();
    }
}