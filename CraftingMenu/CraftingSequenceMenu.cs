using ExileCore;
using ExileCore.Shared.Enums;
using ImGuiNET;
using ItemFilterLibrary;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WheresMyCraftAt.CraftingMenu.Styling;
using WheresMyCraftAt.Handlers;
using static WheresMyCraftAt.CraftingSequence.CraftingSequence;
using static WheresMyCraftAt.Enums.WheresMyCraftAt;
using static WheresMyCraftAt.WheresMyCraftAt;
using Vector2 = System.Numerics.Vector2;

namespace WheresMyCraftAt.CraftingMenu;

public static class CraftingSequenceMenu
{
    private const string DeletePopup = "Delete Confirmation";
    private const string OverwritePopup = "Overwrite Confirmation";

    private static EditorRecord Editor = new EditorRecord(-1, -1, -1);

    private static string _fileSaveName = Main.Settings.NonUserData.CraftingSequenceLastSaved;
    private static string _selectedFileName = Main.Settings.NonUserData.CraftingSequenceLastSaved;

    private static List<string> _files = [];
    private static string tempCondValue = string.Empty;
    private static string condEditValue = string.Empty;

    public static void Draw()
    {
        if (Main == null)
            return;

        if (!Main.Settings.Enable)
            ResetEditingIdentifiers();

        DrawFileOptions();

        if (Main.Settings.NonUserData.SelectedCraftingStepInputs.Count <= 0)
            Main.Settings.NonUserData.SelectedCraftingStepInputs.Add(new CraftingStepInput());

        DrawConfirmAndClear();
        DrawInstructions();
        DrawCraftingStepInputs();
    }

    private static int GetEnumLength<T>() => Enum.GetNames(typeof(T)).Length;

    private static void DrawCraftingStepInputs()
    {
        var currentSteps = new List<CraftingStepInput>(Main.Settings.NonUserData.SelectedCraftingStepInputs);

        for (var stepIndex = 0; stepIndex < currentSteps.Count; stepIndex++)
        {
            ImGui.PushID("Step_" + stepIndex);

            var dropTargetStart = ImGui.GetCursorPos();

            ImGui.Button("=");

            if (ImGui.BeginDragDropSource())
            {
                ImGuiHelpers.SetDragDropPayload("StepIndex", stepIndex);
                var headerText = $"STEP [{stepIndex + 1}] - ";
                headerText += currentSteps[stepIndex + 1].CheckType == ConditionalCheckType.ConditionalCheckOnly
                    ? "Check The Item"
                    : $"Use '{currentSteps[stepIndex].CurrencyItem}'";

                ImGui.Text($"Dragging Step '{headerText}'");
                ImGui.EndDragDropSource();
            }

            ImGui.PushStyleColor(ImGuiCol.Button, 0);

            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Drag me");

            ImGui.SameLine();

            DrawSingleCraftingStep(currentSteps, ref stepIndex);

            if (ImGuiHelpers.DrawAllColumnsBox("##DragTarget", dropTargetStart) && ImGui.BeginDragDropTarget())
            {
                var payload = ImGuiHelpers.AcceptDragDropPayload<int>("StepIndex");
                if (payload != null && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    var sourceIndex = payload.Value;
                    if (sourceIndex != stepIndex)
                    {
                        var draggedStep = currentSteps[sourceIndex];
                        currentSteps.RemoveAt(sourceIndex);
                        currentSteps.Insert(stepIndex, draggedStep);
                    }
                }

                ImGui.EndDragDropTarget();
            }

            ImGui.PopID();
        }

        Main.Settings.NonUserData.SelectedCraftingStepInputs = currentSteps;

        using (new ColorButton(Main.Settings.Styling.AdditionButtons.Normal, Main.Settings.Styling.AdditionButtons.Hovered,
                   Main.Settings.Styling.AdditionButtons.Active))
        {
            if (ImGui.Button("[=] Add New Step"))
            {
                ResetEditingIdentifiers();
                Main.Settings.NonUserData.SelectedCraftingStepInputs.Add(new CraftingStepInput());
            }
        }

        Main.Settings.NonUserData.CraftingSequenceLastSaved = _fileSaveName;
        Main.Settings.NonUserData.CraftingSequenceLastSelected = _selectedFileName;
    }

