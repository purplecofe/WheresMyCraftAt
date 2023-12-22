using ExileCore;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using System;
using System.Collections.Generic;
using static WheresMyCraftAt.WheresMyCraftAt;
using Color = SharpDX.Color;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace WheresMyCraftAt.Handlers
{
    public static class Logging
    {
        private class DebugMsgDescription
        {
            public string Msg { get; init; }
            public DateTime Time { get; init; }
            public Vector4 ColorV4 { get; init; }
            public Color Color { get; init; }
        }

        private static readonly object locker = new();
        private static readonly List<DebugMsgDescription> MessagesList = new(24);
        private static Vector2 position;

        public static void Render()
        {
            if (!Main.Settings.ShowLogWindow)
                return;

            using var fontPush = Main.Graphics.UseCurrentFont();
            var flags = ImGuiWindowFlags.AlwaysVerticalScrollbar;

            if (Main._currentOperation is not null)
            {
                flags = ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.NoInputs;
            }

            ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.Once);
            ImGui.SetNextWindowSize(new Vector2(600, 1000), ImGuiCond.Once);
            ImGui.Begin("WheresMyCraftAt Logs", flags);

            foreach (var msg in MessagesList)
            {
                if (msg == null) continue;
                ImGui.PushStyleColor(ImGuiCol.Text, msg.ColorV4);
                ImGui.TextUnformatted($"{msg.Time.ToLongTimeString()}: {msg.Msg}");
                ImGui.PopStyleColor();
            }
            if (Main._currentOperation is not null)
            {
                // Set auto scroll when running
                if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                    ImGui.SetScrollHereY(1.0f);
            }

            ImGui.End();
        }

        public static void Add(string msg, LogMessageType messageType)
        {
            try
            {
                Color color = Main._logMessageColors[messageType];
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

                lock (locker)
                {
                    MessagesList.Add(debugMsgDescription);
                }
            }
            catch (Exception e)
            {
                DebugWindow.LogError($"{nameof(DebugWindow)} -> {e}");
            }
        }
    }
}