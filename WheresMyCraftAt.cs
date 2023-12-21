using ExileCore;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using WheresMyCraftAt.CraftingSequence;
using WheresMyCraftAt.Extensions;
using WheresMyCraftAt.Handlers;
using static WheresMyCraftAt.CraftingSequence.CraftingSequenceBase;
using Vector2N = System.Numerics.Vector2;

namespace WheresMyCraftAt
{
    public partial class WheresMyCraftAt : BaseSettingsPlugin<WheresMyCraftAtSettings>
    {
        public readonly Dictionary<LogMessageType, Color> _logMessageColors = new()
        {
            { LogMessageType.Info, Color.White },
            { LogMessageType.Warning, Color.Yellow },
            { LogMessageType.Error, Color.Red },
            { LogMessageType.Success, Color.Green },
            { LogMessageType.Cancelled, Color.Orange },
            { LogMessageType.Special, Color.Gray }
        };

        public readonly Dictionary<SpecialSlot, Vector2N> specialSlotDimensionMap = new()
        {
            { SpecialSlot.CurrencyTab, new Vector2N(126f, 252f) },
            { SpecialSlot.EssenceTab, new Vector2N(127.2f, 254.4f) }
        };

        private CancellationTokenSource _operationCts;
        private SyncTask<bool> _currentOperation;
        public Vector2 ClickWindowOffset;
        public int ServerLatency;
        public List<CraftingStep> SelectedCraftingSteps = [];

        public WheresMyCraftAt()
        {
            Name = "Wheres My Craft At";
            _operationCts = new CancellationTokenSource();
        }

        public override bool Initialise()
        {
            RegisterHotkey(Settings.TestButton1);

            CraftingSequenceExecutor.Initialize(this);
            ElementHandler.Initialize(this);
            ExecuteHandler.Initialize(this);
            GameHandler.Initialize(this);
            InventoryHandler.Initialize(this);
            ItemExtensions.Initialize(this);
            ItemHandler.Initialize(this);
            KeyHandler.Initialize(this);
            MouseHandler.Initialize(this);
            StashHandler.Initialize(this);

            return true;
        }

        private static void RegisterHotkey(HotkeyNode hotkey)
        {
            Input.RegisterKey(hotkey);
            hotkey.OnValueChanged += () => Input.RegisterKey(hotkey);
        }

        public override Job Tick()
        {
            ClickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            ServerLatency = GameController.IngameState.ServerData.Latency;

            if (!GameHandler.IsInGameCondition())
            {
                Stop();
                return null;
            }

            if (Settings.TestButton1.PressedOnce())
            {
                if (_currentOperation is not null)
                {
                    // Imediate cancelation called, release all buttons.
                    Stop();
                }
                else
                {
                    DebugPrint($"{Name}: Attempting to Start New Operation.", LogMessageType.Info);
                    ResetCancellationTokenSource();
                    _currentOperation = AsyncStart(_operationCts.Token);
                }
            }

            if (_currentOperation is not null)
                TaskUtils.RunOrRestart(ref _currentOperation, () => null);

            return null;
        }

        private void Stop()
        {
            if (_currentOperation is not null)
            {
                _currentOperation = null;

                var keysToRelease = new List<Keys>
                {
                    Keys.LControlKey,
                    Keys.ShiftKey,
                    Keys.LButton,
                    Keys.RButton
                };

                foreach (var key in keysToRelease)
                    if (Input.GetKeyState(key))
                        Input.KeyUp(key);

                if (ItemHandler.IsItemRightClickedCondition())
                    Input.KeyPressRelease(Keys.Escape);

                DebugPrint($"{Name}: Stop() has been ran.", LogMessageType.Warning);
            }
        }

        private void ResetCancellationTokenSource()
        {
            if (_operationCts != null)
            {
                if (!_operationCts.IsCancellationRequested)
                {
                    _operationCts.Cancel();
                }
                _operationCts.Dispose();
            }
            _operationCts = new CancellationTokenSource();
        }

        private async SyncTask<bool> AsyncStart(CancellationToken token)
        {
            if (!GameHandler.IsInGameCondition())
            {
                DebugPrint($"{Name}: Not in game, operation will be terminated.", LogMessageType.Error);
                return false;
            }

            try
            {
                bool isInvOpen = await InventoryHandler.AsyncWaitForInventoryOpen(token);
                bool isStashOpen = await StashHandler.AsyncWaitForStashOpen(token);

                if (!isStashOpen || !isInvOpen)
                    return false;

                var giveItems = new CraftingSequenceExecutor(SelectedCraftingSteps);

                if (!await giveItems.Execute(CancellationToken.None))
                    return false;

                DebugPrint($"{Name}: AsyncTestButton1Main() Completed.", LogMessageType.Success);
            }
            catch (OperationCanceledException)
            {
                Stop();
                return false;
            }

            return true;
        }

