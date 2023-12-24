using ImGuiNET;
using ItemFilterLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using WheresMyCraftAt.Handlers;
using static WheresMyCraftAt.CraftingSequence.CraftingSequence;
using static WheresMyCraftAt.Enums.WheresMyCraftAt;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.CraftingSequence;

public static class CraftingSequenceMenu
{
    private const string DeletePopup = "Delete Confirmation";
    private const string OverwritePopup = "Overwrite Confirmation";
    private const string FilterEditPopup = "Filter (Multi-Line)";

    // Load last saved for both on initialization as its less confusing
    private static string _fileSaveName = Main.Settings.CraftingSequenceLastSaved;
    private static string _selectedFileName = Main.Settings.CraftingSequenceLastSaved;

    private static List<string> _files = [];
    private static string tempCondValue = string.Empty;
    private static string condEditValue = string.Empty;

    public static void Draw()
    {
        DrawFileOptions();
        DrawConfirmAndClear();
        DrawCraftingStepInputs();
    }

    private static void DrawCraftingStepInputs()
    {
        var currentSteps = new List<CraftingStepInput>(Main.Settings.SelectedCraftingStepInputs);

        for (var i = 0; i < currentSteps.Count; i++)
        {
            var stepInput = currentSteps[i];

            // Use a colored, collapsible header for each step
            ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.ButtonActive)); // Set the header color

