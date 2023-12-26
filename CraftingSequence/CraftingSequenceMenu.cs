using ExileCore.Shared.Helpers;
using ImGuiNET;
using ItemFilterLibrary;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WheresMyCraftAt.Handlers;
using static WheresMyCraftAt.CraftingSequence.CraftingSequence;
using static WheresMyCraftAt.Enums.WheresMyCraftAt;
using static WheresMyCraftAt.WheresMyCraftAt;
using Vector2 = System.Numerics.Vector2;

namespace WheresMyCraftAt.CraftingSequence;

public class ButtonStyle(Color normalColor, Color hoveredColor, Color activeColor)
{
    public Color NormalColor { get; set; } = normalColor;
    public Color HoveredColor { get; set; } = hoveredColor;
    public Color ActiveColor { get; set; } = activeColor;
}

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

    private static readonly ButtonStyle redButtonStyle = new(
        new Color(250, 66, 66, 102), // Normal
        new Color(250, 66, 66, 255), // Hovered
        new Color(250, 15, 15, 255)  // Active
    );

    private static readonly ButtonStyle greenButtonStyle = new(
        new Color(66, 250, 66, 102), // Normal
        new Color(66, 250, 66, 150), // Hovered
        new Color(15, 250, 15, 200)  // Active
    );

    public static void Draw()
    {
        DrawFileOptions();
        DrawConfirmAndClear();
        DrawInstructions();
        DrawCraftingStepInputs();
    }

    private static void DrawCraftingStepInputs()
    {
        var currentSteps = new List<CraftingStepInput>(Main.Settings.SelectedCraftingStepInputs);

        for (var stepIndex = 0; stepIndex < currentSteps.Count; stepIndex++)
        {
            var currentStep = currentSteps[stepIndex];

            if (!ImGui.CollapsingHeader($"STEP [{stepIndex + 1}]##header{stepIndex}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                continue;
            }

            ImGui.BeginChild(
                $"##conditionalGroup_{stepIndex}",
                Vector2.Zero,
                ImGuiChildFlags.Border | ImGuiChildFlags.AutoResizeY
            );

            #region Step Settings

            ImGui.Indent();

            #region Method Type

            // Check Timing Combo Box
            var checkTimingIndex = (int)currentStep.CheckType;

            if (ImGui.Combo(
                    $" Method Type##{stepIndex}",
                    ref checkTimingIndex,
                    Enum.GetNames(typeof(ConditionalCheckType)),
                    GetEnumLength<ConditionalCheckType>()
                ))
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

                if (ImGui.InputTextWithHint(
                        $"Currency Item##{stepIndex}",
                        "Case Sensitive Currency BaseName \"Orb of Transmutation\"...",
                        ref currencyItem,
                        100
                    ))
                {
                    currentStep.CurrencyItem = currencyItem;
                }

                // Automatic Success Checkbox
                var autoSuccess = currentStep.AutomaticSuccess;

                if (ImGui.Checkbox($"Automatic Success##{stepIndex}", ref autoSuccess))
                {
                    currentStep.AutomaticSuccess = autoSuccess;
                }
            }

            #endregion

            #region Success Action

            // Success Action
            var successActionIndex = (int)currentStep.SuccessAction;

            if (ImGui.Combo(
                    $"##SuccessAction{stepIndex}",
                    ref successActionIndex,
                    Enum.GetNames(typeof(SuccessAction)),
                    GetEnumLength<SuccessAction>()
                ))
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
                var dropdownIndex = successActionStepIndex >= stepIndex && successActionStepIndex < currentSteps.Count
                    ? successActionStepIndex - 1 : successActionStepIndex;

                var comboItems = string.Join('\0', stepNames) + '\0';

                if (ImGui.Combo($"##SuccessStepIndex{stepIndex}", ref dropdownIndex, comboItems, stepNames.Count))
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

                if (ImGui.Combo(
                        $"##FailureAction{stepIndex}",
                        ref failureActionIndex,
                        Enum.GetNames(typeof(FailureAction)),
                        GetEnumLength<FailureAction>()
                    ))
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
                    var dropdownIndex
                        = failureActionStepIndex >= stepIndex && failureActionStepIndex < currentSteps.Count
                            ? failureActionStepIndex - 1 : failureActionStepIndex;

                    var comboItems = string.Join('\0', stepNames) + '\0';

                    if (ImGui.Combo($"##FailureStepIndex{stepIndex}", ref dropdownIndex, comboItems, stepNames.Count))
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

                // Generate a list of step names, excluding the current step for dropdown selection
                var stepNamesForDropdown = new List<string>();

                for (var step = 0; step < currentSteps.Count; step++)
                    if (step != stepIndex) // Exclude the current step
                    {
                        stepNamesForDropdown.Add($"STEP [{step + 1}]");
                    }

                // Concatenate step names into a single string for the dropdown items
                var dropdownItemsForCopy = string.Join('\0', stepNamesForDropdown) + '\0';
                var currentStepIndex = -1; // Initialize to -1 to indicate no selection

                // Create a dropdown for selecting a step to copy conditionals from
                if (ImGui.Combo(
                        $"Copy ConditionalGroups From##CopyConditionsFrom{stepIndex}",
                        ref currentStepIndex,
                        dropdownItemsForCopy,
                        stepNamesForDropdown.Count
                    ))
                    // Dropdown selection made, parse the selected step index
                {
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
                        {
                            currentStep.ConditionalGroups = currentSteps[selectedStepIndex].ConditionalGroups;
                        }
                    }
                }

                #endregion

                #region Add Conditional Group

                SetButtonColor(
                    greenButtonStyle.NormalColor,
                    greenButtonStyle.HoveredColor,
                    greenButtonStyle.ActiveColor
                );

                if (ImGui.Button($"Add Conditional Group##{stepIndex}"))
                {
                    currentStep.ConditionalGroups.Add(new ConditionalGroup());
                }

                PopStyleColors(3);

                #endregion

                #region Render Conditional Groups

                for (var groupIndex = 0; groupIndex < currentStep.ConditionalGroups.Count; groupIndex++)
                {
                    ImGui.BeginChild(
                        $"##conditionalGroup_{stepIndex}_{groupIndex}",
                        Vector2.Zero,
                        ImGuiChildFlags.Border | ImGuiChildFlags.AutoResizeY
                    );

                    #region Conditional Group Header

                    #region Group Delete

                    SetButtonColor(redButtonStyle.NormalColor, redButtonStyle.HoveredColor, redButtonStyle.ActiveColor);

                    if (ImGui.Button($"Remove Group##{stepIndex}_{groupIndex}"))
                    {
                        currentStep.ConditionalGroups.RemoveAt(groupIndex);
                        groupIndex--;
                        PopStyleColors(3);
                        continue;
                    }

                    PopStyleColors(3);

                    #endregion

                    #region Group Type

                    ImGui.SameLine();
                    // Check Timing Combo Box
                    var groupTypeIndex = (int)currentStep.ConditionalGroups[groupIndex].GroupType;
                    ImGui.SetNextItemWidth(60);

                    if (ImGui.Combo(
                            $" Group Type##{stepIndex}_{groupIndex}",
                            ref groupTypeIndex,
                            Enum.GetNames(typeof(ConditionGroup)),
                            GetEnumLength<ConditionGroup>()
                        ))
                    {
                        currentStep.ConditionalGroups[groupIndex].GroupType = (ConditionGroup)groupTypeIndex;
                    }

                    #endregion

                    #region Add Conditional Check

                    SetButtonColor(
                        greenButtonStyle.NormalColor,
                        greenButtonStyle.HoveredColor,
                        greenButtonStyle.ActiveColor
                    );

                    // Manage Conditional Checks
                    if (ImGui.Button($"Add Conditional Check##{stepIndex}_{groupIndex}"))
                    {
                        currentStep.ConditionalGroups[groupIndex].Conditionals.Add(new ConditionalKeys());
                    }

                    PopStyleColors(3);

                    #endregion

                    #region Conditional Group modification

                    if (currentStep.ConditionalGroups[groupIndex].GroupType != ConditionGroup.NOT)
                    {
                        ImGui.SameLine();

                        var conditionalChecksTrue
                            = currentStep.ConditionalGroups[groupIndex].ConditionalsToBePassForSuccess;

                        if (currentStep.ConditionalGroups[groupIndex].GroupType != ConditionGroup.NOT)
                        {
                            if (ImGui.InputInt(
                                    $"Req Checks to Pass##conditionalChecksTrue{stepIndex}_{groupIndex}",
                                    ref conditionalChecksTrue
                                ))
                            {
                                // Clamp the value between 1 and the number of conditional checks
                                conditionalChecksTrue = Math.Max(
                                    1,
                                    Math.Min(conditionalChecksTrue, currentStep.ConditionalGroups.Count)
                                );

                                currentStep.ConditionalGroups[groupIndex].ConditionalsToBePassForSuccess
                                    = conditionalChecksTrue;
                            }
                        }
                    }

                    #endregion

                    #endregion

                    #region Conditional Group modification

                    ImGui.Indent();
                    var checksToRemove = new List<int>(); // Track checks to remove

                    for (var conditionalIndex = 0;
                         conditionalIndex < currentStep.ConditionalGroups[groupIndex].Conditionals.Count;
                         conditionalIndex++)
                    {
                        SetButtonColor(
                            redButtonStyle.NormalColor,
                            redButtonStyle.HoveredColor,
                            redButtonStyle.ActiveColor
                        );

                        if (ImGui.Button($"Remove##{stepIndex}_{groupIndex}_{conditionalIndex}"))
                        {
                            checksToRemove.Add(conditionalIndex); // Mark this check for removal
                            PopStyleColors(3);
                            continue; // Skip the rest of the loop to avoid accessing a removed item
                        }

                        PopStyleColors(3);
                        ImGui.SameLine();
                        var checkKey = currentStep.ConditionalGroups[groupIndex].Conditionals[conditionalIndex].Name;
                        var availableWidth = ImGui.GetContentRegionAvail().X * 0.75f;
                        ImGui.SetNextItemWidth(availableWidth);

                        if (ImGui.InputTextWithHint(
                                $"##{stepIndex}_{groupIndex}_{conditionalIndex}",
                                "Name of condition...",
                                ref checkKey,
                                1000
                            ))
                        {
                            currentStep.ConditionalGroups[groupIndex].Conditionals[conditionalIndex].Name = checkKey;
                        }
                        // Update the check key

                        ImGui.SameLine();
                        const bool showPopup = true;

                        // Initialize both tempCondValue and condEditValue when opening the popup

                        if (ImGui.Button($"Edit##{stepIndex}_{groupIndex}_{conditionalIndex}"))
                        {
                            condEditValue = currentStep.ConditionalGroups[groupIndex].Conditionals[conditionalIndex]
                                                       .Value;

                            tempCondValue = condEditValue;

                            ImGui.OpenPopup(
                                FilterEditPopup + $"##conditionalEditPopup{stepIndex}_{groupIndex}_{conditionalIndex}"
                            );
                        }

                        ConditionValueEditPopup(showPopup, stepIndex, groupIndex, conditionalIndex, currentStep);
                    }

                    ImGui.Unindent();

                    foreach (var index in checksToRemove.OrderByDescending(j => j))
                    {
                        if (currentStep.ConditionalGroups[groupIndex].Conditionals.Count >=
                            currentStep.ConditionalGroups[groupIndex].ConditionalsToBePassForSuccess &&
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

                #endregion
            }

            #endregion

            #region Insert Step Above

            SetButtonColor(greenButtonStyle.NormalColor, greenButtonStyle.HoveredColor, greenButtonStyle.ActiveColor);

            if (ImGui.Button($"[^] Insert Step Above##{stepIndex}"))
            {
                // Manually initialize the ConditionalGroups
                currentSteps.Insert(stepIndex, new CraftingStepInput());
                PopStyleColors(3); // Always pop style color to avoid styling issues
                continue;
            }

            PopStyleColors(3); // Always pop style color to avoid styling issues

            #endregion

            #region Remove Current Step

            ImGui.SameLine();
            SetButtonColor(redButtonStyle.NormalColor, redButtonStyle.HoveredColor, redButtonStyle.ActiveColor);

            if (ImGui.Button($"[-] Remove This Step##{stepIndex}"))
            {
                currentSteps.RemoveAt(stepIndex);
                stepIndex--;
                PopStyleColors(3); // Always pop style color to avoid styling issues
                continue;
            }

            PopStyleColors(3); // Always pop style color to avoid styling issues

            #endregion

            #region Insert Step Below

            if (stepIndex < currentSteps.Count - 1)
            {
                ImGui.SameLine();

                SetButtonColor(
                    greenButtonStyle.NormalColor,
                    greenButtonStyle.HoveredColor,
                    greenButtonStyle.ActiveColor
                );

                if (ImGui.Button($"[v] Insert Step Below##{stepIndex}"))
                {
                    // Manually initialize the ConditionalGroups
                    currentSteps.Insert(stepIndex + 1, new CraftingStepInput());
                    PopStyleColors(3); // Always pop style color to avoid styling issues
                    continue;
                }

                PopStyleColors(3); // Always pop style color to avoid styling issues
            }

            #endregion

            ImGui.Unindent();

            #endregion Step Settings

            ImGui.EndChild();
        }

        Main.Settings.SelectedCraftingStepInputs = currentSteps;

        #region Add New Step

        SetButtonColor(greenButtonStyle.NormalColor, greenButtonStyle.HoveredColor, greenButtonStyle.ActiveColor);

        if (ImGui.Button("[=] Add New Step"))
        {
            Main.Settings.SelectedCraftingStepInputs.Add(new CraftingStepInput());
        }

        PopStyleColors(3); // Always pop style color to avoid styling issues

        #endregion

        Main.Settings.CraftingSequenceLastSaved = _fileSaveName;
        Main.Settings.CraftingSequenceLastSelected = _selectedFileName;
    }

    private static void SetButtonColor(Color button, Color hovered, Color active)
    {
        if (!Main.Settings.MenuStyling)
        {
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Button, button.ToImguiVec4());
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hovered.ToImguiVec4());
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, active.ToImguiVec4());
    }

    private static void PopStyleColors(int count)
    {
        if (Main.Settings.MenuStyling)
        {
            ImGui.PopStyleColor(count);
        }
    }

    private static void ConditionValueEditPopup(bool showPopup, int stepIndex, int groupIndex, int conditionalIndex,
        CraftingStepInput stepInput)
    {
        if (!ImGui.BeginPopupModal(
                FilterEditPopup + $"##conditionalEditPopup{stepIndex}_{groupIndex}_{conditionalIndex}",
                ref showPopup,
                ImGuiWindowFlags.AlwaysAutoResize
            ))
        {
            return;
        }

        ImGui.InputTextMultiline(
            $"##text{stepIndex}_{groupIndex}_{conditionalIndex}",
            ref tempCondValue,
            15000,
            new Vector2(800, 600),
            ImGuiInputTextFlags.AllowTabInput
        );

        if (ImGui.Button("Save"))
        {
            stepInput.ConditionalGroups[groupIndex].Conditionals[conditionalIndex].Value = tempCondValue;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();

        if (ImGui.Button("Revert"))
        {
            tempCondValue = condEditValue;
        }

        ImGui.SameLine();

        if (ImGui.Button("Close"))
        {
            ImGui.CloseCurrentPopup();
        }

        // Aggregate all conditionals from all steps and groups, keeping track of the step index
        var allConditionalsWithStepInfo = Main.Settings.SelectedCraftingStepInputs.SelectMany(
            (step, stepIndex) => step.ConditionalGroups.SelectMany(
                group => group.Conditionals,
                (group, conditional) => new
                {
                    stepIndex,
                    conditional
                }
            )
        ).ToList();

        // Generate display names for each conditional with step information
        var conditionalNames = allConditionalsWithStepInfo.Select(
            (c, index) => $"Step {c.stepIndex + 1}: " + (string.IsNullOrEmpty(c.conditional.Name)
                ? $"Unnamed Conditional {index + 1}" : c.conditional.Name)
        ).ToArray();

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

        ImGui.EndPopup();
    }

    private static void DrawConfirmAndClear()
    {
        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.ButtonActive)); // Set the header color

        if (!ImGui.CollapsingHeader(
                $"Confirm / Clear Steps##{Main.Name}Confirm / Clear Steps",
                ImGuiTreeNodeFlags.DefaultOpen
            ))
        {
            return;
        }

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
                            Logging.Logging.Add(
                                $"CraftingSequenceMenu: Failed to load filter from  for string: {checkKey.Name}",
                                LogMessageType.Error
                            );

                            return; // No point going on from here.
                        }

                        //newGroup.ConditionalChecks.Add(() => FilterHandler.IsMatchingCondition(filter));
                        newGroup.ConditionalChecks.Add(
                            async token =>
                            {
                                var resultTuple = await FilterHandler.AsyncIsMatchingCondition(
                                    filter,
                                    SpecialSlot.CurrencyTab,
                                    token
                                );

                                return resultTuple; // Assuming Item2 is the boolean result of the condition
                            }
                        );
                    }

                    newStep.ConditionalCheckGroups.Add(newGroup);
                }

                Main.SelectedCraftingSteps.Add(newStep);
            }

            Logging.Logging.Add(
                $"CraftingSequenceMenu: Steps added {Main.SelectedCraftingSteps.Count}",
                LogMessageType.Info
            );
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
                Main.Settings.SelectedCraftingStepInputs.Clear();
                Main.SelectedCraftingSteps.Clear();
            }
        }

        ImGui.Separator();
        ImGui.Unindent();
    }

    private static void DrawInstructions()
    {
        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.ButtonActive)); // Set the header color

        if (!ImGui.CollapsingHeader($"Selected Step Instructions##{Main.Name}Instructions"))
        {
            return;
        }

        ImGui.Indent();
        // Start of indented Section
        var steps = Main.Settings.SelectedCraftingStepInputs.ToList();

        for (var index = 0; index < steps.Count; index++)
        {
            var currentStep = Main.Settings.SelectedCraftingStepInputs[index];

            var childWindowTitle = currentStep.CheckType == ConditionalCheckType.ConditionalCheckOnly ? "Check the item"
                : $"Use '{steps[index].CurrencyItem}'";

            CreateStepChildWindow(index, childWindowTitle);

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

        void CreateStepChildWindow(int index, string title)
        {
            ImGui.BeginChild(
                $"[{index + 1}] {title}##stepinstruction{index}",
                Vector2.Zero,
                ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.Border
            );

            ImGui.Text($"[{index + 1}] {title}");
            ImGui.Indent();

            // Add "Check the item" text if the title starts with "Use"
            if (title.StartsWith("Use"))
            {
                ImGui.Text("Check the item");
            }
        }

        // Function to handle the display for steps with automatic success
        void HandleAutomaticSuccess(CraftingStepInput currentStep)
        {
            if (currentStep.SuccessAction == SuccessAction.GoToStep)
            {
                ImGui.Text($"Then, go to step {currentStep.SuccessActionStepIndex + 1}");
            }
        }

        // Function to handle the display for steps requiring manual success evaluation
        void HandleManualSuccess(CraftingStepInput currentStep)
        {
            foreach (var conditionalGroup in currentStep.ConditionalGroups.OrderBy(group => group.GroupType))
            {
                // Use a different symbol or format for the header line
                ImGui.Text(
                    $"{conditionalGroup.GroupType} {conditionalGroup.ConditionalsToBePassForSuccess} or more of the following conditions pass:"
                );

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
        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.ButtonActive)); // Set the header color

        if (!ImGui.CollapsingHeader($"Load / Save##{Main.Name}Load / Save", ImGuiTreeNodeFlags.DefaultOpen))
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
            var directoryToOpen = Directory.Exists(configDir);

            if (!directoryToOpen)
                // Log error when the config directory doesn't exist
            {
                Logging.Logging.Add("Unable to open config directory because it does not exist.", LogMessageType.Error);
            }

            if (configDir != null)
            {
                Process.Start("explorer.exe", configDir);
                // Log info when opening the config directory
                Logging.Logging.Add("Opened config directory in explorer.", LogMessageType.Info);
            }
        }

        if (ShowButtonPopup(OverwritePopup, ["Are you sure?", "STOP"], out var saveSelectedIndex))
        {
            if (saveSelectedIndex == 0)
            {
                SaveFile(Main.Settings.SelectedCraftingStepInputs, $"{_fileSaveName}.json");

                // Log success when file is saved after overwrite confirmation
                Logging.Logging.Add(
                    $"File {_fileSaveName}.json saved successfully after overwrite confirmation.",
                    LogMessageType.Info
                );
            }
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
}