        public void DebugPrint(string printString, LogMessageType messageType)
        {
            if (Settings.DebugPrint)
            {
                Color messageColor = _logMessageColors[messageType];
                DebugWindow.LogMsg(printString, Settings.DebugPrintLingerTime, messageColor);
            }
        }

        #region Draw Settings

        public override void DrawSettings()
        {
            base.DrawSettings();

            if (ImGui.Button("Remove All"))
            {
                Settings.SelectedCraftingStepInputs.Clear();
            }

            var currentSteps = new List<CraftingStepInput>(Settings.SelectedCraftingStepInputs);

            for (int i = 0; i < currentSteps.Count; i++)
            {
                var stepInput = currentSteps[i];
                ImGui.Separator();

                // Use a colored, collapsible header for each step
                ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.ButtonActive)); // Set the header color
                if (ImGui.CollapsingHeader($"Step {i}##header{i}", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    #region Step Settings

                    float availableWidth = ImGui.GetContentRegionAvail().X * 0.65f;
                    ImGui.Indent();
                    ImGui.PopStyleColor(); // Always pop style color to avoid styling issues

                    float dropdownWidth = availableWidth * 0.6f;
                    float inputWidth = availableWidth * 0.4f;

                    // Currency Item Input
                    string currencyItem = stepInput.CurrencyItem;
                    if (ImGui.InputText($"Currency Item##{i}", ref currencyItem, 100))
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
                    if (ImGui.Combo($"Check Timing##{i}", ref checkTimingIndex, Enum.GetNames(typeof(ConditionalCheckTiming)), GetEnumLength<ConditionalCheckTiming>()))
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

                        // Success Action Combo Box
                        int conditionalCheckIndex = (int)stepInput.ItemRarityWanted;
                        if (ImGui.Combo($"Conditional Rarity Check (Temp)##{i}", ref conditionalCheckIndex, Enum.GetNames(typeof(ItemRarity)), GetEnumLength<ItemRarity>()))
                        {
                            stepInput.ItemRarityWanted = (ItemRarity)conditionalCheckIndex;
                        }
                    }

                    #endregion Step Settings

                    ImGui.Separator();

                    if (ImGui.Button($"Insert Step##{i}"))
                    {
                        currentSteps.Insert(i + 1, new CraftingStepInput());
                        i++; // Skip the newly added step in this iteration
                        continue; // Skip the rest of the loop for this iteration
                    }

                    ImGui.SameLine();
                    if (ImGui.Button($"Remove THIS Step##{i}"))
                    {
                        currentSteps.RemoveAt(i);
                        i--; // Adjust index to account for the removed item
                        continue; // Skip the rest of the loop for this iteration
                    }

                    ImGui.Separator();
                    ImGui.Unindent();
                }
                else
                {
                    ImGui.PopStyleColor();
                }
            }

            // Reflect the changes back to Settings.SelectedCraftingStepInputs
            Settings.SelectedCraftingStepInputs = currentSteps;

            if (ImGui.Button("Add New Step"))
            {
                Settings.SelectedCraftingStepInputs.Add(new CraftingSequenceBase.CraftingStepInput());
            }

            if (ImGui.Button("Apply Steps"))
            {
                SelectedCraftingSteps.Clear();
                foreach (var input in Settings.SelectedCraftingStepInputs)
                {
                    CraftingSequenceBase.CraftingStep newStep = new CraftingSequenceBase.CraftingStep
                    {
                        Method = async (token) => await ItemHandler.AsyncTryApplyOrbToSlot(SpecialSlot.CurrencyTab, input.CurrencyItem, token),
                        ConditionalCheck = () => ItemHandler.IsItemRarityFromSpecialSlotCondition(SpecialSlot.CurrencyTab, input.ItemRarityWanted),
                        AutomaticSuccess = input.AutomaticSuccess,
                        SuccessAction = input.SuccessAction,
                        SuccessActionStepIndex = input.SuccessActionStepIndex,
                        FailureAction = input.FailureAction,
                        FailureActionStepIndex = input.FailureActionStepIndex
                    };
                    SelectedCraftingSteps.Add(newStep);
                }
            }
        }

        private int GetEnumLength<T>()
        {
            return Enum.GetNames(typeof(T)).Length;
        }

        #endregion Draw Settings
    }
}