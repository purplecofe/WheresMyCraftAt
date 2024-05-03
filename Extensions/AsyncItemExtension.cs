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
            Logging.Logging.Add($"AsyncTryClick: Clicking with {button} on item.", LogMessageType.Info);

            if (!await MouseHandler.AsyncMoveMouse(clickPosition, token))
            {
                Logging.Logging.Add($"AsyncTryClick: Failed MouseHandler.AsyncMoveMouse, attempting ElementHandler.IsElementsSameCondition.", LogMessageType.Warning);
                if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(() => ElementHandler.IsElementsSameCondition(item, ElementHandler.GetHoveredElementUiAction()), 2, HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), token))
                {
                    Logging.Logging.Add($"AsyncTryClick: Failed ElementHandler.IsElementsSameCondition after failing MouseHandler.AsyncMoveMouse.", LogMessageType.Error);
                    return false;
                }
            }

            if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(() => ElementHandler.IsElementsSameCondition(item, ElementHandler.GetHoveredElementUiAction()), 2, HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), token))
            {
                Logging.Logging.Add("AsyncTryClick: Failed ElementHandler.IsElementsSameCondition.", LogMessageType.Error);
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

                        Logging.Logging.Add($"AsyncTryClick: MouseActionType.Free when rightClick retry count {loopCount}, attempts left {maxRetriesAllowed - loopCount}", LogMessageType.Debug);
                        if (!await ItemHandler.AsyncWaitForRightClickedItemOnCursor(token))
                        {
                            if (loopCount > maxRetriesAllowed)
                            {
                                Logging.Logging.Add($"AsyncTryClick: Failed to press {button} and get an item on the cursor.", LogMessageType.Error);
                                return false;
                            }
                            await KeyHandler.AsyncButtonPress(button, token);
                        }
                        else
                        {
                            Logging.Logging.Add($"AsyncTryClick: Successfully pressed {button} and got an item on the cursor.", LogMessageType.Info);
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
            Logging.Logging.Add($"Click action for {button} completed successfully.", LogMessageType.Info);
            return booleanCheck;
        }
        catch (OperationCanceledException e)
        {
            Logging.Logging.Add($"AsyncTryClick: Catch!\n{e.Message}\n{e.StackTrace}", LogMessageType.Error);
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
            Logging.Logging.Add($"AsyncTryClick: Clicking with {button} on item.", LogMessageType.Info);

            Element matchingElement;
            if (!await MouseHandler.AsyncMoveMouse(clickPosition, token))
            {
                Logging.Logging.Add($"AsyncTryClick: Failed MouseHandler.AsyncMoveMouse, attempting ElementHandler.IsElementsSameCondition.", LogMessageType.Warning);

                if (!ElementHandler.TryGetMatchingElementFromSlotItem(item, out matchingElement))
                {
                    Logging.Logging.Add($"AsyncTryClick: Failed to get matching element from slot '{item.InventoryPositionNum}'", LogMessageType.Error);
                    return false;
                }
                if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(() => ElementHandler.IsElementsSameCondition(matchingElement, ElementHandler.GetHoveredElementUiAction()), 2, HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), token))
                {
                    Logging.Logging.Add($"AsyncTryClick: Failed ElementHandler.IsElementsSameCondition after failing MouseHandler.AsyncMoveMouse.", LogMessageType.Error);
                    return false;
                }
            }

            if (!ElementHandler.TryGetMatchingElementFromSlotItem(item, out matchingElement))
            {
                Logging.Logging.Add($"AsyncTryClick: Failed to get matching element from slot '{item.InventoryPositionNum}'", LogMessageType.Error);
                return false;
            }
            if (!await ExecuteHandler.AsyncExecuteWithCancellationHandling(() => ElementHandler.IsElementsSameCondition(matchingElement, ElementHandler.GetHoveredElementUiAction()), 2, HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), token))
            {
                Logging.Logging.Add("AsyncTryClick: Failed ElementHandler.IsElementsSameCondition.", LogMessageType.Error);
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

                        Logging.Logging.Add($"AsyncTryClick: MouseActionType.Free when rightClick retry count {loopCount}, attempts left {maxRetriesAllowed - loopCount}", LogMessageType.Debug);
                        if (!await ItemHandler.AsyncWaitForRightClickedItemOnCursor(token))
                        {
                            if (loopCount > maxRetriesAllowed)
                            {
                                Logging.Logging.Add($"AsyncTryClick: Failed to press {button} and get an item on the cursor.", LogMessageType.Error);
                                return false;
                            }
                            await KeyHandler.AsyncButtonPress(button, token);
                        }
                        else
                        {
                            Logging.Logging.Add($"AsyncTryClick: Successfully pressed {button} and got an item on the cursor.", LogMessageType.Info);
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
            Logging.Logging.Add($"Click action for {button} completed successfully.", LogMessageType.Info);
            return booleanCheck;
        }
        catch (OperationCanceledException e)
        {
            Logging.Logging.Add($"AsyncTryClick: Catch!\n{e.Message}\n{e.StackTrace}", LogMessageType.Error);
            return false;
        }
    }
}