            if (ImGui.CollapsingHeader($"STEP [{i + 1}]##header{i}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                #region Step Settings

                var availableWidth = ImGui.GetContentRegionAvail().X * 0.65f;
                ImGui.Indent();
                ImGui.PopStyleColor(); // Always pop style color to avoid styling issues
                var dropdownWidth = availableWidth * 0.6f;
                var inputWidth = availableWidth * 0.4f;

                // Check Timing Combo Box
                var checkTimingIndex = (int)stepInput.CheckType;

                if (ImGui.Combo(
                        $" Method Type##{i}",
                        ref checkTimingIndex,
                        Enum.GetNames(typeof(ConditionalCheckType)),
                        GetEnumLength<ConditionalCheckType>()
                    ))
                    stepInput.CheckType = (ConditionalCheckType)checkTimingIndex;

                if (stepInput.CheckType != ConditionalCheckType.ConditionalCheckOnly)
                {
                    // Currency Item Input
                    var currencyItem = stepInput.CurrencyItem;

                    if (ImGui.InputTextWithHint(
                            $"Currency Item##{i}",
                            "Case Sensitive Currency BaseName \"Orb of Transmutation\"...",
                            ref currencyItem,
                            100
                        ))
                        stepInput.CurrencyItem = currencyItem;

                    // Automatic Success Checkbox
                    var autoSuccess = stepInput.AutomaticSuccess;

                    if (ImGui.Checkbox($"Automatic Success##{i}", ref autoSuccess))
                        stepInput.AutomaticSuccess = autoSuccess;
                }

                // Success Action
                var successActionIndex = (int)stepInput.SuccessAction;

                if (stepInput.SuccessAction == SuccessAction.GoToStep)
                    ImGui.SetNextItemWidth(dropdownWidth);

                if (ImGui.Combo(
                        $"##SuccessAction{i}",
                        ref successActionIndex,
                        Enum.GetNames(typeof(SuccessAction)),
                        GetEnumLength<SuccessAction>()
                    ))
                    stepInput.SuccessAction = (SuccessAction)successActionIndex;

                #region SuccessStepSelectorIndex

                if (stepInput.SuccessAction == SuccessAction.GoToStep)
                {
                    ImGui.SameLine();
                    var successActionStepIndex = stepInput.SuccessActionStepIndex;
                    ImGui.SetNextItemWidth(inputWidth);

                    // Generate step names, excluding the current step
                    var stepNames = new List<string>();

                    for (var step = 0; step < currentSteps.Count; step++)
                        if (step != i) // Exclude the current step
                            stepNames.Add($"STEP [{step + 1}]");

                    // Initialize dropdownIndex based on the successActionStepIndex
                    var dropdownIndex = successActionStepIndex >= i && successActionStepIndex < currentSteps.Count
                        ? successActionStepIndex - 1
                        : successActionStepIndex;

                    var comboItems = string.Join('\0', stepNames) + '\0';

                    if (ImGui.Combo($"##SuccessStepIndex{i}", ref dropdownIndex, comboItems, stepNames.Count))
                    {
                        // Adjust the selectedStepIndex based on the current step's position
                        var selectedStepIndex = dropdownIndex >= i
                            ? dropdownIndex + 1
                            : dropdownIndex;

                        stepInput.SuccessActionStepIndex = selectedStepIndex;
                    }
                }

                #endregion

                ImGui.SameLine();
                ImGui.Text("On Success");

                // Hide additional settings if AutomaticSuccess is true
                if (!stepInput.AutomaticSuccess)
                {
                    // Failure Action
                    var failureActionIndex = (int)stepInput.FailureAction;

                    if (stepInput.FailureAction == FailureAction.GoToStep)
                        ImGui.SetNextItemWidth(dropdownWidth);

                    if (ImGui.Combo(
                            $"##FailureAction{i}",
                            ref failureActionIndex,
                            Enum.GetNames(typeof(FailureAction)),
                            GetEnumLength<FailureAction>()
                        ))
                        stepInput.FailureAction = (FailureAction)failureActionIndex;

                    #region FailureStepSelectorIndex

                    if (stepInput.FailureAction == FailureAction.GoToStep)
                    {
                        ImGui.SameLine();
                        var failureActionStepIndex = stepInput.FailureActionStepIndex;
                        ImGui.SetNextItemWidth(inputWidth);

                        // Generate step names, excluding the current step
                        var stepNames = new List<string>();

                        for (var step = 0; step < currentSteps.Count; step++)
                            if (step != i) // Exclude the current step
                                stepNames.Add($"STEP [{step + 1}]");

                        // Initialize dropdownIndex based on the failureActionStepIndex
                        var dropdownIndex = failureActionStepIndex >= i && failureActionStepIndex < currentSteps.Count
                            ? failureActionStepIndex - 1
                            : failureActionStepIndex;

                        var comboItems = string.Join('\0', stepNames) + '\0';

                        if (ImGui.Combo($"##FailureStepIndex{i}", ref dropdownIndex, comboItems, stepNames.Count))
                        {
                            // Adjust the selectedStepIndex based on the current step's position
                            var selectedStepIndex = dropdownIndex >= i
                                ? dropdownIndex + 1
                                : dropdownIndex;

                            stepInput.FailureActionStepIndex = selectedStepIndex;
                        }
                    }

                    #endregion

                    ImGui.SameLine();
                    ImGui.Text("On Failure");

                    // Manage Conditional Checks
                    if (ImGui.Button($"Add Conditional Check##{i}"))
                        stepInput.Conditionals.Add(new ConditionalKeys()); // Add a new empty string to be filled out

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200);
                    var conditionalChecksTrue = stepInput.ConditionalsToBePassForSuccess;

                    if (ImGui.InputInt($"Req Checks to Pass##conditionalChecksTrue{i}", ref conditionalChecksTrue))
                    {
                        // Clamp the value between 1 and the number of conditional checks
                        conditionalChecksTrue = Math.Max(
                            1,
                            Math.Min(conditionalChecksTrue, stepInput.Conditionals.Count)
                        );

                        stepInput.ConditionalsToBePassForSuccess = conditionalChecksTrue;
                    }

                    ImGui.Indent();

                    // Generate a list of step names, excluding the current step for dropdown selection
                    var stepNamesForDropdown = new List<string>();

                    for (var step = 0; step < currentSteps.Count; step++)
                        if (step != i) // Exclude the current step
                            stepNamesForDropdown.Add($"STEP [{step + 1}]");

                    // Concatenate step names into a single string for the dropdown items
                    var dropdownItemsForCopy = string.Join('\0', stepNamesForDropdown) + '\0';
                    var currentStepIndex = -1; // Initialize to -1 to indicate no selection

                    // Create a dropdown for selecting a step to copy conditionals from
                    if (ImGui.Combo(
                            $"Copy Conditionals From##CopyConditionsFrom{i}",
                            ref currentStepIndex,
                            dropdownItemsForCopy,
                            stepNamesForDropdown.Count
                        ))
                        // Dropdown selection made, parse the selected step index
                        if (currentStepIndex >= 0 && currentStepIndex < stepNamesForDropdown.Count)
                        {
                            var selectedStepName = stepNamesForDropdown[currentStepIndex];

                            var parsedIndex = int.Parse(
                                selectedStepName.Substring(
                                    selectedStepName.IndexOf('[') + 1,
                                    selectedStepName.IndexOf(']') - selectedStepName.IndexOf('[') - 1
                                )
                            );

                            // Since step labels are 1-indexed (STEP [1], STEP [2], etc.), 
                            // subtract 1 to get the actual 0-indexed step
                            var selectedStepIndex = parsedIndex - 1;

                            // Assign conditionals from the selected step to the current step's conditionals
                            if (selectedStepIndex >= 0 && selectedStepIndex < currentSteps.Count)
                                stepInput.Conditionals = currentSteps[selectedStepIndex].Conditionals;
                        }

                    var checksToRemove = new List<int>(); // Track checks to remove

                    for (var j = 0; j < stepInput.Conditionals.Count; j++)
                    {
                        if (ImGui.Button($"Remove##{i}_{j}"))
                        {
                            checksToRemove.Add(j); // Mark this check for removal
                            continue;              // Skip the rest of the loop to avoid accessing a removed item
                        }

                        ImGui.SameLine();
                        var checkKey = stepInput.Conditionals[j].Name;

                        if (ImGui.InputTextWithHint($"##{i}_{j}", "Name of condition...", ref checkKey, 1000))
                            stepInput.Conditionals[j].Name = checkKey; // Update the check key

                        ImGui.SameLine();
                        var showPopup = true;

                        // Initialize both tempCondValue and condEditValue when opening the popup
                        if (ImGui.Button($"Edit##{i}_{j}"))
                        {
                            condEditValue = stepInput.Conditionals[j].Value;
                            tempCondValue = condEditValue;
                            ImGui.OpenPopup(FilterEditPopup + $"##conditionalEditPopup{i}_{j}");
                        }

                        ConditionValueEditPopup(showPopup, i, j, stepInput);
                    }

                    ImGui.Unindent();

                    foreach (var index in checksToRemove.OrderByDescending(j => j))
                    {
                        if (stepInput.Conditionals.Count >= stepInput.ConditionalsToBePassForSuccess &&
                            stepInput.ConditionalsToBePassForSuccess > 1)
                            stepInput.ConditionalsToBePassForSuccess--; // Decrement the required checks to pass

                        stepInput.Conditionals.RemoveAt(index); // Remove marked checks
                    }
                }

                ImGui.Separator();

                if (ImGui.Button($"[+] Insert Step Above##{i}"))
                {
                    // Manually initialize the Conditionals
                    currentSteps.Insert(i, new CraftingStepInput());
                    continue;
                }

                ImGui.SameLine();

                if (ImGui.Button($"[-] Remove This Step##{i}"))
                {
                    currentSteps.RemoveAt(i);
                    i--;
                    continue;
                }

                ImGui.Separator();
                ImGui.Unindent();

                #endregion Step Settings
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        Main.Settings.SelectedCraftingStepInputs = currentSteps;

        if (ImGui.Button("[=] Add New Step"))
            Main.Settings.SelectedCraftingStepInputs.Add(new CraftingStepInput());

        Main.Settings.CraftingSequenceLastSaved = _fileSaveName;
        Main.Settings.CraftingSequenceLastSelected = _selectedFileName;
    }

