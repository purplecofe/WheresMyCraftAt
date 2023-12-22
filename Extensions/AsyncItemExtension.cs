using System;
using System.Threading;
using System.Windows.Forms;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using WheresMyCraftAt.Handlers;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Extensions;

public static class ItemExtensions
{
    public static async SyncTask<bool> AsyncTryClick(this NormalInventoryItem item, bool rightClick,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        try
        {
            ElementHandler.TryGetCursorStateCondition(out var cursorStateCondition);

            var clickPosition = item.GetClientRectCache.Center.ToVector2Num();
            var button = !rightClick ? Keys.LButton : Keys.RButton;

            Logging.Logging.Add($"AsyncTryClick Button is {button}", Enums.WheresMyCraftAt.LogMessageType.Success);

            if (!await MouseHandler.AsyncMoveMouse(clickPosition, token))
                if (!ElementHandler.IsElementsSameCondition(item, ElementHandler.GetHoveredElementUiAction()))
                    return false;

            if (!await KeyHandler.AsyncButtonPress(button, token))
                return false;

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

            return booleanCheck;
        }
        catch (OperationCanceledException)
        {
            // store states later possibly and apply state correction based on the progress?
            return false;
        }
    }

    public static async SyncTask<bool> AsyncTryApplyOrb(this NormalInventoryItem item, string currencyName,
        CancellationToken token)
    {
        try
        {
            var (item1, orbItem) = await StashHandler.AsyncTryGetItemInStash(currencyName, token);
            if (!item1)
            {
                Main.Stop();
                return false;
            }

            if (!await orbItem.AsyncTryClick(true, token))
            {
                Main.Stop();
                return false;
            }

            if (!await item.AsyncTryClick(false, token))
            {
                Main.Stop();
                return false;
            }

            if (!await ElementHandler.AsyncExecuteNotSameElementWithCancellationHandling(item, 3, token))
            {
                Main.Stop();
                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation logic or state correction here if necessary
            return false;
        }
    }
}