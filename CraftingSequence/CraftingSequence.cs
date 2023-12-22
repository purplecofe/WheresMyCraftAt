using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ExileCore;
using ExileCore.Shared;
using Newtonsoft.Json;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.CraftingSequence;

public class CraftingSequence
{
    public enum ConditionalCheckTiming
    {
        BeforeMethodRun,
        AfterMethodRun
    }

    public enum FailureAction
    {
        RepeatStep,
        Restart,
        GoToStep
    }

    public enum SuccessAction
    {
        Continue,
        End,
        GoToStep
    }

    public static void SaveFile(List<CraftingStepInput> input, string filePath)
    {
        var fullPath = Path.Combine(Main.ConfigDirectory, filePath);
        var jsonString = JsonConvert.SerializeObject(input, Formatting.Indented);
        File.WriteAllText(fullPath, jsonString);
    }

    public static void LoadFile(string fileName)
    {
        var fileContent = File.ReadAllText(Path.Combine(Main.ConfigDirectory, $"{fileName}.json"));
        Main.Settings.SelectedCraftingStepInputs = JsonConvert.DeserializeObject<List<CraftingStepInput>>(fileContent);
    }

    public static List<string> GetFiles()
    {
        var fileList = new List<string>();

        try
        {
            var dir = new DirectoryInfo(Main.ConfigDirectory);
            fileList = dir.GetFiles().Select(file => Path.GetFileNameWithoutExtension(file.Name)).ToList();
        }
        catch (Exception e)
        {
            DebugWindow.LogError($"{Main.Name}: An error occurred while getting files: {e.Message}", 30);
        }

        return fileList;
    }

    public class CraftingStep
    {
        public Func<CancellationToken, SyncTask<bool>> Method { get; set; }
        public List<Func<bool>> ConditionalChecks { get; set; } = [];
        public ConditionalCheckTiming CheckTiming { get; set; } = ConditionalCheckTiming.AfterMethodRun;
        public bool AutomaticSuccess { get; set; } = false;
        public SuccessAction SuccessAction { get; set; }
        public int SuccessActionStepIndex { get; set; }
        public FailureAction FailureAction { get; set; }
        public int FailureActionStepIndex { get; set; }
    }

    public class CraftingStepInput
    {
        public string CurrencyItem { get; set; } = string.Empty;
        public bool AutomaticSuccess { get; set; } = false;
        public SuccessAction SuccessAction { get; set; } = SuccessAction.Continue;
        public int SuccessActionStepIndex { get; set; } = 1;
        public FailureAction FailureAction { get; set; } = FailureAction.Restart;
        public int FailureActionStepIndex { get; set; } = 1;
        public List<string> ConditionalCheckKeys { get; set; } = [];
        public List<string> ConditionalCheckKeysMultiLine { get; set; } = [];
        public ConditionalCheckTiming CheckTiming { get; set; } = ConditionalCheckTiming.AfterMethodRun;
    }
}