    private static void ConditionValueEditPopup(bool showPopup, int i, int j, CraftingStepInput stepInput)
    {
        if (!ImGui.BeginPopupModal(
                FilterEditPopup + $"##conditionalEditPopup{i}_{j}",
                ref showPopup,
                ImGuiWindowFlags.AlwaysAutoResize
            ))
            return;

        ImGui.InputTextMultiline(
            $"##text{i}_{j}",
            ref tempCondValue,
            15000,
            new Vector2(800, 600),
            ImGuiInputTextFlags.AllowTabInput
        );

        if (ImGui.Button("Save"))
        {
            stepInput.Conditionals[j].Value = tempCondValue;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();

        if (ImGui.Button("Revert"))
            tempCondValue = condEditValue;

        ImGui.SameLine();

        if (ImGui.Button("Close"))
            ImGui.CloseCurrentPopup();

        var conditionals = Main.Settings.SelectedCraftingStepInputs[i].Conditionals;

        var conditionalNames = conditionals.Select(
                                               (c, index) => string.IsNullOrEmpty(c.Name)
                                                   ? $"Unnamed Conditional {index + 1}"
                                                   : c.Name
                                           )
                                           .ToArray();

        var selectedIndex = -1;
        ImGui.SameLine();

        if (ImGui.Combo("Copy Conditional From", ref selectedIndex, conditionalNames, conditionalNames.Length))
            if (selectedIndex >= 0 && selectedIndex < conditionals.Count)
                tempCondValue = conditionals[selectedIndex].Value;

        ImGui.EndPopup();
    }

    private static void DrawConfirmAndClear()
    {
        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.ButtonActive)); // Set the header color

