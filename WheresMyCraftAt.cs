using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vector2N = System.Numerics.Vector2;

namespace WheresMyCraftAt
{
    public partial class WheresMyCraftAt : BaseSettingsPlugin<WheresMyCraftAtSettings>
    {
        private readonly Dictionary<LogMessageType, Color> _logMessageColors = new()
        {
            { LogMessageType.Info, Color.White },
            { LogMessageType.Warning, Color.Yellow },
            { LogMessageType.Error, Color.Red },
            { LogMessageType.Success, Color.Green },
            { LogMessageType.Cancelled, Color.Orange },
            { LogMessageType.Timeout, Color.Gray }
        };

        private CancellationTokenSource _operationCts;
        private Task<bool> _currentOperationTask;
        private Vector2 _clickWindowOffset;
        private int _serverLatency;

        public WheresMyCraftAt()
        {
            Name = "Wheres My Craft At";
            _operationCts = new CancellationTokenSource();
            _currentOperationTask = Task.FromResult(false);
        }

        public override bool Initialise()
        {
            RegisterHotkey(Settings.TestButton1);

            return true;
        }

        private static void RegisterHotkey(HotkeyNode hotkey)
        {
            Input.RegisterKey(hotkey);
            hotkey.OnValueChanged += () => { Input.RegisterKey(hotkey); };
        }

        public override void AreaChange(AreaInstance area)
        {
            CancelCurrentOperation();
        }

        public override void Render()
        {
            //Graphics.DrawText($"MouseState ({Input.GetKeyState(Keys.LButton)})", new Vector2N(900, 600));
            Graphics.DrawText($"{IsAnItemRightClickedOnCursor(GameController)}", new Vector2N(900, 600));
        }

        public override Job Tick()
        {
            _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            _serverLatency = GameController.IngameState.ServerData.Latency;

            if (Settings.TestButton1.PressedOnce())
            {
                if (_currentOperationTask.Status != TaskStatus.RanToCompletion)
                {
                    CancelCurrentOperation();
                }
                else
                {
                    StartNewOperation();
                }
            }

            return null;
        }

        private void CancelCurrentOperation()
        {
            DebugPrint($"{Name}: Attempting to Cancel Current Operation.", LogMessageType.Cancelled);
            ResetCancellationTokenSource();
            _currentOperationTask = Task.FromResult(false);
        }

