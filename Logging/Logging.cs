using ExileCore;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        },

        // Magenta for ItemData messages
        {
            Enums.WheresMyCraftAt.LogMessageType.ItemData, Color.Yellow
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

        var isOpen = Main.Settings.Debugging.LogWindow.Value;

        if (isOpen)
        {
            ImGui.Begin("Wheres My Craft At Logs", ref isOpen, flags);
            var logMessageTypes = Main.Settings.Debugging.LogMessageFilters.Keys.ToList();

            for (var index = 0; index < logMessageTypes.Count; index++)
            {
                var logMessageType = logMessageTypes[index];
                var isEnabled = Main.Settings.Debugging.LogMessageFilters[logMessageType];
                ImGui.Checkbox(logMessageType.ToString(), ref isEnabled);
                Main.Settings.Debugging.LogMessageFilters[logMessageType] = isEnabled;

                if (index != logMessageTypes.Count - 1)
                {
                    ImGui.SameLine();
                }
            }

            if (ImGui.Button("Save Log"))
            {
                List<string> stringList;

                lock (Locker)
                {
                    stringList = MessagesList
                                 .Where(msg => msg != null && Main.Settings.Debugging.LogMessageFilters[msg.LogType])
                                 .Select(msg => $"{msg.Time.ToLongTimeString()}: {msg.Msg}").ToList();
                }

                SaveLog(stringList);
            }

            ImGui.SameLine();

            if (ImGui.Button("Open Log Folder"))
            {
                var fullPath = Path.Combine(Main.ConfigDirectory, "SavedLogs");

                if (!Directory.Exists(fullPath))
                {
                    Add(
                        "Unable to open log directory because it does not exist.",
                        Enums.WheresMyCraftAt.LogMessageType.Error
                    );
                }
                else
                {
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = fullPath
                        }
                    );

                    Add("Opened log directory in explorer.", Enums.WheresMyCraftAt.LogMessageType.Info);
                }
            }

            ImGui.BeginChild("LogMessages", new Vector2(0, 0), ImGuiChildFlags.Border);

            lock (Locker)
            {
                foreach (var msg in MessagesList.Where(msg => msg != null))
                {
                    if (!Main.Settings.Debugging.LogMessageFilters[msg.LogType])
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

        Main.Settings.Debugging.LogWindow.Value = isOpen;
    }

    public static void Add(string msg, Enums.WheresMyCraftAt.LogMessageType messageType)
    {
        try
        {
            var color = LogMessageColors[messageType];

            if (Main.Settings.Debugging.PrintTopLeft)
            {
                DebugWindow.LogMsg(msg, Main.Settings.Debugging.PrintLingerTime, color);
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

    public static void SaveLog(List<string> input)
    {
        try
        {
            var fullPath = Path.Combine(Main.ConfigDirectory, "SavedLogs");
            Directory.CreateDirectory(fullPath);
            var filename = $"Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var fullFilePath = Path.Combine(fullPath, filename);
            File.WriteAllLines(fullFilePath, input);
            Add($"Successfully saved file to {fullFilePath}.", Enums.WheresMyCraftAt.LogMessageType.Info);
        }
        catch (Exception e)
        {
            var errorPath = Path.Combine(Main.ConfigDirectory, "SavedLogs");
            Add($"Error saving file to {errorPath}: {e.Message}", Enums.WheresMyCraftAt.LogMessageType.Error);
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