        if (!ImGui.CollapsingHeader(
                $"Confirm / Clear Steps##{Main.Name}Confirm / Clear Steps",
                ImGuiTreeNodeFlags.DefaultOpen
            ))
            return;

        ImGui.Indent();

        if (ImGui.Button("[+] Apply Steps"))
        {
            Main.SelectedCraftingSteps.Clear();

            foreach (var input in Main.Settings.SelectedCraftingStepInputs)
            {
                var newStep = new CraftingStep
                {
                    Method = async token => await ItemHandler.AsyncTryApplyOrbToSlot(
                        SpecialSlot.CurrencyTab,
                        input.CurrencyItem,
                        token
                    ),
                    CheckType = input.CheckType,
                    AutomaticSuccess = input.AutomaticSuccess,
                    SuccessAction = input.SuccessAction,
                    SuccessActionStepIndex = input.SuccessActionStepIndex,
                    FailureAction = input.FailureAction,
                    FailureActionStepIndex = input.FailureActionStepIndex,
                    ConditionalsToBePassForSuccess = input.ConditionalsToBePassForSuccess,
                    ConditionalChecks = []
                };

                foreach (var checkKey in input.Conditionals)
                {
                    if (input.AutomaticSuccess)
                        continue;

                    var filter = ItemFilter.LoadFromString(checkKey.Value);

                    if (filter.Queries.Count == 0)
                    {
                        Logging.Logging.Add(
                            $"CraftingSequenceMenu: Failed to load filter from string: {checkKey.Name}",
                            LogMessageType.Error
                        );

                        return; // No point going on from here.
                    }

                    newStep.ConditionalChecks.Add(() => FilterHandler.IsMatchingCondition(filter));
                }

                Main.SelectedCraftingSteps.Add(newStep);
            }
        }

        ImGui.SameLine();

        if (ImGui.Button("[x] Clear All"))
            ImGui.OpenPopup(DeletePopup);

        if (ShowButtonPopup(DeletePopup, ["Are you sure?", "STOP"], out var clearSelectedIndex))
            if (clearSelectedIndex == 0)
            {
                Main.Settings.SelectedCraftingStepInputs.Clear();
                Main.SelectedCraftingSteps.Clear();
            }

