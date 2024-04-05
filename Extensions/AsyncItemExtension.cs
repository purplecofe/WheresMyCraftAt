using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System;
using System.Threading;
using System.Windows.Forms;
using WheresMyCraftAt.Handlers;
using static WheresMyCraftAt.Enums.WheresMyCraftAt;

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
                if (!ElementHandler.IsElementsSameCondition(item, ElementHandler.GetHoveredElementUiAction()))
                {
                    return false;
                }
            }

            if (!await KeyHandler.AsyncButtonPress(button, token))
            {
                // Log warning if button press fails
                Logging.Logging.Add($"Failed to press {button}.", LogMessageType.Warning);
                return false;
            }

            var booleanCheck = false;

            switch (cursorStateCondition)
            {
                case MouseActionType.UseItem:
                case MouseActionType.HoldItem:
                    booleanCheck = await ItemHandler.AsyncWaitForNoItemOnCursor(token);
                    break;
                case MouseActionType.Free when rightClick:
                    booleanCheck = await ItemHandler.AsyncWaitForRightClickedItemOnCursor(token);
                    break;
                case MouseActionType.Free:
                    booleanCheck = await ItemHandler.AsyncWaitForItemOnCursor(token);
                    break;
            }

            // Log success of the click action
            Logging.Logging.Add($"Click action for {button} completed successfully.", LogMessageType.Info);
            return booleanCheck;
        }
        catch (OperationCanceledException)
        {
            // store states later possibly and apply state correction based on the progress?
            return false;
        }
    }
}