    private static void DrawSingleCraftingStep(List<CraftingStepInput> steps, ref int stepIndex)
    {
        ImGui.PushID(stepIndex);
        var currentStep = steps[stepIndex];
        var headerText = $"STEP [{stepIndex + 1}] - ";
        headerText += currentStep.CheckType == ConditionalCheckType.ConditionalCheckOnly ? "Check The Item" : $"Use '{currentStep.CurrencyItem}'";

        if (ImGui.CollapsingHeader(headerText, ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.BeginChild("ConditionalGroup", Vector2.Zero, ImGuiChildFlags.Border | ImGuiChildFlags.AutoResizeY);
            ImGui.Indent();

            DrawMethodTypeSection(currentStep);

            if (currentStep.CheckType != ConditionalCheckType.ConditionalCheckOnly)
                DrawCurrencyAndAutoSuccessSection(currentStep);

            DrawSuccessActionSection(currentStep, steps, stepIndex);

            if (!currentStep.AutomaticSuccess)
            {
                DrawFailureActionSection(currentStep, steps, stepIndex);
                DrawCopyConditionalGroupsSection(currentStep, steps, stepIndex);
                DrawAddConditionalGroupButton(currentStep);
                DrawConditionalGroups(currentStep, steps, stepIndex);
            }

            if (DrawStepManipulationControls(steps, ref stepIndex))
            {
                ImGui.Unindent();
                ImGui.EndChild();
                ImGui.PopID();
                return;
            }

            ImGui.Unindent();
            ImGui.EndChild();
        }

        ImGui.PopID();
    }

    private static void DrawMethodTypeSection(CraftingStepInput step)
    {
        var checkTypeIndex = (int)step.CheckType;
        if (ImGui.Combo(" Method Type", ref checkTypeIndex, Enum.GetNames(typeof(ConditionalCheckType)), GetEnumLength<ConditionalCheckType>()))
            step.CheckType = (ConditionalCheckType)checkTypeIndex;
    }

    private static void DrawCurrencyAndAutoSuccessSection(CraftingStepInput step)
    {
        var currencyItem = step.CurrencyItem;
        var availableWidth = ImGui.GetContentRegionAvail().X * 0.75f;
        ImGui.SetNextItemWidth(availableWidth);
        if (ImGui.InputTextWithHint("Currency Item", "Case Sensitive Currency BaseName \"Orb of Transmutation\"...", ref currencyItem, 100))
            step.CurrencyItem = currencyItem;

        var autoSuccess = step.AutomaticSuccess;
        if (ImGui.Checkbox("Automatic Success", ref autoSuccess))
            step.AutomaticSuccess = autoSuccess;
    }

    private static void DrawSuccessActionSection(CraftingStepInput step, List<CraftingStepInput> steps, int stepIndex)
    {
        var successActionIndex = (int)step.SuccessAction;
        if (ImGui.Combo("##SuccessAction", ref successActionIndex, Enum.GetNames(typeof(SuccessAction)), Enum.GetNames(typeof(SuccessAction)).Length))
        {
            step.SuccessAction = (SuccessAction)successActionIndex;
        }

        if (step.SuccessAction == SuccessAction.GoToStep)
        {
            ImGui.SameLine();
            var dropdownIndex = step.SuccessActionStepIndex;
            var stepNames = new List<string>();
            for (var s = 0; s < steps.Count; s++)
            {
                if (s != stepIndex)
                {
                    var description = $"Step {s + 1} - ";
                    description += steps[s].CheckType == ConditionalCheckType.ConditionalCheckOnly ? "Check The Item" : $"Use '{steps[s].CurrencyItem}'";

                    stepNames.Add(description);
                }
            }

            dropdownIndex = dropdownIndex >= stepIndex && dropdownIndex < steps.Count ? dropdownIndex - 1 : dropdownIndex;
            var comboItems = string.Join('\0', stepNames) + '\0';

            if (ImGui.Combo("##SuccessStepIndex", ref dropdownIndex, comboItems, stepNames.Count))
            {
                var selectedStepIndex = dropdownIndex >= stepIndex ? dropdownIndex + 1 : dropdownIndex;
                step.SuccessActionStepIndex = selectedStepIndex;
            }

            ImGui.SameLine();
            ImGui.Text("On Success");
        }
    }

    private static void DrawFailureActionSection(CraftingStepInput step, List<CraftingStepInput> steps, int stepIndex)
    {
        var failureActionIndex = (int)step.FailureAction;
        if (ImGui.Combo("##FailureAction", ref failureActionIndex, Enum.GetNames(typeof(FailureAction)), Enum.GetNames(typeof(FailureAction)).Length))
        {
            step.FailureAction = (FailureAction)failureActionIndex;
        }

        if (step.FailureAction == FailureAction.GoToStep)
        {
            ImGui.SameLine();
            var dropdownIndex = step.FailureActionStepIndex;
            var stepNames = new List<string>();
            for (var s = 0; s < steps.Count; s++)
            {
                if (s != stepIndex)
                {
                    var description = $"Step {s + 1} - ";
                    description += steps[s].CheckType == ConditionalCheckType.ConditionalCheckOnly ? "Check The Item" : $"Use '{steps[s].CurrencyItem}'";

                    stepNames.Add(description);
                }
            }

            dropdownIndex = dropdownIndex >= stepIndex && dropdownIndex < steps.Count ? dropdownIndex - 1 : dropdownIndex;
            var comboItems = string.Join('\0', stepNames) + '\0';

            if (ImGui.Combo("##FailureStepIndex", ref dropdownIndex, comboItems, stepNames.Count))
            {
                var selectedStep = dropdownIndex >= stepIndex ? dropdownIndex + 1 : dropdownIndex;
                step.FailureActionStepIndex = selectedStep;
            }

            ImGui.SameLine();
            ImGui.Text("On Failure");
        }
    }

    private static void DrawCopyConditionalGroupsSection(CraftingStepInput step, List<CraftingStepInput> steps, int stepIndex)
    {
        var stepsWithIndex = steps.Select((s, idx) => new
        {
            Step = s,
            Index = idx
        });

        var stepNamesForDropdown = stepsWithIndex.Where(x => x.Index != stepIndex).Select(x => $"STEP [{x.Index + 1}]").ToArray();

        if (stepNamesForDropdown.Length > 0)
        {
            var currentStepIndex = -1;
            var maxItemWidth = stepNamesForDropdown.Max(name => ImGui.CalcTextSize(name).X);
            ImGui.SetNextItemWidth(maxItemWidth + 60);

            if (ImGui.Combo("Copy Conditional Groups From", ref currentStepIndex, stepNamesForDropdown, stepNamesForDropdown.Length))
            {
                ResetEditingIdentifiers();

                if (currentStepIndex >= stepIndex)
                    currentStepIndex++;

                if (currentStepIndex >= 0 && currentStepIndex < steps.Count)
                {
                    var sourceStep = steps[currentStepIndex];
                    step.ConditionalGroups = sourceStep.ConditionalGroups.Select(group => new ConditionalGroup
                    {
                        GroupType = group.GroupType,
                        ConditionalsToBePassForSuccess = group.ConditionalsToBePassForSuccess,
                        Conditionals = group.Conditionals.Select(conditional => new ConditionalKeys
                        {
                            Name = conditional.Name,
                            Value = conditional.Value
                        }).ToList()
                    }).ToList();
                }
            }
        }
    }

    private static void DrawAddConditionalGroupButton(CraftingStepInput step)
    {
        using (new ColorButton(Main.Settings.Styling.AdditionButtons.Normal, Main.Settings.Styling.AdditionButtons.Hovered,
                   Main.Settings.Styling.AdditionButtons.Active))
        {
            if (ImGui.Button("Add Conditional Group"))
            {
                ResetEditingIdentifiers();
                step.ConditionalGroups.Add(new ConditionalGroup());
            }
        }
    }

    private static void DrawConditionalGroups(CraftingStepInput step, List<CraftingStepInput> steps, int stepIndex)
    {
        var groupsToRemove = new List<int>();

        for (var groupIndex = 0; groupIndex < step.ConditionalGroups.Count; groupIndex++)
        {
            ImGui.PushID(groupIndex);
            Color bgColor = step.ConditionalGroups[groupIndex].GroupType switch
            {
                ConditionGroup.AND => Main.Settings.Styling.ConditionGroupBackgrounds.And,
                ConditionGroup.OR => Main.Settings.Styling.ConditionGroupBackgrounds.Or,
                ConditionGroup.NOT => Main.Settings.Styling.ConditionGroupBackgrounds.Not,
                _ => Main.Settings.Styling.ConditionGroupBackgrounds.And
            };

            using (new ColorBackground((ImGuiCol.ChildBg, bgColor)))
            {
                ImGui.BeginChild("ConditionalGroup", Vector2.Zero, ImGuiChildFlags.Border | ImGuiChildFlags.AutoResizeY);

                var removeGroup = DrawConditionalGroupHeader(step, groupIndex);
                if (removeGroup)
                    groupsToRemove.Add(groupIndex);
                else
                    DrawConditionalChecks(step, stepIndex, groupIndex);

                ImGui.EndChild();
            }

            ImGui.PopID();
        }

        foreach (var idx in groupsToRemove.OrderByDescending(i => i))
            step.ConditionalGroups.RemoveAt(idx);
    }

    private static bool DrawConditionalGroupHeader(CraftingStepInput step, int groupIndex)
    {
        var removeRequested = false;

        using (new ColorButton(Main.Settings.Styling.RemovalButtons.Normal, Main.Settings.Styling.RemovalButtons.Hovered,
                   Main.Settings.Styling.RemovalButtons.Active))
        {
            if (ImGui.Button("Remove Group"))
                removeRequested = true;
        }

        ImGui.SameLine();
        if (groupIndex > 0)
        {
            if (ImGui.ArrowButton("Up", ImGuiDir.Up))
            {
                (step.ConditionalGroups[groupIndex - 1], step.ConditionalGroups[groupIndex])
                    = (step.ConditionalGroups[groupIndex], step.ConditionalGroups[groupIndex - 1]);
            }
        }

        ImGui.SameLine();
        if (groupIndex < step.ConditionalGroups.Count - 1)
        {
            if (ImGui.ArrowButton("Down", ImGuiDir.Down))
            {
                (step.ConditionalGroups[groupIndex + 1], step.ConditionalGroups[groupIndex])
                    = (step.ConditionalGroups[groupIndex], step.ConditionalGroups[groupIndex + 1]);
            }
        }

        ImGui.SameLine();
        var groupTypeIndex = (int)step.ConditionalGroups[groupIndex].GroupType;
        ImGui.SetNextItemWidth(120);
        if (ImGui.Combo(" Group Type", ref groupTypeIndex, Enum.GetNames(typeof(ConditionGroup)), Enum.GetNames(typeof(ConditionGroup)).Length))
            step.ConditionalGroups[groupIndex].GroupType = (ConditionGroup)groupTypeIndex;

        using (new ColorButton(Main.Settings.Styling.AdditionButtons.Normal, Main.Settings.Styling.AdditionButtons.Hovered,
                   Main.Settings.Styling.AdditionButtons.Active))
        {
            if (ImGui.Button("Add Conditional Check"))
            {
                ResetEditingIdentifiers();
                step.ConditionalGroups[groupIndex].Conditionals.Add(new ConditionalKeys());
            }
        }

        if (step.ConditionalGroups[groupIndex].GroupType != ConditionGroup.NOT)
        {
            ImGui.SameLine();
            var reqChecks = step.ConditionalGroups[groupIndex].ConditionalsToBePassForSuccess;
            if (ImGui.InputInt("Req Checks to Pass", ref reqChecks))
            {
                reqChecks = Math.Max(1, Math.Min(reqChecks, step.ConditionalGroups[groupIndex].Conditionals.Count));
                step.ConditionalGroups[groupIndex].ConditionalsToBePassForSuccess = reqChecks;
            }
        }

        return removeRequested;
    }

    private static void DrawConditionalChecks(CraftingStepInput step, int stepIndex, int groupIndex)
    {
        ImGui.Indent();
        var checksToRemove = new List<int>();

        for (var conditionalIndex = 0; conditionalIndex < step.ConditionalGroups[groupIndex].Conditionals.Count; conditionalIndex++)
        {
            ImGui.PushID(conditionalIndex);

            using (new ColorButton(Main.Settings.Styling.RemovalButtons.Normal, Main.Settings.Styling.RemovalButtons.Hovered,
                       Main.Settings.Styling.RemovalButtons.Active))
            {
                if (ImGui.Button("Remove"))
                {
                    ResetEditingIdentifiers();
                    checksToRemove.Add(conditionalIndex);
                    ImGui.PopID();
                    continue;
                }
            }

            ImGui.SameLine();
            var checkKey = step.ConditionalGroups[groupIndex].Conditionals[conditionalIndex].Name;
            var availableWidth = ImGui.GetContentRegionAvail().X * 0.75f;
            ImGui.SetNextItemWidth(availableWidth);
            if (ImGui.InputTextWithHint("##conditionName", "Name of condition...", ref checkKey, 1000))
                step.ConditionalGroups[groupIndex].Conditionals[conditionalIndex].Name = checkKey;

            ImGui.SameLine();

            var isEditing = IsCurrentEditorContext(groupIndex, stepIndex, conditionalIndex);
            var editString = isEditing ? "Editing" : "Edit";

            using (isEditing
                       ? new ColorButton(Main.Settings.Styling.AdditionButtons.Normal, Main.Settings.Styling.AdditionButtons.Hovered,
                           Main.Settings.Styling.AdditionButtons.Active)
                       : null)
            {
                if (ImGui.Button(editString))
                {
                    if (isEditing)
                    {
                        ResetEditingIdentifiers();
                    }
                    else
                    {
                        condEditValue = step.ConditionalGroups[groupIndex].Conditionals[conditionalIndex].Value;
                        tempCondValue = condEditValue;
                        Editor = new EditorRecord(groupIndex, stepIndex, conditionalIndex);
                    }
                }
            }

            if (isEditing)
                ConditionValueEditWindow(step, stepIndex, groupIndex, conditionalIndex);

            ImGui.PopID();
        }

        ImGui.Unindent();

        foreach (var idx in checksToRemove.OrderByDescending(i => i))
        {
            if (step.ConditionalGroups[groupIndex].Conditionals.Count >= step.ConditionalGroups[groupIndex].ConditionalsToBePassForSuccess &&
                step.ConditionalGroups[groupIndex].ConditionalsToBePassForSuccess > 1)
                step.ConditionalGroups[groupIndex].ConditionalsToBePassForSuccess--;

            step.ConditionalGroups[groupIndex].Conditionals.RemoveAt(idx);
        }
    }

    private static bool DrawStepManipulationControls(List<CraftingStepInput> steps, ref int stepIndex)
    {
        using (new ColorButton(Main.Settings.Styling.RemovalButtons.Normal, Main.Settings.Styling.RemovalButtons.Hovered,
                   Main.Settings.Styling.RemovalButtons.Active))
        {
            if (ImGui.Button("[-] Remove This Step"))
            {
                ResetEditingIdentifiers();
                steps.RemoveAt(stepIndex);
                if (stepIndex >= steps.Count)
                    stepIndex = Math.Max(0, steps.Count - 1);

                return true;
            }
        }

        return false;
    }

    private static void ConditionValueEditWindow(CraftingStepInput stepInput, int stepIndex, int groupIndex, int conditionalIndex)
    {
        if (Editor.StepIndex != stepIndex || Editor.GroupIndex != groupIndex || Editor.ConditionalIndex != conditionalIndex)
            return;

        if (!ImGui.Begin("Edit Conditional", ImGuiWindowFlags.None))
        {
            ImGui.End();
            return;
        }

        var conditionalName = Main.Settings.NonUserData.SelectedCraftingStepInputs[Editor.StepIndex].ConditionalGroups[Editor.GroupIndex]
            .Conditionals[Editor.ConditionalIndex].Name;

        ImGui.BulletText(
            $"Editing: STEP[{Editor.StepIndex + 1}] => Group[{Editor.GroupIndex + 1}] => Conditional[{(!string.IsNullOrEmpty(conditionalName) ? conditionalName : Editor.ConditionalIndex + 1)}]");

        if (ImGui.Button("Save"))
        {
            stepInput.ConditionalGroups[Editor.GroupIndex].Conditionals[Editor.ConditionalIndex].Value = tempCondValue;
            ResetEditingIdentifiers();
        }

        ImGui.SameLine();

        if (ImGui.Button("Revert"))
            tempCondValue = condEditValue;

        ImGui.SameLine();

        if (ImGui.Button("Close"))
            ResetEditingIdentifiers();

        var allConditionalsWithStepInfo = Main.Settings.NonUserData.SelectedCraftingStepInputs.SelectMany((s, sIdx) =>
            s.ConditionalGroups.SelectMany(grp => grp.Conditionals, (grp, cond) => new
            {
                sIdx,
                cond
            })).ToList();

        var conditionalNames = allConditionalsWithStepInfo.Select((c, i) =>
            "Step " + (c.sIdx + 1) + ": " + (string.IsNullOrEmpty(c.cond.Name) ? $"Unnamed Conditional {i + 1}" : c.cond.Name)).ToArray();

        var selectedIndex = -1;
        ImGui.SameLine();
        ImGui.SetNextItemWidth(300);

        if (ImGui.Combo("Copy Conditional From", ref selectedIndex, conditionalNames, conditionalNames.Length))
            if (selectedIndex >= 0 && selectedIndex < allConditionalsWithStepInfo.Count)
                tempCondValue = allConditionalsWithStepInfo[selectedIndex].cond.Value;

        ImGui.InputTextMultiline("##text_edit", ref tempCondValue, 15000, ImGui.GetContentRegionAvail(), ImGuiInputTextFlags.AllowTabInput);

        ImGui.End();
    }

    private static bool IsCurrentEditorContext(int groupIndex, int stepIndex, int conditionalIndex) =>
        Editor.StepIndex == stepIndex && Editor.GroupIndex == groupIndex && Editor.ConditionalIndex == conditionalIndex;

    private static void ResetEditingIdentifiers()
    {
        Editor = new EditorRecord(-1, -1, -1);
    }

    private static void DrawConfirmAndClear()
    {
        if (!ImGui.CollapsingHeader("Confirm / Clear Steps", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.Indent();

        if (ImGui.Button("[+] Apply Steps"))
        {
            Main.SelectedCraftingSteps.Clear();

            if (Main.Settings.RunOptions.CraftInventoryInsteadOfCurrencyTab)
                ApplyStepsForInventory();
            else
                ApplyStepsForCurrencyTab();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted("This includes inventory setup\nif an item was removed since last apply\nAPPLY AGAIN");
            ImGui.EndTooltip();
        }

        ImGui.SameLine();

        if (ImGui.Button("[x] Clear All"))
            ImGui.OpenPopup(DeletePopup);

        if (ShowButtonPopup(DeletePopup, ["Are you sure?", "STOP"], out var clearSelectedIndex))
            if (clearSelectedIndex == 0)
            {
                Main.Settings.NonUserData.SelectedCraftingStepInputs.Clear();
                Main.SelectedCraftingSteps.Clear();
                Main.Settings.NonUserData.SelectedCraftingStepInputs.Insert(0, new CraftingStepInput());
            }

        ImGui.Unindent();
    }

    private static void ApplyStepsForInventory()
    {
        var itemsInInventory = InventoryHandler.TryGetValidCraftingItemsFromAnInventory(InventorySlotE.MainInventory1).ToList();

        for (var col = 0; col < 12; col++)
        for (var row = 0; row < 5; row++)
        {
            var isValidAndSelected = itemsInInventory.Any(item => item.PosX == col && item.PosY == row) &&
                                     Main.Settings.RunOptions.InventoryCraftingSlots[row, col] == 1;

            if (!isValidAndSelected)
                continue;

            var newCraftingBase = new CraftingBase
            {
                CraftingSteps = [],
                CraftingPosition = new Vector2(col, row)
            };

            newCraftingBase.MethodReadInventoryItem
                = async token => await InventoryHandler.AsyncTryGetInventoryItemFromSlot(newCraftingBase.CraftingPosition, token);

            foreach (var input in Main.Settings.NonUserData.SelectedCraftingStepInputs)
            {
                var newStep = new CraftingStep
                {
                    Method = async token => await ItemHandler.AsyncTryApplyOrbToSlot(newCraftingBase.CraftingPosition, input.CurrencyItem, token),
                    CheckType = input.CheckType,
                    AutomaticSuccess = input.AutomaticSuccess,
                    SuccessAction = input.SuccessAction,
                    SuccessActionStepIndex = input.SuccessActionStepIndex,
                    FailureAction = input.FailureAction,
                    FailureActionStepIndex = input.FailureActionStepIndex,
                    ConditionalCheckGroups = []
                };

                foreach (var conditionGroup in input.ConditionalGroups)
                {
                    var newGroup = new ConditionalChecksGroup
                    {
                        GroupType = conditionGroup.GroupType,
                        ConditionalsToBePassForSuccess = conditionGroup.ConditionalsToBePassForSuccess,
                        ConditionalChecks = []
                    };

                    if (input.AutomaticSuccess)
                    {
                        newStep.ConditionalCheckGroups.Add(newGroup);
                        continue;
                    }

                    foreach (var checkKey in conditionGroup.Conditionals)
                    {
                        var filter = ItemFilter.LoadFromString(checkKey.Value);
                        if (filter.Queries.Count == 0)
                        {
                            Logging.Logging.LogMessage($"CraftingSequenceMenu: Failed to load filter from  for string: {checkKey.Name}", LogMessageType.Error);
                            return;
                        }

                        newGroup.ConditionalChecks.Add(async token =>
                        {
                            var resultTuple = await FilterHandler.AsyncIsMatchingCondition(filter, newCraftingBase.CraftingPosition, token);
                            return resultTuple;
                        });
                    }

                    newStep.ConditionalCheckGroups.Add(newGroup);
                }

                newCraftingBase.CraftingSteps.Add(newStep);
            }

            Main.SelectedCraftingSteps.Add(newCraftingBase);
        }

        Logging.Logging.LogMessage(
            $"CraftingSequenceMenu: {Main.SelectedCraftingSteps.Count} items added with a step count of {Main.SelectedCraftingSteps.FirstOrDefault()?.CraftingSteps.Count}",
            LogMessageType.Info);
    }

    private static void ApplyStepsForCurrencyTab()
    {
        var newCraftingBase = new CraftingBase
        {
            CraftingSteps = [],
            MethodReadStashItem = async token => await StashHandler.AsyncTryGetStashSpecialSlot(SpecialSlot.CurrencyTab, token)
        };

        foreach (var input in Main.Settings.NonUserData.SelectedCraftingStepInputs)
        {
            var newStep = new CraftingStep
            {
                Method = async token => await ItemHandler.AsyncTryApplyOrbToSpecialSlot(SpecialSlot.CurrencyTab, input.CurrencyItem, token),
                CheckType = input.CheckType,
                AutomaticSuccess = input.AutomaticSuccess,
                SuccessAction = input.SuccessAction,
                SuccessActionStepIndex = input.SuccessActionStepIndex,
                FailureAction = input.FailureAction,
                FailureActionStepIndex = input.FailureActionStepIndex,
                ConditionalCheckGroups = []
            };

            foreach (var conditionGroup in input.ConditionalGroups)
            {
                var newGroup = new ConditionalChecksGroup
                {
                    GroupType = conditionGroup.GroupType,
                    ConditionalsToBePassForSuccess = conditionGroup.ConditionalsToBePassForSuccess,
                    ConditionalChecks = []
                };

                if (input.AutomaticSuccess)
                {
                    newStep.ConditionalCheckGroups.Add(newGroup);
                    continue;
                }

                foreach (var checkKey in conditionGroup.Conditionals)
                {
                    var filter = ItemFilter.LoadFromString(checkKey.Value);
                    if (filter.Queries.Count == 0)
                    {
                        Logging.Logging.LogMessage($"CraftingSequenceMenu: Failed to load filter from  for string: {checkKey.Name}", LogMessageType.Error);
                        return;
                    }

                    newGroup.ConditionalChecks.Add(async token =>
                    {
                        var resultTuple = await FilterHandler.AsyncIsMatchingCondition(filter, SpecialSlot.CurrencyTab, token);
                        return resultTuple;
                    });
                }

                newStep.ConditionalCheckGroups.Add(newGroup);
            }

            newCraftingBase.CraftingSteps.Add(newStep);
        }

        Main.SelectedCraftingSteps.Add(newCraftingBase);
        Logging.Logging.LogMessage($"CraftingSequenceMenu: Currency Tab Item Added with a step count of {newCraftingBase.CraftingSteps.Count}",
            LogMessageType.Info);
    }

    private static void DrawInstructions()
    {
        if (!ImGui.CollapsingHeader("Selected Step Instructions"))
            return;

        ImGui.Indent();
        var steps = Main.Settings.NonUserData.SelectedCraftingStepInputs.ToList();

        for (var index = 0; index < steps.Count; index++)
        {
            var currentStep = steps[index];
            var childWindowTitle = currentStep.CheckType == ConditionalCheckType.ConditionalCheckOnly ? "Check the item" : $"Use '{steps[index].CurrencyItem}'";

            CreateStepChildWindow(currentStep, index, childWindowTitle);

            if (currentStep.AutomaticSuccess)
                HandleAutomaticSuccess(currentStep);
            else
                HandleManualSuccess(currentStep);

            ImGui.Unindent();
            ImGui.EndChild();
            ImGui.NewLine();
        }

        ImGui.Unindent();
    }

    private static void CreateStepChildWindow(CraftingStepInput currentStep, int index, string title)
    {
        ImGui.PushID(index);
        ImGui.BeginChild($"[{index + 1}] {title}", Vector2.Zero, ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.Border);
        ImGui.Text($"[{index + 1}] {title}");
        ImGui.Indent();
        if (title.StartsWith("Use") && !currentStep.AutomaticSuccess)
            ImGui.Text("Check the item");

        ImGui.PopID();
    }

    private static void HandleAutomaticSuccess(CraftingStepInput currentStep)
    {
        switch (currentStep.SuccessAction)
        {
            case SuccessAction.GoToStep:
                ImGui.Text($"Then, go to step {currentStep.SuccessActionStepIndex + 1}");
                break;
            case SuccessAction.Continue:
                ImGui.Text("Then continue");
                break;
            case SuccessAction.End:
                ImGui.Text("Then End");
                break;
        }
    }

    private static void HandleManualSuccess(CraftingStepInput currentStep)
    {
        foreach (var group in currentStep.ConditionalGroups.OrderBy(g => g.GroupType))
        {
            ImGui.Text($"{group.GroupType} {group.ConditionalsToBePassForSuccess} or more of the following conditions pass:");
            ImGui.Indent();
            foreach (var conditional in group.Conditionals)
            {
                ImGui.Bullet();
                ImGui.SameLine();
                ImGui.Text(conditional.Name);
            }

            ImGui.Unindent();
        }

        if (currentStep.SuccessAction == SuccessAction.GoToStep)
            ImGui.Text($"If so, go to step {currentStep.SuccessActionStepIndex + 1}");
        else if (currentStep.SuccessAction == SuccessAction.End)
            ImGui.Text("If so, the item is done!");

        switch (currentStep.FailureAction)
        {
            case FailureAction.GoToStep:
                ImGui.Text($"If not, go to step {currentStep.FailureActionStepIndex + 1}");
                break;
            case FailureAction.Restart:
                ImGui.Text("If not, restart from first step");
                break;
            case FailureAction.RepeatStep:
                ImGui.Text("If not, repeat this step");
                break;
        }
    }

    private static void DrawFileOptions()
    {
        if (!ImGui.CollapsingHeader("Load / Save", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.Indent();
        ImGui.InputTextWithHint("##SaveAs", "File Path...", ref _fileSaveName, 100);
        ImGui.SameLine();

        if (ImGui.Button("Save To File"))
        {
            _files = GetFiles();
            _fileSaveName = HelperHandler.CleanWindowsString(_fileSaveName);

            if (_fileSaveName == string.Empty)
            {
                Logging.Logging.LogMessage("Attempted to save file without a name.", LogMessageType.Error);
            }
            else if (_files.Contains(_fileSaveName))
            {
                ImGui.OpenPopup(OverwritePopup);
                Logging.Logging.LogMessage($"File {_fileSaveName} already exists, requesting overwrite confirmation.", LogMessageType.Info);
            }
            else
            {
                SaveFile(Main.Settings.NonUserData.SelectedCraftingStepInputs, $"{_fileSaveName}.json");
                Logging.Logging.LogMessage($"File {_fileSaveName}.json saved successfully.", LogMessageType.Info);
            }
        }

        ImGui.Separator();

        if (ImGui.BeginCombo("Load File", _selectedFileName))
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
                    Logging.Logging.LogMessage($"File {fileName} loaded successfully.", LogMessageType.Info);
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

            if (!Directory.Exists(configDir))
            {
                Logging.Logging.LogMessage("Unable to open config directory because it does not exist.", LogMessageType.Error);
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = configDir
                });

                Logging.Logging.LogMessage("Opened config directory in explorer.", LogMessageType.Info);
            }
        }

        if (ShowButtonPopup(OverwritePopup, ["Are you sure?", "STOP"], out var saveSelectedIndex) && saveSelectedIndex == 0)
        {
            SaveFile(Main.Settings.NonUserData.SelectedCraftingStepInputs, $"{_fileSaveName}.json");
            Logging.Logging.LogMessage($"File {_fileSaveName}.json saved successfully after overwrite confirmation.", LogMessageType.Info);
        }

        ImGui.Unindent();
    }

    public static bool ShowButtonPopup(string popupId, List<string> items, out int selectedIndex)
    {
        selectedIndex = -1;
        var isItemClicked = false;
        var showPopup = true;

        if (!ImGui.BeginPopupModal(popupId, ref showPopup, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
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

    private record EditorRecord(int GroupIndex, int StepIndex, int ConditionalIndex);
}