        ImGui.Separator();
        ImGui.Unindent();
    }

    private static void DrawFileOptions()
    {
        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.ButtonActive)); // Set the header color

        if (!ImGui.CollapsingHeader($"Load / Save##{Main.Name}Load / Save", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.Indent();
        ImGui.InputTextWithHint("##SaveAs", "File Path...", ref _fileSaveName, 100);
        ImGui.SameLine();

        if (ImGui.Button("Save To File"))
        {
            _files = GetFiles();

            // Sanitize the file name by replacing invalid characters
            foreach (var c in Path.GetInvalidFileNameChars())
                _fileSaveName = _fileSaveName.Replace(c, '_');

            if (_fileSaveName == string.Empty)
            {
                // Log error when the file name is empty
                Logging.Logging.Add("Attempted to save file without a name.", LogMessageType.Error);
            }
            else if (_files.Contains(_fileSaveName))
            {
                ImGui.OpenPopup(OverwritePopup);

                // Log info for overwrite confirmation
                Logging.Logging.Add(
                    $"File {_fileSaveName} already exists, requesting overwrite confirmation.",
                    LogMessageType.Info
                );
            }
            else
            {
                SaveFile(Main.Settings.SelectedCraftingStepInputs, $"{_fileSaveName}.json");
                // Log success when file is saved
                Logging.Logging.Add($"File {_fileSaveName}.json saved successfully.", LogMessageType.Info);
            }
        }

        ImGui.Separator();

        if (ImGui.BeginCombo("Load File##LoadFile", _selectedFileName))
        {
            _files = GetFiles();

            foreach (var fileName in _files)
            {
                var isSelected = _selectedFileName == fileName;

                if (ImGui.Selectable(fileName, isSelected))
                {
                    _selectedFileName = fileName;
                    _fileSaveName = fileName;
                    LoadFile(fileName);
                    // Log success when a file is loaded
                    Logging.Logging.Add($"File {fileName} loaded successfully.", LogMessageType.Info);
                }

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }

        ImGui.Separator();

        if (ImGui.Button("Open Crafting Template Folder"))
        {
            var configDir = Main.ConfigDirectory;
            var directoryToOpen = Directory.Exists(configDir);

            if (!directoryToOpen)
                // Log error when the config directory doesn't exist
                Logging.Logging.Add("Unable to open config directory because it does not exist.", LogMessageType.Error);

            if (configDir != null)
            {
                Process.Start("explorer.exe", configDir);
                // Log info when opening the config directory
                Logging.Logging.Add("Opened config directory in explorer.", LogMessageType.Info);
            }
        }

        if (ShowButtonPopup(OverwritePopup, ["Are you sure?", "STOP"], out var saveSelectedIndex))
            if (saveSelectedIndex == 0)
            {
                SaveFile(Main.Settings.SelectedCraftingStepInputs, $"{_fileSaveName}.json");

                // Log success when file is saved after overwrite confirmation
                Logging.Logging.Add(
                    $"File {_fileSaveName}.json saved successfully after overwrite confirmation.",
                    LogMessageType.Info
                );
            }

        ImGui.Unindent();
    }

    public static bool ShowButtonPopup(string popupId, List<string> items, out int selectedIndex)
    {
        selectedIndex = -1;
        var isItemClicked = false;
        var showPopup = true;

        if (!ImGui.BeginPopupModal(
                popupId,
                ref showPopup,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize
            ))
            return false;

        for (var i = 0; i < items.Count; i++)
        {
            if (ImGui.Button(items[i]))
            {
                selectedIndex = i;
                isItemClicked = true;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
        }

        ImGui.EndPopup();
        return isItemClicked;
    }

    private static int GetEnumLength<T>()
    {
        return Enum.GetNames(typeof(T)).Length;
    }
}