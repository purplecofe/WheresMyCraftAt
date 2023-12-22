using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ExileCore;
using ImGuiNET;
using ItemFilterLibrary;
using WheresMyCraftAt.Handlers;
using static WheresMyCraftAt.CraftingSequence.CraftingSequence;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.CraftingSequence;

public static class CraftingSequenceMenu
{
    private const string DeletePopup = "Delete Confirmation";
    private const string OverwritePopup = "Overwrite Confirmation";
    private static string _fileSaveName = string.Empty;
    private static List<string> _files = [];
    private static string _selectedFileName = string.Empty;

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
            if (ImGui.CollapsingHeader($"STEP [{i}]##header{i}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                #region Step Settings

                var availableWidth = ImGui.GetContentRegionAvail().X * 0.65f;
                ImGui.Indent();
                ImGui.PopStyleColor(); // Always pop style color to avoid styling issues

                var dropdownWidth = availableWidth * 0.6f;
                var inputWidth = availableWidth * 0.4f;

                // Currency Item Input
                var currencyItem = stepInput.CurrencyItem;
                if (ImGui.InputTextWithHint($"Currency Item##{i}",
                        "Case Sensitive Currency BaseName \"Orb of Transmutation\"...", ref currencyItem, 100))
                    stepInput.CurrencyItem = currencyItem;

                // Automatic Success Checkbox
                var autoSuccess = stepInput.AutomaticSuccess;
                if (ImGui.Checkbox($"Automatic Success##{i}", ref autoSuccess))
                    stepInput.AutomaticSuccess = autoSuccess;

                // Check Timing Combo Box
                var checkTimingIndex = (int)stepInput.CheckTiming;
                if (ImGui.Combo($"Check Conditionals When##{i}", ref checkTimingIndex,
                        Enum.GetNames(typeof(ConditionalCheckTiming)), GetEnumLength<ConditionalCheckTiming>()))
                    stepInput.CheckTiming = (ConditionalCheckTiming)checkTimingIndex;

                // Success Action
                var successActionIndex = (int)stepInput.SuccessAction;
                if (stepInput.SuccessAction == SuccessAction.GoToStep)
                    ImGui.SetNextItemWidth(dropdownWidth);
                if (ImGui.Combo($"##SuccessAction{i}", ref successActionIndex, Enum.GetNames(typeof(SuccessAction)),
                        GetEnumLength<SuccessAction>()))
                    stepInput.SuccessAction = (SuccessAction)successActionIndex;

                if (stepInput.SuccessAction == SuccessAction.GoToStep)
                {
                    ImGui.SameLine();
                    var successActionStepIndex = stepInput.SuccessActionStepIndex;
                    ImGui.SetNextItemWidth(inputWidth);
                    if (ImGui.InputInt($"##SuccessStepIndex{i}", ref successActionStepIndex))
                        stepInput.SuccessActionStepIndex = successActionStepIndex;
                }

                ImGui.SameLine();
                ImGui.Text("On Success");

                // Hide additional settings if AutomaticSuccess is true
                if (!stepInput.AutomaticSuccess)
                {
                    // Failure Action
                    var failureActionIndex = (int)stepInput.FailureAction;
                    if (stepInput.FailureAction == FailureAction.GoToStep)
                        ImGui.SetNextItemWidth(dropdownWidth);
                    if (ImGui.Combo($"##FailureAction{i}", ref failureActionIndex, Enum.GetNames(typeof(FailureAction)),
                            GetEnumLength<FailureAction>()))
                        stepInput.FailureAction = (FailureAction)failureActionIndex;

                    if (stepInput.FailureAction == FailureAction.GoToStep)
                    {
                        ImGui.SameLine();
                        var failureActionStepIndex = stepInput.FailureActionStepIndex;
                        ImGui.SetNextItemWidth(inputWidth);
                        if (ImGui.InputInt($"##FailureStepIndex{i}", ref failureActionStepIndex))
                            stepInput.FailureActionStepIndex = failureActionStepIndex;
                    }

                    ImGui.SameLine();
                    ImGui.Text("On Failure");

                    // Manage Conditional Checks
                    if (ImGui.Button($"Add Conditional Check##{i}"))
                        stepInput.ConditionalCheckKeys.Add(""); // Add a new empty string to be filled out
                    ImGui.Indent();
                    var checksToRemove = new List<int>(); // Track checks to remove
                    for (var j = 0; j < stepInput.ConditionalCheckKeys.Count; j++)
                    {
                        if (ImGui.Button($"Remove##{i}_{j}"))
                        {
                            checksToRemove.Add(j); // Mark this check for removal
                            continue; // Skip the rest of the loop to avoid accessing a removed item
                        }

                        ImGui.SameLine();
                        var checkKey = stepInput.ConditionalCheckKeys[j];
                        if (ImGui.InputTextWithHint($"Condition [{j}]##{i}_{j}", "ItemFitlerLibrary filter string...",
                                ref checkKey,
                                1000)) stepInput.ConditionalCheckKeys[j] = checkKey; // Update the check key
                    }

                    foreach (var index in checksToRemove.OrderByDescending(j => j))
                        stepInput.ConditionalCheckKeys.RemoveAt(index); // Remove marked checks

                    ImGui.Unindent();
                }

                ImGui.Separator();

                if (ImGui.Button($"[+] Insert Step Below##{i}"))
                {
                    currentSteps.Insert(i + 1, new CraftingStepInput());
                    i++;
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

                #endregion Step Settings
            }
            else
            {
                ImGui.PopStyleColor();
            }

            ImGui.Unindent();
        }

        Main.Settings.SelectedCraftingStepInputs = currentSteps;

        if (ImGui.Button("[=] Add New Step")) Main.Settings.SelectedCraftingStepInputs.Add(new CraftingStepInput());
    }

