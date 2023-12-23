using ExileCore;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using static WheresMyCraftAt.WheresMyCraftAt;
using Color = SharpDX.Color;
using Vector4 = System.Numerics.Vector4;

namespace WheresMyCraftAt.Logging;

public static class Logging
{
    private static readonly Dictionary<Enums.WheresMyCraftAt.LogMessageType, Color> LogMessageColors = new()
    {
        { Enums.WheresMyCraftAt.LogMessageType.Info, Color.White },
        { Enums.WheresMyCraftAt.LogMessageType.Warning, Color.Yellow },
        { Enums.WheresMyCraftAt.LogMessageType.Error, Color.Red },
        { Enums.WheresMyCraftAt.LogMessageType.Success, Color.Green },
        { Enums.WheresMyCraftAt.LogMessageType.Cancelled, Color.Orange },
        { Enums.WheresMyCraftAt.LogMessageType.Special, Color.Gray },
        { Enums.WheresMyCraftAt.LogMessageType.Profiler, Color.SkyBlue }
    };

    private static readonly object Locker = new();
    private static readonly List<DebugMsgDescription> MessagesList = new(24);

    public static void Render()
    {
        if (!Main.Settings.ShowLogWindow)
            return;

        using var fontPush = Main.Graphics.UseCurrentFont();
        var flags = ImGuiWindowFlags.AlwaysVerticalScrollbar;

        if (Main.CurrentOperation is not null)
            flags = ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.NoInputs;

        ImGui.Begin("WheresMyCraftAt Logs", flags);

        lock (Locker)
        {
            foreach (var msg in MessagesList.Where(msg => msg != null))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, msg.ColorV4);
                ImGui.TextUnformatted($"{msg.Time.ToLongTimeString()}: {msg.Msg}");
                ImGui.PopStyleColor();
            }
        }

        if (Main.CurrentOperation is not null)
            // Set auto scroll when running
            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                ImGui.SetScrollHereY(1.0f);

        ImGui.End();
    }

    public static void Add(string msg, Enums.WheresMyCraftAt.LogMessageType messageType)
    {
        try
        {
            var color = LogMessageColors[messageType];
            var time = Main.Settings.DebugPrintLingerTime;

            if (Main.Settings.DebugPrint)
                DebugWindow.LogMsg(msg, Main.Settings.DebugPrintLingerTime, color);

            var debugMsgDescription = new DebugMsgDescription
            {
                Msg = msg,
                Time = DateTime.Now,
                ColorV4 = color.ToImguiVec4(),
                Color = color
            };

            lock (Locker)
            {
                MessagesList.Add(debugMsgDescription);
            }
        }
        catch (Exception e)
        {
            DebugWindow.LogError($"{nameof(DebugWindow)} -> {e}");
        }
    }

    private class DebugMsgDescription
    {
        public string Msg { get; init; }
        public DateTime Time { get; init; }
        public Vector4 ColorV4 { get; init; }
        public Color Color { get; init; }
    }
}