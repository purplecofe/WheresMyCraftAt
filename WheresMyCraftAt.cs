using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
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
        private SyncTask<bool> _currentOperation;
        private Vector2 _clickWindowOffset;
        private int _serverLatency;

        public WheresMyCraftAt()
        {
            Name = "Wheres My Craft At";
            _operationCts = new CancellationTokenSource();
        }

        public override bool Initialise()
        {
            RegisterHotkey(Settings.TestButton1);

            return true;
        }

        private static void RegisterHotkey(HotkeyNode hotkey)
        {
            Input.RegisterKey(hotkey);
            hotkey.OnValueChanged += () => Input.RegisterKey(hotkey);
        }

        public override void Render()
        {
        }

        public override Job Tick()
        {
            _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            _serverLatency = GameController.IngameState.ServerData.Latency;

            if (!IsInGameCondition(GameController))
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
                    _currentOperation = AsyncTestButton1Main(_operationCts.Token);
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

                if (IsItemRightClickedCondition(GameController))
                    Input.KeyPressRelease(Keys.Escape);
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

        private async SyncTask<bool> AsyncTestButton1Main(CancellationToken token)
        {
            if (!IsInGameCondition(GameController))
            {
                DebugPrint($"{Name}: Not in game, operation will be terminated.", LogMessageType.Error);
                return false;
            }

            try
            {
                bool isInvOpen = await AsyncWaitForInventoryOpen(token);
                bool isStashOpen = await AsyncWaitForStashOpen(token);

                if (!isStashOpen || !isInvOpen)
                    return false;

                if (!await AsyncMoveMouse(new Vector2N(Settings.MouseMoveX, Settings.MouseMoveY), token))
                    return false;

                if (!await AsyncButtonPress(Keys.RButton, token))
                    return false;

                if (!await AsyncWaitForRightClickedItemOnCursor(token))
                    return false;

                if (!await AsyncButtonPress(Keys.RButton, token))
                    return false;

                if (!await AsyncWaitForRightClickedItemOffCursor(token))
                {
                    await AsyncButtonPress(Keys.Escape, token);
                    await AsyncWaitServerLatency(token);
                    return false;
                }

                DebugPrint($"{Name}: AsyncTestButton1Main() Completed.", LogMessageType.Success);
            }
            catch (OperationCanceledException)
            {
                Stop();
                return false;
            }

            return true;
        }

        private async SyncTask<bool> AsyncWaitServerLatency(CancellationToken token)
        {
            await Task.Delay(_serverLatency, token);

            return true;
        }

        private async SyncTask<bool> AsyncMoveMouse(Vector2N position, CancellationToken token) => await AsyncIsMouseInPlace(position, token);

        private async SyncTask<bool> AsyncIsMouseInPlace(Vector2N position, CancellationToken token)
        {
            return await ExecuteWithCancellationHandling(
                action: () => SetCursorPositionAction(position),
                condition: () => IsMouseInPositionCondition(position),
                timeoutS: Settings.ActionTimeoutInSeconds,
                token: token);
        }

        private async SyncTask<bool> AsyncButtonPress(Keys button, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            bool isDown = await AsyncIsButtonDown(button, token);
            bool isUp = await AsyncIsButtonUp(button, token);

            return isDown && isUp;
        }

        private async SyncTask<bool> AsyncSetButtonDown(Keys button, CancellationToken token) =>
            await AsyncIsButtonDown(button, token);

        private void PerformButtonAction(Keys button, bool press)
        {
            if (press)
            {
                if (button == Keys.LButton) Input.LeftDown();
                else if (button == Keys.RButton) Input.RightDown();
                else Input.KeyDown(button);
            }
            else
            {
                if (button == Keys.LButton) Input.LeftUp();
                else if (button == Keys.RButton) Input.RightUp();
                else Input.KeyUp(button);
            }
        }

        private async SyncTask<bool> AsyncIsButtonUp(Keys button, CancellationToken token)
        {
            return await ExecuteWithCancellationHandling(
                action: () => PerformButtonAction(button, false),
                condition: () => !Input.GetKeyState(button),
                timeoutS: Settings.ActionTimeoutInSeconds,
                token: token);
        }

        private async SyncTask<bool> AsyncIsButtonDown(Keys button, CancellationToken token)
        {
            return await ExecuteWithCancellationHandling(
                action: () => PerformButtonAction(button, true),
                condition: () => Input.GetKeyState(button),
                timeoutS: Settings.ActionTimeoutInSeconds,
                token: token);
        }

        public Vector2N GetRelativeWinPos(Vector2N position)
        {
            return new Vector2N(
                position.X + _clickWindowOffset.X,
                position.Y + _clickWindowOffset.Y
            );
        }

        private async SyncTask<bool> AsyncWaitForStashOpen(CancellationToken token, int timeout = 2)
        {
            return await ExecuteWithCancellationHandling(
                condition: () => IsStashPanelOpenCondition(GameController),
                timeoutS: timeout,
                token: token
                );
        }

        private async SyncTask<bool> AsyncWaitForInventoryOpen(CancellationToken token, int timeout = 2)
        {
            return await ExecuteWithCancellationHandling(
                condition: () => IsInventoryPanelOpenCondition(GameController),
                timeoutS: timeout,
                token: token
                );
        }

        private async SyncTask<bool> AsyncWaitForItemOnCursor(CancellationToken token, int timeout = 2)
        {
            return await ExecuteWithCancellationHandling(
                condition: () => IsAnItemPickedUpCondition(GameController),
                timeoutS: timeout,
                token: token
                );
        }

        private async SyncTask<bool> AsyncWaitForItemOffCursor(CancellationToken token, int timeout = 2)
        {
            return await ExecuteWithCancellationHandling(
                condition: () => !IsAnItemPickedUpCondition(GameController),
                timeoutS: timeout,
                token: token
                );
        }

        private async SyncTask<bool> AsyncWaitForRightClickedItemOnCursor(CancellationToken token, int timeout = 2)
        {
            return await ExecuteWithCancellationHandling(
                condition: () => IsItemRightClickedCondition(GameController),
                timeoutS: timeout,
                token: token
                );
        }

        private async SyncTask<bool> AsyncWaitForRightClickedItemOffCursor(CancellationToken token, int timeout = 2)
        {
            return await ExecuteWithCancellationHandling(
                condition: () => !IsItemRightClickedCondition(GameController),
                timeoutS: timeout,
                token: token
                );
        }

        private async SyncTask<bool> ExecuteWithCancellationHandling(Func<bool> condition, int timeoutS, CancellationToken token)
        {
            using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            ctsTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutS));

            try
            {
                while (!ctsTimeout.Token.IsCancellationRequested)
                {
                    await AsyncWaitServerLatency(ctsTimeout.Token);

                    if (condition())
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private async SyncTask<bool> ExecuteWithCancellationHandling(Action action, Func<bool> condition, int timeoutS, CancellationToken token)
        {
            using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            ctsTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutS));

            try
            {
                while (!ctsTimeout.Token.IsCancellationRequested)
                {
                    action();
                    await AsyncWaitServerLatency(ctsTimeout.Token);

                    if (condition()) return true;
                }

                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private static bool TryGetCursorStateCondition(GameController GC, out MouseActionType cursorState) =>
            (cursorState = GC?.Game?.IngameState?.IngameUi?.Cursor?.Action ?? MouseActionType.Free) != MouseActionType.Free;

        private static bool IsItemOnLeftClickCondition(GameController GC) =>
            TryGetCursorStateCondition(GC, out var cursorState) && cursorState == MouseActionType.HoldItem;

        private static bool IsItemRightClickedCondition(GameController GC) =>
            TryGetCursorStateCondition(GC, out var cursorState) && cursorState == MouseActionType.UseItem;

        private static bool IsStashPanelOpenCondition(GameController GC) =>
            GC?.Game?.IngameState?.IngameUi?.StashElement?.IsVisible ?? false;

        private static bool IsInventoryPanelOpenCondition(GameController GC) =>
            GC?.Game?.IngameState?.IngameUi?.InventoryPanel?.IsVisible ?? false;

        private static bool IsAnItemPickedUpCondition(GameController GC) =>
            GC?.Game?.IngameState?.ServerData?.PlayerInventories[(int)InventorySlotE.Cursor1]?.Inventory?.ItemCount > 0;

        private static IList<Entity> GetItemsFromAnInventory(GameController GC, InventorySlotE invSlot) =>
            GC?.Game?.IngameState?.ServerData?.PlayerInventories[(int)invSlot]?.Inventory?.Items;

        private static bool IsInGameCondition(GameController GC) => GC?.Game?.IngameState?.InGame ?? false;

        private void SetCursorPositionAction(Vector2N position) => Input.SetCursorPos(GetRelativeWinPos(position));

        private bool IsMouseInPositionCondition(Vector2N position) => GetCurrentMousePosition() == position;

        private Vector2N GetCurrentMousePosition() => new(GameController.IngameState.MousePosX, GameController.IngameState.MousePosY);

        private static Entity GetPickedUpItem(GameController GC) =>
            IsAnItemPickedUpCondition(GC) ? GetItemsFromAnInventory(GC, InventorySlotE.Cursor1).FirstOrDefault() : null;

        private static string GetPickedUpItemBaseName(GameController GC) => GC?.Files.BaseItemTypes.Translate(GetPickedUpItem(GC)?.Path)?.BaseName ?? string.Empty;

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