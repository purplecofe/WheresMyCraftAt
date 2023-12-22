using ExileCore;
using ImGuiNET;
using ItemFilterLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WheresMyCraftAt.Handlers;
using static WheresMyCraftAt.CraftingSequence.CraftingSequence;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.CraftingSequence
{
    public static class CraftingSequenceMenu
    {
        private const string DeletePopup = "Delete Confirmation";
        private const string OverwritePopup = "Overwrite Confirmation";
        private static string FileSaveName = string.Empty;
        private static List<string> Files = [];
        private static string selectedFileName = string.Empty;

        public static void Draw()
        {
            ImGui.Separator();
            ImGui.InputTextWithHint("##SaveAs", "File Path...", ref FileSaveName, 100);
            ImGui.SameLine();
            if (ImGui.Button("Save To File"))
            {
                Files = GetFiles();
                if (FileSaveName == string.Empty)
                    DebugWindow.LogError($"{Main.Name}: File name must not be empty.", 30);
                else if (Files.Contains(FileSaveName))
                    ImGui.OpenPopup(OverwritePopup);
                else
                    SaveFile(Main.Settings.SelectedCraftingStepInputs, $"{FileSaveName}.json");
            }

            ImGui.Separator();
            if (ImGui.BeginCombo("Load File##LoadFile", selectedFileName))
            {
                Files = GetFiles();
                foreach (var fileName in Files)
                {
                    bool isSelected = (selectedFileName == fileName);
                    if (ImGui.Selectable(fileName, isSelected))
                    {
                        selectedFileName = fileName;
                        LoadFile(fileName);
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            ImGui.Separator();
            if (ImGui.Button("Clear All"))
                ImGui.OpenPopup(DeletePopup);

            if (ShowButtonPopup(OverwritePopup, ["Are you sure?", "STOOOOP"], out var saveSelectedIndex))
            {
                if (saveSelectedIndex == 0)
                    SaveFile(Main.Settings.SelectedCraftingStepInputs, $"{FileSaveName}.json");
            }

            if (ShowButtonPopup(DeletePopup, ["Are you sure?", "STOOOOP"], out var clearSelectedIndex))
            {
                if (clearSelectedIndex == 0)
                    Main.Settings.SelectedCraftingStepInputs.Clear();
            }

            var currentSteps = new List<CraftingStepInput>(Main.Settings.SelectedCraftingStepInputs);

            for (int i = 0; i < currentSteps.Count; i++)
            {
                var stepInput = currentSteps[i];
                ImGui.Separator();

                // Use a colored, collapsible header for each step
                ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.ButtonActive)); // Set the header color
                if (ImGui.CollapsingHeader($"STEP [{i}]##header{i}", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    #region Step Settings

                    float availableWidth = ImGui.GetContentRegionAvail().X * 0.65f;
                    ImGui.Indent();
                    ImGui.PopStyleColor(); // Always pop style color to avoid styling issues

                    float dropdownWidth = availableWidth * 0.6f;
                    float inputWidth = availableWidth * 0.4f;

                    // Currency Item Input
                    string currencyItem = stepInput.CurrencyItem;
                    if (ImGui.InputTextWithHint($"Currency Item##{i}", "Case Sensitive Currency BaseName \"Orb of Transmutation\"...", ref currencyItem, 100))
                    {
                        stepInput.CurrencyItem = currencyItem;
                    }

                    // Automatic Success Checkbox
                    bool autoSuccess = stepInput.AutomaticSuccess;
                    if (ImGui.Checkbox($"Automatic Success##{i}", ref autoSuccess))
                    {
                        stepInput.AutomaticSuccess = autoSuccess;
                    }

                    // Check Timing Combo Box
                    int checkTimingIndex = (int)stepInput.CheckTiming;
                    if (ImGui.Combo($"Check Conditionals When##{i}", ref checkTimingIndex, Enum.GetNames(typeof(ConditionalCheckTiming)), GetEnumLength<ConditionalCheckTiming>()))
                    {
                        stepInput.CheckTiming = (ConditionalCheckTiming)checkTimingIndex;
                    }

                    // Success Action
                    int successActionIndex = (int)stepInput.SuccessAction;
                    if (stepInput.SuccessAction == SuccessAction.GoToStep)
                        ImGui.SetNextItemWidth(dropdownWidth);
                    if (ImGui.Combo($"##SuccessAction{i}", ref successActionIndex, Enum.GetNames(typeof(SuccessAction)), GetEnumLength<SuccessAction>()))
                        stepInput.SuccessAction = (SuccessAction)successActionIndex;

                    if (stepInput.SuccessAction == SuccessAction.GoToStep)
                    {
                        ImGui.SameLine();
                        int successActionStepIndex = stepInput.SuccessActionStepIndex;
                        ImGui.SetNextItemWidth(inputWidth);
                        if (ImGui.InputInt($"##SuccessStepIndex{i}", ref successActionStepIndex))
                            stepInput.SuccessActionStepIndex = successActionStepIndex;
                    }
                    ImGui.SameLine(); ImGui.Text("On Success");

                    // Hide additional settings if AutomaticSuccess is true
                    if (!stepInput.AutomaticSuccess)
                    {
                        // Failure Action
                        int failureActionIndex = (int)stepInput.FailureAction;
                        if (stepInput.FailureAction == FailureAction.GoToStep)
                            ImGui.SetNextItemWidth(dropdownWidth);
                        if (ImGui.Combo($"##FailureAction{i}", ref failureActionIndex, Enum.GetNames(typeof(FailureAction)), GetEnumLength<FailureAction>()))
                            stepInput.FailureAction = (FailureAction)failureActionIndex;

                        if (stepInput.FailureAction == FailureAction.GoToStep)
                        {
                            ImGui.SameLine();
                            int failureActionStepIndex = stepInput.FailureActionStepIndex;
                            ImGui.SetNextItemWidth(inputWidth);
                            if (ImGui.InputInt($"##FailureStepIndex{i}", ref failureActionStepIndex))
                                stepInput.FailureActionStepIndex = failureActionStepIndex;
                        }
                        ImGui.SameLine(); ImGui.Text("On Failure");

                        // Manage Conditional Checks
                        if (ImGui.Button($"Add Conditional Check##{i}"))
                        {
                            stepInput.ConditionalCheckKeys.Add(""); // Add a new empty string to be filled out
                        }
                        ImGui.Indent();
                        for (int j = 0; j < stepInput.ConditionalCheckKeys.Count; j++)
                        {
                            if (ImGui.Button($"Remove##{i}_{j}"))
                            {
                                stepInput.ConditionalCheckKeys.RemoveAt(j);
                                j--;
                            }

                            ImGui.SameLine();
                            string checkKey = stepInput.ConditionalCheckKeys[j];
                            if (ImGui.InputTextWithHint($"Condition [{j}]##{i}_{j}", "ItemFitlerLibrary filter string...", ref checkKey, 1000))
                            {
                                stepInput.ConditionalCheckKeys[j] = checkKey; // Update the check key
                            }
                        }

                        ImGui.Unindent();
                    }

                    #endregion Step Settings

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
                    ImGui.Unindent();
                }
                else
                {
                    ImGui.PopStyleColor();
                }
            }

            Main.Settings.SelectedCraftingStepInputs = currentSteps;

            if (ImGui.Button("[=] Add New Step"))
            {
                Main.Settings.SelectedCraftingStepInputs.Add(new CraftingStepInput());
            }
            ImGui.Separator();
            if (ImGui.Button("[+] Apply Steps"))
            {
                Main.SelectedCraftingSteps.Clear();
                foreach (var input in Main.Settings.SelectedCraftingStepInputs)
                {
                    CraftingStep newStep = new CraftingStep
                    {
                        Method = async (token) => await ItemHandler.AsyncTryApplyOrbToSlot(SpecialSlot.CurrencyTab, input.CurrencyItem, token),
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
                        ItemFilter filter = ItemFilter.LoadFromString(checkKey);
                        if (filter.Queries.Count == 0)
                        {
                            Logging.Add($"CraftingSequenceMenu: Failed to load filter from string: {checkKey}", LogMessageType.Error);
                            return; // No point going on from here.
                        }

                        newStep.ConditionalChecks.Add(() =>
                        {
                            return FilterHandler.IsMatchingCondition(filter);
                        });
                    }

                    Main.SelectedCraftingSteps.Add(newStep);
                }
            }
        }

        private static void LoadFile(string fileName)
        {
            var fileContent = File.ReadAllText(Path.Combine(Main.ConfigDirectory, $"{fileName}.json"));
            Main.Settings.SelectedCraftingStepInputs = JsonConvert.DeserializeObject<List<CraftingStepInput>>(fileContent);
        }

        public static bool ShowButtonPopup(string popupId, List<string> items, out int selectedIndex)
        {
            selectedIndex = -1;
            bool isItemClicked = false;
            bool showPopup = true;

            if (ImGui.BeginPopupModal(popupId, ref showPopup, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
            {
                for (int i = 0; i < items.Count; i++)
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

        private static int GetEnumLength<T>()
        {
            return Enum.GetNames(typeof(T)).Length;
        }
    }
}