        private void StartNewOperation()
        {
            DebugPrint($"{Name}: Attempting to Start New Operation.", LogMessageType.Info);
            ResetCancellationTokenSource();

            _currentOperationTask = Task.Run(async () => await AsyncTestButton1Main(_operationCts.Token), _operationCts.Token);
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

        private async Task<bool> AsyncTestButton1Main(CancellationToken cancellationToken)
        {
            var FunctionName = "AsyncTestButton1Main";
            DebugPrint($"{Name}: {FunctionName} called.", LogMessageType.Info);

            if (!IsInGame(GameController))
            {
                DebugPrint($"{Name}: Not in game, operation will be terminated.", LogMessageType.Error);
                return false;
            }

            try
            {
                //if (!await AsyncMoveMouse(new Vector2N(Settings.MouseMoveX, Settings.MouseMoveY), cancellationToken))
                //    return false;

                //if (!await AsyncMoveMouse(new Vector2N(0, 0), cancellationToken))
                //    return false;

                //if (!await AsyncMoveMouse(new Vector2N(Settings.MouseMoveX, Settings.MouseMoveY), cancellationToken))
                //    return false;

                //if (!await AsyncButtonPress(Keys.LButton, cancellationToken))
                //    return false;

                bool isStashOpen = await WaitForStashOpen(cancellationToken);
                bool isInvOpen = await WaitForInventoryOpen(cancellationToken);

                if (!isStashOpen || !isInvOpen)
                    return false;

                DebugPrint("Stash & Inventory Open", LogMessageType.Success);
            }
            catch (Exception ex)
            {
                DebugPrint($"{Name}: Exception in {FunctionName}: {ex.Message}", LogMessageType.Error);
                return false;
            }

            DebugPrint($"{Name}: {FunctionName}() Finished", LogMessageType.Success);
            return true;
        }

        private async Task<bool> AsyncWaitServerLatency(CancellationToken cancellationToken)
        {
            var FunctionName = "AsyncWaitServerLatency";
            DebugPrint($"{Name}: {FunctionName}({_serverLatency})", LogMessageType.Info);
            await Task.Delay(_serverLatency, cancellationToken);

            return true;
        }

        private async Task<bool> AsyncMoveMouse(Vector2N wantedPosition, CancellationToken cancellationToken)
        {
            var FunctionName = "AsyncMoveMouse";
            DebugPrint($"{Name}: {FunctionName}({wantedPosition})", LogMessageType.Info);

            cancellationToken.ThrowIfCancellationRequested();

            return await AsyncIsMouseInPlace(wantedPosition, cancellationToken);
        }

        private async Task<bool> AsyncIsMouseInPlace(Vector2N wantedPosition, CancellationToken cancellationToken)
        {
            var FunctionName = "AsyncIsMouseInPlace";
            using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ctsTimeout.CancelAfter(TimeSpan.FromSeconds(Settings.ActionTimeoutInSeconds));

            try
            {
                while (!ctsTimeout.Token.IsCancellationRequested)
                {
                    Input.SetCursorPos(GetRelativeWinPos(wantedPosition));

                    await AsyncWaitServerLatency(ctsTimeout.Token);

                    var currentMousePosition = new Vector2N(GameController.IngameState.MousePosX, GameController.IngameState.MousePosY);
                    if (currentMousePosition == wantedPosition)
                    {
                        DebugPrint($"{Name}: {FunctionName}({wantedPosition}) = True", LogMessageType.Success);
                        return true;
                    }
                }

                return HandleTimeoutOrCancellation(new Vector2N(Settings.MouseMoveX, Settings.MouseMoveY), FunctionName, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return HandleTimeoutOrCancellation(new Vector2N(Settings.MouseMoveX, Settings.MouseMoveY), FunctionName, cancellationToken);
            }
        }

        private async Task<bool> AsyncButtonPress(Keys button, CancellationToken cancellationToken)
        {
            var FunctionName = "AsyncButtonPress";
            DebugPrint($"{Name}: {FunctionName}({button})", LogMessageType.Info);

            cancellationToken.ThrowIfCancellationRequested();

            bool isDown = await AsyncIsButtonDown(button, cancellationToken);
            bool isUp = await AsyncIsButtonUp(button, cancellationToken);

            return isDown && isUp;
        }

        private async Task<bool> AsyncSetButtonDown(Keys button, CancellationToken cancellationToken)
        {
            var FunctionName = "AsyncSetButtonDown";
            DebugPrint($"{Name}: {FunctionName}({button})", LogMessageType.Info);

            cancellationToken.ThrowIfCancellationRequested();

            return await AsyncIsButtonDown(button, cancellationToken);
        }

        private async Task<bool> AsyncIsButtonDown(Keys button, CancellationToken cancellationToken)
        {
            var FunctionName = "AsyncIsButtonDown";
            using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ctsTimeout.CancelAfter(TimeSpan.FromSeconds(Settings.ActionTimeoutInSeconds));

            try
            {
                while (!ctsTimeout.Token.IsCancellationRequested)
                {
                    if (button == Keys.LButton)
                        Input.LeftDown();
                    else if (button == Keys.RButton)
                        Input.RightDown();
                    else
                        Input.KeyDown(button);

                    await AsyncWaitServerLatency(ctsTimeout.Token);

                    var isButtonDown = Input.GetKeyState(button);
                    if (isButtonDown)
                    {
                        DebugPrint($"{Name}: {FunctionName}({button}) = True", LogMessageType.Success);
                        return true;
                    }
                }

                return HandleTimeoutOrCancellation(button, FunctionName, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return HandleTimeoutOrCancellation(button, FunctionName, cancellationToken);
            }
        }

        private async Task<bool> AsyncIsButtonUp(Keys button, CancellationToken cancellationToken)
        {
            var FunctionName = "AsyncIsButtonUp";
            using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ctsTimeout.CancelAfter(TimeSpan.FromSeconds(Settings.ActionTimeoutInSeconds));

            try
            {
                while (!ctsTimeout.Token.IsCancellationRequested)
                {
                    if (button == Keys.LButton)
                        Input.LeftUp();
                    else if (button == Keys.RButton)
                        Input.RightUp();
                    else
                        Input.KeyUp(button);

                    await AsyncWaitServerLatency(ctsTimeout.Token);

                    var isButtonDown = Input.GetKeyState(button);
                    if (!isButtonDown)
                    {
                        DebugPrint($"{Name}: {FunctionName}({button}) = True", LogMessageType.Success);
                        return true;
                    }
                }

                return HandleTimeoutOrCancellation(button, FunctionName, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return HandleTimeoutOrCancellation(button, FunctionName, cancellationToken);
            }
        }

        private bool HandleTimeoutOrCancellation<T>(T identifier, string actionName, CancellationToken cancellationToken)
        {
            string message = cancellationToken.IsCancellationRequested
                ? $"{Name}: {actionName}({identifier}) = False (Cancelled)"
                : $"{Name}: {actionName}({identifier}) = False (Timeout)";

            LogMessageType messageType = cancellationToken.IsCancellationRequested
                ? LogMessageType.Cancelled
                : LogMessageType.Timeout;

            DebugPrint(message, messageType);
            return false;
        }

        private bool HandleTimeoutOrCancellation(string actionName, CancellationToken cancellationToken)
        {
            string message = cancellationToken.IsCancellationRequested
                ? $"{Name}: {actionName} = False (Cancelled)"
                : $"{Name}: {actionName} = False (Timeout)";

            LogMessageType messageType = cancellationToken.IsCancellationRequested
                ? LogMessageType.Cancelled
                : LogMessageType.Timeout;

            DebugPrint(message, messageType);
            return false;
        }

        public Vector2N GetRelativeWinPos(Vector2N wantedPosition)
        {
            return new Vector2N(
                wantedPosition.X + _clickWindowOffset.X,
                wantedPosition.Y + _clickWindowOffset.Y
            );
        }

        private async Task<bool> WaitForStashOpen(CancellationToken cancellationToken, int timeout = 10)
        {
            var FunctionName = "WaitForStashOpen";
            using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ctsTimeout.CancelAfter(TimeSpan.FromSeconds(timeout));

            try
            {
                while (!ctsTimeout.Token.IsCancellationRequested)
                {
                    await AsyncWaitServerLatency(ctsTimeout.Token);

                    var panel = IsStashPanelOpen(GameController);
                    if (panel)
                    {
                        DebugPrint($"{Name}: {FunctionName}({panel}) = True", LogMessageType.Success);
                        return true;
                    }
                }

                return HandleTimeoutOrCancellation(FunctionName, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return HandleTimeoutOrCancellation(FunctionName, cancellationToken);
            }
        }

        private async Task<bool> WaitForInventoryOpen(CancellationToken cancellationToken, int timeout = 10)
        {
            var FunctionName = "WaitForInventoryOpen";
            using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ctsTimeout.CancelAfter(TimeSpan.FromSeconds(timeout));

            try
            {
                while (!ctsTimeout.Token.IsCancellationRequested)
                {
                    await AsyncWaitServerLatency(ctsTimeout.Token);

                    var panel = IsInGame(GameController);
                    if (panel)
                    {
                        DebugPrint($"{Name}: {FunctionName}({panel}) = True", LogMessageType.Success);
                        return true;
                    }
                }

                return HandleTimeoutOrCancellation(FunctionName, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return HandleTimeoutOrCancellation(FunctionName, cancellationToken);
            }
        }

        private static bool IsAnItemRightClickedOnCursor(GameController gameController)
        {
            return gameController?.Game?.IngameState?.IngameUi?.Cursor?.GetChildAtIndex(0)?.IsVisible ?? false;
        }

        private static bool IsStashPanelOpen(GameController gameController)
        {
            return gameController?.Game?.IngameState?.IngameUi?.StashElement?.IsVisible ?? false;
        }

        private static bool IsInventoryPanelOpen(GameController gameController)
        {
            return gameController?.Game?.IngameState?.IngameUi?.InventoryPanel?.IsVisible ?? false;
        }

        private static bool IsAnItemPickedUp(GameController gameController)
        {
            return gameController?.Game?.IngameState?.ServerData?.PlayerInventories[(int)InventorySlotE.Cursor1]?.Inventory?.ItemCount > 0;
        }

        private static Entity GetPickedUpItem(GameController gameController, InventorySlotE invSlot)
        {
            if (IsAnItemPickedUp(gameController))
                return GetItemsFromAnInventory(gameController, invSlot).FirstOrDefault();

            return null;
        }

        private static IList<Entity> GetItemsFromAnInventory(GameController gameController, InventorySlotE invSlot)
        {
            return gameController?.Game?.IngameState?.ServerData?.PlayerInventories[(int)invSlot]?.Inventory?.Items;
        }

        private static bool IsInGame(GameController gameController)
        {
            return gameController?.Game?.IngameState?.InGame ?? false;
        }

        public void DebugPrint(string printString, LogMessageType messageType)
        {
            if (Settings.DebugPrint)
            {
                Color messageColor = _logMessageColors[messageType];
                DebugWindow.LogMsg(printString, Settings.DebugPrintLingerTime, messageColor);
            }
        }
    }
}