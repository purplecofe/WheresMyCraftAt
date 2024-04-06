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

    // Load last saved for both on initialization as its less confusing
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
            return;

        DrawConfirmAndClear();
        DrawInstructions();
        DrawCraftingStepInputs();
    }

    private static void DrawCraftingStepInputs()
    {
        var currentSteps = new List<CraftingStepInput>(Main.Settings.NonUserData.SelectedCraftingStepInputs);

        for (var stepIndex = 0; stepIndex < currentSteps.Count; stepIndex++)
        {
            ImGui.PushID(stepIndex);
            var currentStep = currentSteps[stepIndex];

            if (ImGui.CollapsingHeader($"STEP [{stepIndex + 1}]", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.BeginChild("ConditionalGroup", Vector2.Zero, ImGuiChildFlags.Border | ImGuiChildFlags.AutoResizeY);

                #region Step Settings

                ImGui.Indent();

                #region Method Type

                // Check Timing Combo Box
                var checkTimingIndex = (int)currentStep.CheckType;

                if (ImGui.Combo(" Method Type", ref checkTimingIndex, Enum.GetNames(typeof(ConditionalCheckType)), GetEnumLength<ConditionalCheckType>()))
                {
                    currentStep.CheckType = (ConditionalCheckType)checkTimingIndex;
                }

                #endregion

                #region NOT Check Only Settings

                if (currentStep.CheckType != ConditionalCheckType.ConditionalCheckOnly)
                {
                    // Currency Item Input
                    var currencyItem = currentStep.CurrencyItem;
                    var availableWidth = ImGui.GetContentRegionAvail().X * 0.75f;
                    ImGui.SetNextItemWidth(availableWidth);

                    if (ImGui.InputTextWithHint("Currency Item", "Case Sensitive Currency BaseName \"Orb of Transmutation\"...", ref currencyItem, 100))
                    {
                        currentStep.CurrencyItem = currencyItem;
                    }

                    // Automatic Success Checkbox
                    var autoSuccess = currentStep.AutomaticSuccess;

                    if (ImGui.Checkbox("Automatic Success", ref autoSuccess))
                    {
                        currentStep.AutomaticSuccess = autoSuccess;
                    }
                }

                #endregion

                #region Success Action

                // Success Action
                var successActionIndex = (int)currentStep.SuccessAction;

                if (ImGui.Combo("##SuccessAction", ref successActionIndex, Enum.GetNames(typeof(SuccessAction)), GetEnumLength<SuccessAction>()))
                {
                    currentStep.SuccessAction = (SuccessAction)successActionIndex;
                }

                #endregion

                #region SuccessStepSelectorIndex

                if (currentStep.SuccessAction == SuccessAction.GoToStep)
                {
                    ImGui.SameLine();
                    var successActionStepIndex = currentStep.SuccessActionStepIndex;

                    // Generate step names, excluding the current step
                    var stepNames = new List<string>();

                    for (var step = 0; step < currentSteps.Count; step++)
                        if (step != stepIndex) // Exclude the current step
                        {
                            stepNames.Add($"STEP [{step + 1}]");
                        }

                    // Initialize dropdownIndex based on the successActionStepIndex
                    var dropdownIndex = successActionStepIndex >= stepIndex && successActionStepIndex < currentSteps.Count ? successActionStepIndex - 1 : successActionStepIndex;

                    var comboItems = string.Join('\0', stepNames) + '\0';

                    if (ImGui.Combo("##SuccessStepIndex", ref dropdownIndex, comboItems, stepNames.Count))
                    {
                        // Adjust the selectedStepIndex based on the current step's position
                        var selectedStepIndex = dropdownIndex >= stepIndex ? dropdownIndex + 1 : dropdownIndex;
                        currentStep.SuccessActionStepIndex = selectedStepIndex;
                    }
                }

                ImGui.SameLine();
                ImGui.Text("On Success");

                #endregion

                #region NOT Automatic Success Items

                // Hide additional settings if AutomaticSuccess is true
                if (!currentStep.AutomaticSuccess)
                {
                    #region Failure Action

                    // Failure Action
                    var failureActionIndex = (int)currentStep.FailureAction;

                    if (ImGui.Combo("##FailureAction", ref failureActionIndex, Enum.GetNames(typeof(FailureAction)), GetEnumLength<FailureAction>()))
                    {
                        currentStep.FailureAction = (FailureAction)failureActionIndex;
                    }

                    #endregion

                    #region FailureStepSelectorIndex

                    if (currentStep.FailureAction == FailureAction.GoToStep)
                    {
                        ImGui.SameLine();
                        var failureActionStepIndex = currentStep.FailureActionStepIndex;

                        // Generate step names, excluding the current step
                        var stepNames = new List<string>();

                        for (var step = 0; step < currentSteps.Count; step++)
                            if (step != stepIndex) // Exclude the current step
                            {
                                stepNames.Add($"STEP [{step + 1}]");
                            }

                        // Initialize dropdownIndex based on the failureActionStepIndex
                        var dropdownIndex = failureActionStepIndex >= stepIndex && failureActionStepIndex < currentSteps.Count ? failureActionStepIndex - 1 : failureActionStepIndex;

                        var comboItems = string.Join('\0', stepNames) + '\0';

                        if (ImGui.Combo("##FailureStepIndex", ref dropdownIndex, comboItems, stepNames.Count))
                        {
                            // Adjust the selectedStepIndex based on the current step's position
                            var selectedStepIndex = dropdownIndex >= stepIndex ? dropdownIndex + 1 : dropdownIndex;
                            currentStep.FailureActionStepIndex = selectedStepIndex;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.Text("On Failure");

                    #endregion

                    #region Copy Conditions From Step X

                    // Ensure we're operating on a collection that supports LINQ's indexed Select
                    var stepsWithIndex = currentSteps.Select((step, index) => new
                    {
                        Step = step,
                        Index = index
                    });

                    var stepNamesForDropdown = stepsWithIndex.Where(stepWithIndex => stepWithIndex.Index != stepIndex).Select(stepWithIndex => $"STEP [{stepWithIndex.Index + 1}]").ToArray();

                    // Only show the dropdown if there are other steps to select
                    if (stepNamesForDropdown.Length > 0)
                    {
                        var currentStepIndex = -1;
                        var maxItemWidth = stepNamesForDropdown.Max(name => ImGui.CalcTextSize(name).X);
                        ImGui.SetNextItemWidth(maxItemWidth + 60);

                        if (ImGui.Combo("Copy Conditional Groups From", ref currentStepIndex, stepNamesForDropdown, stepNamesForDropdown.Length))
                        {
                            ResetEditingIdentifiers();
                            // Adjust index if necessary
                            if (currentStepIndex >= stepIndex)
                            {
                                currentStepIndex++;
                            }

                            // Ensure the index is within bounds before accessing
                            if (currentStepIndex >= 0 && currentStepIndex < currentSteps.Count)
                            {
                                var sourceStep = currentSteps[currentStepIndex];
                                var targetStep = currentSteps[stepIndex];

                                // Deep copy of conditional groups from source step to target step
                                targetStep.ConditionalGroups = sourceStep.ConditionalGroups.Select(group => new ConditionalGroup
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

                    #endregion

                    #region Add Conditional Group

                    using (new ColorButton(Main.Settings.Styling.AdditionButtons.Normal, Main.Settings.Styling.AdditionButtons.Hovered, Main.Settings.Styling.AdditionButtons.Active))
                    {
                        if (ImGui.Button("Add Conditional Group"))
                        {
                            ResetEditingIdentifiers();
                            currentStep.ConditionalGroups.Add(new ConditionalGroup());
                        }
                    }

                    #endregion

                    #region Render Conditional Groups

                    var groupsToRemove = new List<int>();

                    for (var groupIndex = 0; groupIndex < currentStep.ConditionalGroups.Count; groupIndex++)
                    {
                        ImGui.PushID(groupIndex);

                        Color bgColor = currentStep.ConditionalGroups[groupIndex].GroupType switch
                        {
                            ConditionGroup.AND => Main.Settings.Styling.ConditionGroupBackgrounds.And,
                            ConditionGroup.OR => Main.Settings.Styling.ConditionGroupBackgrounds.Or,
                            ConditionGroup.NOT => Main.Settings.Styling.ConditionGroupBackgrounds.Not
                        };

                        using (new ColorBackground((ImGuiCol.ChildBg, bgColor)))
                        {
                            ImGui.BeginChild("ConditionalGroup", Vector2.Zero, ImGuiChildFlags.Border | ImGuiChildFlags.AutoResizeY);

                            #region Conditional Group Header

                            #region Group Delete

                            using (new ColorButton(Main.Settings.Styling.RemovalButtons.Normal, Main.Settings.Styling.RemovalButtons.Hovered, Main.Settings.Styling.RemovalButtons.Active))
                            {
                                if (ImGui.Button("Remove Group"))
                                {
                                    groupsToRemove.Add(groupIndex);
                                }
                            }

                            #endregion

                            #region Group Type

                            ImGui.SameLine();

                            var groupTypeIndex = (int)currentStep.ConditionalGroups[groupIndex].GroupType;
                            ImGui.SetNextItemWidth(120);

                            if (ImGui.Combo(" Group Type", ref groupTypeIndex, Enum.GetNames(typeof(ConditionGroup)), GetEnumLength<ConditionGroup>()))
                            {
                                currentStep.ConditionalGroups[groupIndex].GroupType = (ConditionGroup)groupTypeIndex;
                            }

                            #endregion

                            #region Add Conditional Check

                            using (new ColorButton(Main.Settings.Styling.AdditionButtons.Normal, Main.Settings.Styling.AdditionButtons.Hovered, Main.Settings.Styling.AdditionButtons.Active))
                            {
                                if (ImGui.Button("Add Conditional Check"))
                                {
                                    ResetEditingIdentifiers();
                                    currentStep.ConditionalGroups[groupIndex].Conditionals.Add(new ConditionalKeys());
                                }
                            }

                            #endregion

                            #region Conditional Group modification

                            if (currentStep.ConditionalGroups[groupIndex].GroupType != ConditionGroup.NOT)
                            {
                                ImGui.SameLine();

                                var conditionalChecksTrue = currentStep.ConditionalGroups[groupIndex].ConditionalsToBePassForSuccess;

                                if (currentStep.ConditionalGroups[groupIndex].GroupType != ConditionGroup.NOT)
                                {
                                    if (ImGui.InputInt("Req Checks to Pass", ref conditionalChecksTrue))
                                    {
                                        conditionalChecksTrue = Math.Max(1, Math.Min(conditionalChecksTrue, currentStep.ConditionalGroups[groupIndex].Conditionals.Count));
                                        currentStep.ConditionalGroups[groupIndex].ConditionalsToBePassForSuccess = conditionalChecksTrue;
                                    }
                                }
                            }

                            #endregion

                            #endregion

                            #region Conditional Group modification

                            ImGui.Indent();
                            var checksToRemove = new List<int>(); // Track checks to remove

                            for (var conditionalIndex = 0; conditionalIndex < currentStep.ConditionalGroups[groupIndex].Conditionals.Count; conditionalIndex++)
                            {
                                ImGui.PushID(conditionalIndex);

                                using (new ColorButton(Main.Settings.Styling.RemovalButtons.Normal, Main.Settings.Styling.RemovalButtons.Hovered, Main.Settings.Styling.RemovalButtons.Active))
                                {
                                    if (ImGui.Button("Remove"))
                                    {
                                        ResetEditingIdentifiers();
                                        checksToRemove.Add(conditionalIndex);
                                        continue;
                                    }
                                }

                                ImGui.SameLine();
                                var checkKey = currentStep.ConditionalGroups[groupIndex].Conditionals[conditionalIndex].Name;
                                var availableWidth = ImGui.GetContentRegionAvail().X * 0.75f;
                                ImGui.SetNextItemWidth(availableWidth);

                                if (ImGui.InputTextWithHint("##conditionName", "Name of condition...", ref checkKey, 1000))
                                {
                                    currentStep.ConditionalGroups[groupIndex].Conditionals[conditionalIndex].Name = checkKey;
                                }

                                ImGui.SameLine();

                                #region Edit Button

                                var isEditing = IsCurrentEditorContext(groupIndex, stepIndex, conditionalIndex);
                                var editString = isEditing ? "Editing" : "Edit";

                                using (isEditing
                                           ? new ColorButton(Main.Settings.Styling.AdditionButtons.Normal, Main.Settings.Styling.AdditionButtons.Hovered, Main.Settings.Styling.AdditionButtons.Active)
                                           : null)
                                {
                                    if (ImGui.Button($"{editString}"))
                                    {
                                        if (isEditing)
                                        {
                                            ResetEditingIdentifiers();
                                        }
                                        else
                                        {
                                            condEditValue = currentStep.ConditionalGroups[groupIndex].Conditionals[conditionalIndex].Value;
                                            tempCondValue = condEditValue;
                                            Editor = new EditorRecord(groupIndex, stepIndex, conditionalIndex);
                                        }
                                    }
                                }

                                if (isEditing)
                                {
                                    ConditionValueEditWindow(currentStep, stepIndex, groupIndex, conditionalIndex);
                                }

                                #endregion

                                ImGui.PopID();
                            }

                            ImGui.Unindent();

                            foreach (var index in checksToRemove.OrderByDescending(j => j))
                            {
                                if (currentStep.ConditionalGroups[groupIndex].Conditionals.Count >= currentStep.ConditionalGroups[groupIndex].ConditionalsToBePassForSuccess &&
                                    currentStep.ConditionalGroups[groupIndex].ConditionalsToBePassForSuccess > 1)
                                {
                                    currentStep.ConditionalGroups[groupIndex].ConditionalsToBePassForSuccess--;
                                }
                                // Decrement the required checks to pass

                                currentStep.ConditionalGroups[groupIndex].Conditionals.RemoveAt(index); // Remove marked checks
                            }

                            #endregion

                            ImGui.EndChild();
                        }

                        ImGui.PopID();
                    }

                    // Remove the marked groups after the loop
                    foreach (var indexToRemove in groupsToRemove.OrderByDescending(i => i))
                        currentStep.ConditionalGroups.RemoveAt(indexToRemove);

                    #endregion
                }

                #endregion

                #region Insert Step Above

                using (new ColorButton(Main.Settings.Styling.AdditionButtons.Normal, Main.Settings.Styling.AdditionButtons.Hovered, Main.Settings.Styling.AdditionButtons.Active))
                {
                    if (ImGui.Button("[^] Insert Step Above"))
                    {
                        ResetEditingIdentifiers();
                        currentSteps.Insert(stepIndex, new CraftingStepInput());
                        continue;
                    }
                }

                #endregion

                #region Remove Current Step

                ImGui.SameLine();

                using (new ColorButton(Main.Settings.Styling.RemovalButtons.Normal, Main.Settings.Styling.RemovalButtons.Hovered, Main.Settings.Styling.RemovalButtons.Active))
                {
                    if (ImGui.Button("[-] Remove This Step"))
                    {
                        ResetEditingIdentifiers();
                        currentSteps.RemoveAt(stepIndex);
                        continue;
                    }
                }

                #endregion

                #region Insert Step Below

                if (stepIndex < currentSteps.Count - 1)
                {
                    ImGui.SameLine();

                    using (new ColorButton(Main.Settings.Styling.AdditionButtons.Normal, Main.Settings.Styling.AdditionButtons.Hovered, Main.Settings.Styling.AdditionButtons.Active))
                    {
                        if (ImGui.Button("[v] Insert Step Below"))
                        {
                            ResetEditingIdentifiers();
                            currentSteps.Insert(stepIndex + 1, new CraftingStepInput());
                            continue;
                        }
                    }
                }

                #endregion

                ImGui.Unindent();

                #endregion Step Settings

                ImGui.EndChild();
            }
            ImGui.PopID();
        }

        Main.Settings.NonUserData.SelectedCraftingStepInputs = currentSteps;

        #region Add New Step

        using (new ColorButton(Main.Settings.Styling.AdditionButtons.Normal, Main.Settings.Styling.AdditionButtons.Hovered, Main.Settings.Styling.AdditionButtons.Active))
        {
            if (ImGui.Button("[=] Add New Step"))
            {
                ResetEditingIdentifiers();
                Main.Settings.NonUserData.SelectedCraftingStepInputs.Add(new CraftingStepInput());
            }
        }

        #endregion

        Main.Settings.NonUserData.CraftingSequenceLastSaved = _fileSaveName;
        Main.Settings.NonUserData.CraftingSequenceLastSelected = _selectedFileName;
    }

    private static bool IsCurrentEditorContext(int groupIndex, int stepIndex, int conditionalIndex) =>
        Editor.StepIndex == stepIndex && Editor.GroupIndex == groupIndex && Editor.ConditionalIndex == conditionalIndex;

    private static void ConditionValueEditWindow(CraftingStepInput stepInput, int stepIndex, int groupIndex, int conditionalIndex)
    {
        if (Editor.StepIndex != stepIndex || Editor.GroupIndex != groupIndex || Editor.ConditionalIndex != conditionalIndex)
        {
            return;
        }

        if (!ImGui.Begin("Edit Conditional", ImGuiWindowFlags.None))
        {
            ImGui.End();
            return;
        }

        var conditionalName = Main.Settings.NonUserData.SelectedCraftingStepInputs[Editor.StepIndex].ConditionalGroups[Editor.GroupIndex].Conditionals[Editor.ConditionalIndex].Name;

        ImGui.BulletText(
            $"Editing: STEP[{Editor.StepIndex + 1}] => Group[{Editor.GroupIndex + 1}] => Conditional[{(!string.IsNullOrEmpty(conditionalName) ? conditionalName : Editor.ConditionalIndex + 1)}]");

        if (ImGui.Button("Save"))
        {
            stepInput.ConditionalGroups[Editor.GroupIndex].Conditionals[Editor.ConditionalIndex].Value = tempCondValue;
            ResetEditingIdentifiers();
        }

        ImGui.SameLine();

        if (ImGui.Button("Revert"))
        {
            tempCondValue = condEditValue;
        }

        ImGui.SameLine();

        if (ImGui.Button("Close"))
        {
            ResetEditingIdentifiers();
        }

        var allConditionalsWithStepInfo = Main.Settings.NonUserData.SelectedCraftingStepInputs.SelectMany((step, stepIndex) => step.ConditionalGroups.SelectMany(group => group.Conditionals,
            (group, conditional) => new
            {
                stepIndex,
                conditional
            })).ToList();

        var conditionalNames = allConditionalsWithStepInfo
            .Select((c, index) => $"Step {c.stepIndex + 1}: " + (string.IsNullOrEmpty(c.conditional.Name) ? $"Unnamed Conditional {index + 1}" : c.conditional.Name)).ToArray();

        var selectedIndex = -1;
        ImGui.SameLine();
        ImGui.SetNextItemWidth(300);

        if (ImGui.Combo("Copy Conditional From", ref selectedIndex, conditionalNames, conditionalNames.Length))
        {
            if (selectedIndex >= 0 && selectedIndex < allConditionalsWithStepInfo.Count)
            {
                tempCondValue = allConditionalsWithStepInfo[selectedIndex].conditional.Value;
            }
        }

        ImGui.InputTextMultiline("##text_edit", ref tempCondValue, 15000, ImGui.GetContentRegionAvail(), ImGuiInputTextFlags.AllowTabInput);

        ImGui.End();
    }

    private static void ResetEditingIdentifiers()
    {
        Editor = new EditorRecord(-1, -1, -1);
    }

    private static void DrawConfirmAndClear()
    {
        if (!ImGui.CollapsingHeader("Confirm / Clear Steps", ImGuiTreeNodeFlags.DefaultOpen))
        {
            return;
        }

        ImGui.Indent();

        if (ImGui.Button("[+] Apply Steps"))
        {
            Main.SelectedCraftingSteps.Clear();

            foreach (var input in Main.Settings.NonUserData.SelectedCraftingStepInputs)
            {
                var newStep = new CraftingStep
                {
                    Method = async token => await ItemHandler.AsyncTryApplyOrbToSlot(SpecialSlot.CurrencyTab, input.CurrencyItem, token),
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
                            Logging.Logging.Add($"CraftingSequenceMenu: Failed to load filter from  for string: {checkKey.Name}", LogMessageType.Error);

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

                Main.SelectedCraftingSteps.Add(newStep);
            }

            Logging.Logging.Add($"CraftingSequenceMenu: Steps added {Main.SelectedCraftingSteps.Count}", LogMessageType.Info);
        }

        ImGui.SameLine();

        if (ImGui.Button("[x] Clear All"))
        {
            ImGui.OpenPopup(DeletePopup);
        }

        if (ShowButtonPopup(DeletePopup, ["Are you sure?", "STOP"], out var clearSelectedIndex))
        {
            if (clearSelectedIndex == 0)
            {
                Main.Settings.NonUserData.SelectedCraftingStepInputs.Clear();
                Main.SelectedCraftingSteps.Clear();
            }
        }

        ImGui.Separator();
        ImGui.Unindent();
    }

    private static void DrawInstructions()
    {
        if (!ImGui.CollapsingHeader("Selected Step Instructions"))
        {
            return;
        }

        ImGui.Indent();
        var steps = Main.Settings.NonUserData.SelectedCraftingStepInputs.ToList();

        for (var index = 0; index < steps.Count; index++)
        {
            var currentStep = Main.Settings.NonUserData.SelectedCraftingStepInputs[index];

            var childWindowTitle = currentStep.CheckType == ConditionalCheckType.ConditionalCheckOnly ? "Check the item" : $"Use '{steps[index].CurrencyItem}'";

            CreateStepChildWindow(currentStep, index, childWindowTitle);

            if (currentStep.AutomaticSuccess)
            {
                HandleAutomaticSuccess(currentStep);
            }
            else
            {
                HandleManualSuccess(currentStep);
            }

            ImGui.Unindent();
            ImGui.EndChild();
            ImGui.NewLine();
        }

        // End of indented Section
        ImGui.Separator();
        ImGui.Unindent();
        return;

        void CreateStepChildWindow(CraftingStepInput currentStep, int index, string title)
        {
            ImGui.PushID(index);
            ImGui.BeginChild($"[{index + 1}] {title}", Vector2.Zero, ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.Border);

            ImGui.Text($"[{index + 1}] {title}");
            ImGui.Indent();

            // Add "Check the item" text if the title starts with "Use"
            if (title.StartsWith("Use") && !currentStep.AutomaticSuccess)
            {
                ImGui.Text("Check the item");
            }

            ImGui.PopID();
        }

        // Function to handle the display for steps with automatic success
        void HandleAutomaticSuccess(CraftingStepInput currentStep)
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

        // Function to handle the display for steps requiring manual success evaluation
        void HandleManualSuccess(CraftingStepInput currentStep)
        {
            foreach (var conditionalGroup in currentStep.ConditionalGroups.OrderBy(group => group.GroupType))
            {
                // Use a different symbol or format for the header line
                ImGui.Text($"{conditionalGroup.GroupType} {conditionalGroup.ConditionalsToBePassForSuccess} or more of the following conditions pass:");

                ImGui.Indent();

                // Use a uniform symbol for each condition
                foreach (var conditional in conditionalGroup.Conditionals)
                {
                    ImGui.Bullet();
                    ImGui.SameLine();
                    ImGui.Text(conditional.Name);
                }

                ImGui.Unindent();
            }

            if (currentStep.SuccessAction == SuccessAction.GoToStep)
            {
                ImGui.Text($"If so, go to step {currentStep.SuccessActionStepIndex + 1}");
            }
            else if (currentStep.SuccessAction == SuccessAction.End)
            {
                ImGui.Text("If so, the item is done!");
            }

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
    }

    private static void DrawFileOptions()
    {
        if (!ImGui.CollapsingHeader("Load / Save", ImGuiTreeNodeFlags.DefaultOpen))
        {
            return;
        }

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
                Logging.Logging.Add($"File {_fileSaveName} already exists, requesting overwrite confirmation.", LogMessageType.Info);
            }
            else
            {
                SaveFile(Main.Settings.NonUserData.SelectedCraftingStepInputs, $"{_fileSaveName}.json");
                // Log success when file is saved
                Logging.Logging.Add($"File {_fileSaveName}.json saved successfully.", LogMessageType.Info);
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
                    // Log success when a file is loaded
                    Logging.Logging.Add($"File {fileName} loaded successfully.", LogMessageType.Info);
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }

        ImGui.Separator();

        if (ImGui.Button("Open Crafting Template Folder"))
        {
            var configDir = Main.ConfigDirectory;

            if (!Directory.Exists(configDir))
            {
                Logging.Logging.Add("Unable to open config directory because it does not exist.", LogMessageType.Error);
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = configDir
                });

                Logging.Logging.Add("Opened config directory in explorer.", LogMessageType.Info);
            }
        }

        if (ShowButtonPopup(OverwritePopup, ["Are you sure?", "STOP"], out var saveSelectedIndex))
        {
            if (saveSelectedIndex == 0)
            {
                SaveFile(Main.Settings.NonUserData.SelectedCraftingStepInputs, $"{_fileSaveName}.json");

                // Log success when file is saved after overwrite confirmation
                Logging.Logging.Add($"File {_fileSaveName}.json saved successfully after overwrite confirmation.", LogMessageType.Info);
            }
        }

        ImGui.Unindent();
    }

    public static bool ShowButtonPopup(string popupId, List<string> items, out int selectedIndex)
    {
        selectedIndex = -1;
        var isItemClicked = false;
        var showPopup = true;

        if (!ImGui.BeginPopupModal(popupId, ref showPopup, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
        {
            return false;
        }

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

    private static int GetEnumLength<T>() => Enum.GetNames(typeof(T)).Length;
    private record EditorRecord(int GroupIndex, int StepIndex, int ConditionalIndex);
}