using ExileCore;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using static WheresMyCraftAt.CraftingSequence.CraftingSequence;
using static WheresMyCraftAt.Enums.WheresMyCraftAt;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Logging;

public static class Logging
{
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

            if (ImGui.TreeNode("Configure Logging Filters And Their Colors"))
            {
                foreach (var logMessageType in logMessageTypes)
                {
                    var logSetting = Main.Settings.Debugging.LogMessageFilters[logMessageType];
                    var colorToVector4 = logSetting.color.ToImguiVec4();

                    ImGui.ColorEdit4(
                        $"##ColorPicker_{logMessageType}",
                        ref colorToVector4,
                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreviewHalf |
                        ImGuiColorEditFlags.AlphaBar
                    );

                    logSetting.color = colorToVector4.ToSharpColor();
                    ImGui.SameLine();
                    ImGui.Checkbox(logMessageType.ToString(), ref logSetting.enabled);
                    Main.Settings.Debugging.LogMessageFilters[logMessageType] = logSetting;
                }

                ImGui.TreePop();
            }

            if (ImGui.Button("Save Log"))
            {
                List<string> stringList;

                lock (Locker)
                {
                    stringList = MessagesList
                                 .Where(
                                     msg => msg != null &&
                                            Main.Settings.Debugging.LogMessageFilters[msg.LogType].enabled
                                 ).Select(msg => $"{msg.Time.ToLongTimeString()}: {msg.Msg}").ToList();
                }

                SaveLog(stringList);
            }

            ImGui.SameLine();

            if (ImGui.Button("Open Log Folder"))
            {
                var fullPath = Path.Combine(Main.ConfigDirectory, "SavedLogs");

                if (!Directory.Exists(fullPath))
                {
                    Add("Unable to open log directory because it does not exist.", LogMessageType.Error);
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

                    Add("Opened log directory in explorer.", LogMessageType.Info);
                }
            }

            ImGui.BeginChild("LogMessages", new Vector2(0, 0), ImGuiChildFlags.Border);

            lock (Locker)
            {
                foreach (var msg in MessagesList.Where(msg => msg != null))
                {
                    var logType = Main.Settings.Debugging.LogMessageFilters[msg.LogType];

                    if (!logType.enabled)
                    {
                        continue;
                    }

                    ImGui.PushStyleColor(ImGuiCol.Text, logType.color.ToImguiVec4());
                    ImGui.TextUnformatted($"{msg.Time.ToLongTimeString()}: {msg.Msg}");
                    ImGui.PopStyleColor();
                }
            }

            ImGui.EndChild();
            ImGui.End();
        }

        Main.Settings.Debugging.LogWindow.Value = isOpen;
    }

    public static void Add(string msg, LogMessageType messageType)
    {
        try
        {
            var color = Main.Settings.Debugging.LogMessageFilters[messageType].color;

            if (Main.Settings.Debugging.PrintTopLeft)
            {
                DebugWindow.LogMsg(msg, Main.Settings.Debugging.PrintLingerTime, color);
            }

            var debugMsgDescription = new DebugMsgDescription
            {
                Msg = msg,
                Time = DateTime.Now,
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
            Add($"Successfully saved file to {fullFilePath}.", LogMessageType.Info);
        }
        catch (Exception e)
        {
            var errorPath = Path.Combine(Main.ConfigDirectory, "SavedLogs");
            Add($"Error saving file to {errorPath}: {e.Message}", LogMessageType.Error);
        }
    }

    public static void LogEndCraftingStats()
    {
        const LogMessageType messageType = LogMessageType.EndSessionStats;
        Add("-----------", messageType);
        Add("Total Items Applied Successfully:", messageType);

        foreach (var itemUsed in Main.CurrentOperationUsedItemsList)
            Add($"{itemUsed.Key}: {itemUsed.Value}", messageType);

        Add("-----------", messageType);
        Add("Total Steps Run:", LogMessageType.EndSessionStats);
        var sortedStepCountList = Main.CurrentOperationStepCountList.OrderBy(x => x.Key);

        foreach (var step in sortedStepCountList)
        {
            var stepIndexInputs = Main.Settings.NonUserData.SelectedCraftingStepInputs[step.Key];

            var stepAction = stepIndexInputs.CheckType == ConditionalCheckType.ConditionalCheckOnly ? "Check the item"
                : $"Use '{stepIndexInputs.CurrencyItem}'";

            Add(
                $"STEP [{step.Key + 1}] {stepAction}: (pass:{step.Value.passCount}, fail:{step.Value.failCount}, total:{step.Value.totalCount})",
                messageType
            );
        }

        Add("-----------", LogMessageType.EndSessionStats);
    }

    public class DebugMsgDescription
    {
        public string Msg { get; init; }
        public DateTime Time { get; init; }
        public LogMessageType LogType { get; init; }
    }
}