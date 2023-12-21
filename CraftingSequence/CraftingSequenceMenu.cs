using ImGuiNET;
using System;
using System.Collections.Generic;
using WheresMyCraftAt.Handlers;
using static WheresMyCraftAt.CraftingSequence.CraftingSequence;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.CraftingSequence
{
    public static class CraftingSequenceMenu
    {
        public static void Draw()
        {
            if (ImGui.Button("Remove All"))
            {
                Main.Settings.SelectedCraftingStepInputs.Clear();
            }

            var currentSteps = new List<CraftingStepInput>(Main.Settings.SelectedCraftingStepInputs);

            for (int i = 0; i < currentSteps.Count; i++)
            {
                var stepInput = currentSteps[i];
                ImGui.Separator();

                // Use a colored, collapsible header for each step
                ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.ButtonActive)); // Set the header color
                if (ImGui.CollapsingHeader($"STEP [{i + 1}]##header{i}", ImGuiTreeNodeFlags.DefaultOpen))
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
                            if (ImGui.InputTextWithHint($"Condition [{j + 1}]##{i}_{j}", "ItemFitlerLibrary filter string...", ref checkKey, 1000))
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

            if (ImGui.Button("[+] Add New Step"))
            {
                Main.Settings.SelectedCraftingStepInputs.Add(new CraftingSequence.CraftingStepInput());
            }
            ImGui.Separator();
            if (ImGui.Button("[X] Apply Steps"))
            {
                Main.SelectedCraftingSteps.Clear();
                foreach (var input in Main.Settings.SelectedCraftingStepInputs)
                {
                    CraftingSequence.CraftingStep newStep = new CraftingSequence.CraftingStep
                    {
                        Method = async (token) => await ItemHandler.AsyncTryApplyOrbToSlot(SpecialSlot.CurrencyTab, input.CurrencyItem, token),
                        //ConditionalCheck = () => ItemHandler.IsItemRarityFromSpecialSlotCondition(SpecialSlot.CurrencyTab, input.ItemRarityWanted),
                        AutomaticSuccess = input.AutomaticSuccess,
                        SuccessAction = input.SuccessAction,
                        SuccessActionStepIndex = input.SuccessActionStepIndex - 1,
                        FailureAction = input.FailureAction,
                        FailureActionStepIndex = input.FailureActionStepIndex - 1
                    };
                    Main.SelectedCraftingSteps.Add(newStep);
                }
            }
        }

        private static int GetEnumLength<T>()
        {
            return Enum.GetNames(typeof(T)).Length;
        }
    }
}