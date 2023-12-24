using System;
using System.Threading;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System.Runtime.InteropServices;
using WheresMyCraftAt.Extensions;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class ElementHandler
{
    public static Element GetHoveredElementUiAction()
    {
        return Main.GameController?.Game?.IngameState?.UIHoverElement;
    }

    public static bool IsElementsSameCondition(Element first, Element second)
    {
        return first.Address == second.Address;
    }

    public static bool IsInGameUiElementOpenCondition(Func<IngameUIElements, Element> panelSelector)
    {
        return panelSelector(Main.GameController?.Game?.IngameState?.IngameUi)?.IsVisible ?? false;
    }

    /*
     * Note: State doesnt change if you run out of the currency you were using if holding shift
     * TODO: Must check before each click that we have that item in the stash tab when this is called (Solve performance issues at a later date for this)
     */

    public static bool TryGetCursorStateCondition(out MouseActionType cursorState)
    {
        var gameController = Main.GameController;

        if (gameController?.Game?.IngameState?.IngameUi?.Cursor != null)
        {
            cursorState = gameController.Game.IngameState.IngameUi.Cursor.Action;
            return true;
        }

        cursorState = MouseActionType.HoldItemForSell; // Default value if the state cannot be retrieved
        return false;
    }

    public static async SyncTask<bool> AsyncExecuteNotSameElementWithCancellationHandling(Element elementToChange,
        int timeoutS, CancellationToken token)
    {
        using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        ctsTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutS));

        try
        {
            while (!ctsTimeout.Token.IsCancellationRequested)
            {
                await GameHandler.AsyncWaitServerLatency(ctsTimeout.Token);

                if (IsElementsSameCondition(elementToChange, GetHoveredElementUiAction()))
                    continue;

                Logging.Logging.Add(
                    "AsyncExecuteNotSameElementWithCancellationHandling Pass",
                    Enums.WheresMyCraftAt.LogMessageType.Success
                );

                return true;
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public static async SyncTask<bool> AsyncExecuteNotSameItemWithCancellationHandling(long itemToChange, int timeoutS,
        CancellationToken token)
    {
        using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        ctsTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutS));

        try
        {
            while (!ctsTimeout.Token.IsCancellationRequested)
            {
                await GameHandler.AsyncWait(HelperHandler.GetRandomTimeInRange(Main.Settings.MinMaxRandomDelay), token);
                var hoveredEntity = GetHoveredElementUiAction().Entity.Address;

                if (HelperHandler.IsAddressSameCondition(itemToChange, hoveredEntity))
                {
                    Logging.Logging.Add(
                        $"AsyncExecuteNotSameItemWithCancellationHandling !IsAddressSameCondition({itemToChange:X}, {hoveredEntity:X}) Fail",
                        Enums.WheresMyCraftAt.LogMessageType.Error
                    );

                    continue;
                }

                Logging.Logging.Add(
                    $"AsyncExecuteNotSameItemWithCancellationHandling !IsElementsSameCondition({itemToChange:X}, {hoveredEntity:X}) Pass",
                    Enums.WheresMyCraftAt.LogMessageType.Success
                );

                return true;
            }

            return false;
        }
        catch (OperationCanceledException)
        {
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
                Logging.Logging.Add($"AsyncTryApplyOrb StashHandler.AsyncTryGetItemInStash() has failed", Enums.WheresMyCraftAt.LogMessageType.Error);
                Main.Stop();
                return false;
            }

            var storeAddressOfItem = item.Item.Address;
            Logging.Logging.Add($"AsyncTryApplyOrb storeAddressOfItem is {storeAddressOfItem:X}", Enums.WheresMyCraftAt.LogMessageType.Warning);

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

            if (!await AsyncExecuteNotSameItemWithCancellationHandling(storeAddressOfItem, Main.Settings.ActionTimeoutInSeconds, token))
            {
                Logging.Logging.Add($"AsyncTryApplyOrb AsyncExecuteNotSameItemWithCancellationHandling(item, {Main.Settings.ActionTimeoutInSeconds.Value}, token) is false",
                                    Enums.WheresMyCraftAt.LogMessageType.Error);
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