    private static void DrawConfirmAndClear()
    {
        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.ButtonActive)); // Set the header color
        if (!ImGui.CollapsingHeader($"Confirm / Clear Steps##{Main.Name}Confirm / Clear Steps",
                ImGuiTreeNodeFlags.DefaultOpen)) return;

        ImGui.Indent();
        if (ImGui.Button("[+] Apply Steps"))
        {
            Main.SelectedCraftingSteps.Clear();
            foreach (var input in Main.Settings.SelectedCraftingStepInputs)
            {
                var newStep = new CraftingStep
                {
                    Method = async token =>
                        await ItemHandler.AsyncTryApplyOrbToSlot(Enums.WheresMyCraftAt.SpecialSlot.CurrencyTab,
                            input.CurrencyItem,
                            token),
                    CheckTiming = input.CheckTiming,
                    AutomaticSuccess = input.AutomaticSuccess,
                    SuccessAction = input.SuccessAction,
                    SuccessActionStepIndex = input.SuccessActionStepIndex,
                    FailureAction = input.FailureAction,
                    FailureActionStepIndex = input.FailureActionStepIndex,
                    ConditionalChecks = []
                };

                foreach (var checkKey in input.ConditionalCheckKeys)
                {
                    var filter = ItemFilter.LoadFromString(checkKey);
                    if (filter.Queries.Count == 0)
                    {
                        Logging.Logging.Add($"CraftingSequenceMenu: Failed to load filter from string: {checkKey}",
                            Enums.WheresMyCraftAt.LogMessageType.Error);
                        return; // No point going on from here.
                    }

                    newStep.ConditionalChecks.Add(() => { return FilterHandler.IsMatchingCondition(filter); });
                }

                Main.SelectedCraftingSteps.Add(newStep);
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("[x] Clear All"))
            ImGui.OpenPopup(DeletePopup);

        if (ShowButtonPopup(DeletePopup, ["Are you sure?", "STOOOOP"], out var clearSelectedIndex))
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
        if (!ImGui.CollapsingHeader($"Load / Save##{Main.Name}Load / Save", ImGuiTreeNodeFlags.DefaultOpen)) return;
        ImGui.Indent();
        ImGui.InputTextWithHint("##SaveAs", "File Path...", ref _fileSaveName, 100);
        ImGui.SameLine();
        if (ImGui.Button("Save To File"))
        {
            _files = GetFiles();
            if (_fileSaveName == string.Empty)
                DebugWindow.LogError($"{Main.Name}: File name must not be empty.", 30);
            else if (_files.Contains(_fileSaveName))
                ImGui.OpenPopup(OverwritePopup);
            else
                SaveFile(Main.Settings.SelectedCraftingStepInputs, $"{_fileSaveName}.json");
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
                }

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }

        ImGui.Separator();

        if (ImGui.Button("Open Build Folder"))
        {
            var configDir = Main.ConfigDirectory;
            var directoryToOpen = Directory.Exists(configDir);
            if (!directoryToOpen)
                DebugWindow.LogError($"{Main.Name}: Config directory does not exist.", 30);

            Process.Start("explorer.exe", configDir);
        }

        if (ShowButtonPopup(OverwritePopup, ["Are you sure?", "STOOOOP"], out var saveSelectedIndex))
            if (saveSelectedIndex == 0)
                SaveFile(Main.Settings.SelectedCraftingStepInputs, $"{_fileSaveName}.json");
        ImGui.Unindent();
    }

    public static bool ShowButtonPopup(string popupId, List<string> items, out int selectedIndex)
    {
        selectedIndex = -1;
        var isItemClicked = false;
        var showPopup = true;

        if (ImGui.BeginPopupModal(popupId, ref showPopup,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (ImGui.Button(items[i]))
                {
                    selectedIndex = i;
                    isItemClicked = true;
                    showPopup = false;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
            }

            ImGui.EndPopup();
        }

        return isItemClicked;
    }

    private static int GetEnumLength<T>()
    {
        return Enum.GetNames(typeof(T)).Length;
    }
}