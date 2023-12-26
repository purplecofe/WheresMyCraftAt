using ExileCore;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static WheresMyCraftAt.WheresMyCraftAt;
using Color = SharpDX.Color;
using Vector4 = System.Numerics.Vector4;

namespace WheresMyCraftAt.Logging;

public static class Logging
{
    private static readonly Dictionary<Enums.WheresMyCraftAt.LogMessageType, Color> LogMessageColors = new()
    {
        // Light gray for detailed, low-level messages
        {
            Enums.WheresMyCraftAt.LogMessageType.Trace, Color.LightGray
        },

        // Cyan for debug-level messages
        {
            Enums.WheresMyCraftAt.LogMessageType.Debug, Color.Cyan
        },

        // White for informational messages
        {
            Enums.WheresMyCraftAt.LogMessageType.Info, Color.White
        },

        // Yellow for warnings
        {
            Enums.WheresMyCraftAt.LogMessageType.Warning, Color.Yellow
        },

        // Red for errors
        {
            Enums.WheresMyCraftAt.LogMessageType.Error, Color.Red
        },

        // Dark red for critical errors
        {
            Enums.WheresMyCraftAt.LogMessageType.Critical, Color.DarkRed
        },

        // Sky blue for profiler messages
        {
            Enums.WheresMyCraftAt.LogMessageType.Profiler, Color.SkyBlue
        },

        // Magenta for Evaluation messages
        {
            Enums.WheresMyCraftAt.LogMessageType.Evaluation, Color.Orange
        },

        // Magenta for special messages
        {
            Enums.WheresMyCraftAt.LogMessageType.Special, Color.Magenta
        }
    };

    private static readonly object Locker = new();
    public static List<DebugMsgDescription> MessagesList = new(24);

    public static void Render()
    {
        using var fontPush = Main.Graphics.UseCurrentFont();
        var flags = ImGuiWindowFlags.AlwaysVerticalScrollbar;

        if (Main.CurrentOperation is not null)
        {
            flags = ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.NoInputs;
        }

        var isOpen = Main.Settings.ShowLogWindow.Value;

        if (isOpen)
        {
            ImGui.Begin("Wheres My Craft At Logs", ref isOpen, flags);
            var logMessageTypes = Main.Settings.LogMessageFilters.Keys.ToList();

            for (var index = 0; index < logMessageTypes.Count; index++)
            {
                var logMessageType = logMessageTypes[index];
                var isEnabled = Main.Settings.LogMessageFilters[logMessageType];
                ImGui.Checkbox(logMessageType.ToString(), ref isEnabled);
                Main.Settings.LogMessageFilters[logMessageType] = isEnabled;

                if (index != logMessageTypes.Count - 1)
                {
                    ImGui.SameLine();
                }
            }

            ImGui.BeginChild("LogMessages", new Vector2(0, 0), ImGuiChildFlags.Border);

            lock (Locker)
            {
                foreach (var msg in MessagesList.Where(msg => msg != null))
                {
                    if (!Main.Settings.LogMessageFilters[msg.LogType])
                    {
                        continue;
                    }

                    ImGui.PushStyleColor(ImGuiCol.Text, msg.ColorV4);
                    ImGui.TextUnformatted($"{msg.Time.ToLongTimeString()}: {msg.Msg}");
                    ImGui.PopStyleColor();
                }
            }

            ImGui.EndChild();
            ImGui.End();
        }

        Main.Settings.ShowLogWindow.Value = isOpen;
    }

    public static void Add(string msg, Enums.WheresMyCraftAt.LogMessageType messageType)
    {
        try
        {
            var color = LogMessageColors[messageType];

            if (Main.Settings.DebugPrint)
            {
                DebugWindow.LogMsg(msg, Main.Settings.DebugPrintLingerTime, color);
            }

            var debugMsgDescription = new DebugMsgDescription
            {
                Msg = msg,
                Time = DateTime.Now,
                ColorV4 = color.ToImguiVec4(),
                Color = color,
                LogType = messageType
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

    public class DebugMsgDescription
    {
        public string Msg { get; init; }
        public DateTime Time { get; init; }
        public Vector4 ColorV4 { get; init; }
        public Color Color { get; init; }
        public Enums.WheresMyCraftAt.LogMessageType LogType { get; init; }
    }
}