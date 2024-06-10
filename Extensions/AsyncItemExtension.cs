using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System;
using System.Threading;
using System.Windows.Forms;
using WheresMyCraftAt.Handlers;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using static WheresMyCraftAt.Enums.WheresMyCraftAt;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Extensions;

public static class ItemExtensions
{
    public static async SyncTask<bool> AsyncTryClick(this NormalInventoryItem item, bool rightClick, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        try
        {
            ElementHandler.TryGetCursorStateCondition(out var cursorStateCondition);
            var clickPosition = item.GetClientRectCache.GetRandomPointWithinWithPerlin(15, 80);
            var button = !rightClick ? Keys.LButton : Keys.RButton;

            // Log info about the click action
            Logging.Logging.LogMessage($"AsyncTryClick<NormalInventoryItem>: Clicking with {button} on item.", LogMessageType.Info);

            if (!await MouseHandler.AsyncMoveMouse(clickPosition, token))
            {
                Logging.Logging.LogMessage($"AsyncTryClick<NormalInventoryItem>: Failed MouseHandler.AsyncMoveMouse, attempting ElementHandler.IsElementsSameCondition.", LogMessageType.Warning);
                if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(() => ElementHandler.IsElementsSameCondition(item, ElementHandler.GetHoveredElementUiAction()), 2, HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), token))
                {
                    Logging.Logging.LogMessage($"AsyncTryClick<NormalInventoryItem>: Failed ElementHandler.IsElementsSameCondition after failing MouseHandler.AsyncMoveMouse.", LogMessageType.Error);
                    return false;
                }
            }

            if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(() => ElementHandler.IsElementsSameCondition(item, ElementHandler.GetHoveredElementUiAction()), 2, HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), token))
            {
                Logging.Logging.LogMessage("AsyncTryClick<NormalInventoryItem>: Failed ElementHandler.IsElementsSameCondition.", LogMessageType.Error);
                return false;
            }

            await KeyHandler.AsyncButtonPress(button, token);

            var booleanCheck = false;
            var mapLoopsAllowed = 10;
            var maxRetriesAllowed = 3;

            switch (cursorStateCondition)
            {
                case MouseActionType.UseItem:
                case MouseActionType.HoldItem:
                    booleanCheck = await ItemHandler.AsyncWaitForNoItemOnCursor(token);
                    break;
                case MouseActionType.Free when rightClick:
                    for (var loopCount = 0; loopCount < mapLoopsAllowed; loopCount++)
                    {

                        Logging.Logging.LogMessage($"AsyncTryClick<NormalInventoryItem>: MouseActionType.Free when rightClick retry count {loopCount}, attempts left {maxRetriesAllowed - loopCount}", LogMessageType.Debug);
                        if (!await ItemHandler.AsyncWaitForRightClickedItemOnCursor(token))
                        {
                            if (loopCount > maxRetriesAllowed)
                            {
                                Logging.Logging.LogMessage($"AsyncTryClick<NormalInventoryItem>: Failed to press {button} and get an item on the cursor.", LogMessageType.Error);
                                return false;
                            }
                            await KeyHandler.AsyncButtonPress(button, token);
                        }
                        else
                        {
                            Logging.Logging.LogMessage($"AsyncTryClick<NormalInventoryItem>: Successfully pressed {button} and got an item on the cursor.", LogMessageType.Info);
                            booleanCheck = true;
                            break;
                        }
                    }
                    break;
                case MouseActionType.Free:
                    booleanCheck = await ItemHandler.AsyncWaitForItemOnCursor(token);
                    break;
            }

            // Log success of the click action
            Logging.Logging.LogMessage($"AsyncTryClick<NormalInventoryItem>: Click action for {button} completed successfully.", LogMessageType.Info);
            return booleanCheck;
        }
        catch (OperationCanceledException e)
        {
            Logging.Logging.LogMessage($"AsyncTryClick: Catch!\n{e.Message}\n{e.StackTrace}", LogMessageType.Error);
            return false;
        }
    }
    public static async SyncTask<bool> AsyncTryClick(this InventSlotItem item, bool rightClick, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        try
        {
            ElementHandler.TryGetCursorStateCondition(out var cursorStateCondition);
            var clickPosition = item.GetClientRect().GetRandomPointWithinWithPerlin(15, 80);
            var button = !rightClick ? Keys.LButton : Keys.RButton;

            // Log info about the click action
            Logging.Logging.LogMessage($"AsyncTryClick<InventSlotItem>: Clicking with {button} on item.", LogMessageType.Info);

            if (!await MouseHandler.AsyncMoveMouse(clickPosition, token))
            {
                Logging.Logging.LogMessage($"AsyncTryClick<InventSlotItem>: Failed MouseHandler.AsyncMoveMouse, attempting ElementHandler.IsElementsSameCondition.", LogMessageType.Warning);
                if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(() => ElementHandler.IsElementsSameCondition(item, ElementHandler.GetHoveredElementUiAction()), 2, HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), token))
                {
                    Logging.Logging.LogMessage($"AsyncTryClick<InventSlotItem>: Failed ElementHandler.IsElementsSameCondition after failing MouseHandler.AsyncMoveMouse.", LogMessageType.Error);
                    return false;
                }
            }

            if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(() => ElementHandler.IsElementsSameCondition(item, ElementHandler.GetHoveredElementUiAction()), 2, HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), token))
            {
                Logging.Logging.LogMessage("AsyncTryClick<InventSlotItem>: Failed ElementHandler.IsElementsSameCondition.", LogMessageType.Error);
                return false;
            }

            await KeyHandler.AsyncButtonPress(button, token);

            var booleanCheck = false;
            var mapLoopsAllowed = 10;
            var maxRetriesAllowed = 3;

            switch (cursorStateCondition)
            {
                case MouseActionType.UseItem:
                case MouseActionType.HoldItem:
                    booleanCheck = await ItemHandler.AsyncWaitForNoItemOnCursor(token);
                    break;
                case MouseActionType.Free when rightClick:
                    for (var loopCount = 0; loopCount < mapLoopsAllowed; loopCount++)
                    {

                        Logging.Logging.LogMessage($"AsyncTryClick<InventSlotItem>: MouseActionType.Free when rightClick retry count {loopCount}, attempts left {maxRetriesAllowed - loopCount}", LogMessageType.Debug);
                        if (!await ItemHandler.AsyncWaitForRightClickedItemOnCursor(token))
                        {
                            if (loopCount > maxRetriesAllowed)
                            {
                                Logging.Logging.LogMessage($"AsyncTryClick<InventSlotItem>: Failed to press {button} and get an item on the cursor.", LogMessageType.Error);
                                return false;
                            }
                            await KeyHandler.AsyncButtonPress(button, token);
                        }
                        else
                        {
                            Logging.Logging.LogMessage($"AsyncTryClick<InventSlotItem>: Successfully pressed {button} and got an item on the cursor.", LogMessageType.Info);
                            booleanCheck = true;
                            break;
                        }
                    }
                    break;
                case MouseActionType.Free:
                    booleanCheck = await ItemHandler.AsyncWaitForItemOnCursor(token);
                    break;
            }

            // Log success of the click action
            Logging.Logging.LogMessage($"AsyncTryClick<InventSlotItem>: Click action for {button} completed successfully.", LogMessageType.Info);
            return booleanCheck;
        }
        catch (OperationCanceledException e)
        {
            Logging.Logging.LogMessage($"AsyncTryClick<InventSlotItem>: Catch!\n{e.Message}\n{e.StackTrace}", LogMessageType.Error);
            return false;
        }
    }
}