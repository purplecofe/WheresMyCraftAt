using ExileCore.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.CraftingSequence;

public class CraftingSequence
{
    public enum ConditionalCheckType
    {
        ModifyThenCheck,
        ConditionalCheckOnly
    }

    public enum ConditionGroup
    {
        AND,
        OR,
        NOT
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
        try
        {
            var fullPath = Path.Combine(Main.ConfigDirectory, filePath);
            var jsonString = JsonConvert.SerializeObject(input, Formatting.Indented);
            File.WriteAllText(fullPath, jsonString);
            Logging.Logging.Add($"Successfully saved file to {fullPath}.", Enums.WheresMyCraftAt.LogMessageType.Info);
        }
        catch (Exception e)
        {
            var fullPath = Path.Combine(Main.ConfigDirectory, filePath);

            Logging.Logging.Add(
                $"Error saving file to {fullPath}: {e.Message}",
                Enums.WheresMyCraftAt.LogMessageType.Error
            );
        }
    }

    public static void LoadFile(string fileName)
    {
        try
        {
            var fullPath = Path.Combine(Main.ConfigDirectory, $"{fileName}.json");
            var fileContent = File.ReadAllText(fullPath);

            Main.Settings.NonUserData.SelectedCraftingStepInputs
                = JsonConvert.DeserializeObject<List<CraftingStepInput>>(fileContent);
        }
        catch (Exception e)
        {
            var fullPath = Path.Combine(Main.ConfigDirectory, $"{fileName}.json");

            Logging.Logging.Add(
                $"Error loading file from {fullPath}: {e.Message}",
                Enums.WheresMyCraftAt.LogMessageType.Error
            );
        }
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
            Logging.Logging.Add(
                $"{Main.Name}: An error occurred while getting files: {e.Message}",
                Enums.WheresMyCraftAt.LogMessageType.Error
            );
        }

        return fileList;
    }

    public class CraftingStep
    {
        public Func<CancellationToken, SyncTask<bool>> Method { get; set; }
        public List<ConditionalChecksGroup> ConditionalCheckGroups { get; set; } = [];
        public ConditionalCheckType CheckType { get; set; } = ConditionalCheckType.ModifyThenCheck;
        public bool AutomaticSuccess { get; set; } = false;
        public SuccessAction SuccessAction { get; set; }
        public int SuccessActionStepIndex { get; set; }
        public FailureAction FailureAction { get; set; }
        public int FailureActionStepIndex { get; set; }
    }

    public class ConditionalChecksGroup
    {
        public ConditionGroup GroupType { get; set; } = ConditionGroup.AND;
        public int ConditionalsToBePassForSuccess { get; set; } = 1;

        public List<Func<CancellationToken, SyncTask<(bool result, bool isMatch)>>> ConditionalChecks { get; set; }
            = [];
    }

    public class CraftingStepInput
    {
        public string CurrencyItem { get; set; } = string.Empty;
        public bool AutomaticSuccess { get; set; } = false;
        public SuccessAction SuccessAction { get; set; } = SuccessAction.Continue;
        public int SuccessActionStepIndex { get; set; } = 1;
        public FailureAction FailureAction { get; set; } = FailureAction.Restart;
        public int FailureActionStepIndex { get; set; } = 1;
        public List<ConditionalGroup> ConditionalGroups { get; set; } = [];
        public ConditionalCheckType CheckType { get; set; } = ConditionalCheckType.ModifyThenCheck;
    }

    public class ConditionalGroup
    {
        public ConditionGroup GroupType { get; set; } = ConditionGroup.AND;
        public int ConditionalsToBePassForSuccess { get; set; } = 1;
        public List<ConditionalKeys> Conditionals { get; set; } = [];
    }

    public class ConditionalKeys
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}