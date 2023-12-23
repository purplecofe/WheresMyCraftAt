using System;
using System.Threading;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
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
}