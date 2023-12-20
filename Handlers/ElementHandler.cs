using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System;
using System.Threading;

namespace WheresMyCraftAt.Handlers
{
    public static class ElementHandler
    {
        private static GameController GC;
        private static WheresMyCraftAt Main;

        public static void Initialize(WheresMyCraftAt main)
        {
            Main = main;
            GC = main.GameController;
        }

        public static Element GetHoveredElementUIAction() =>
            GC?.Game?.IngameState?.UIHoverElement;

        public static bool IsElementsSameCondition(Element first, Element second) =>
            first.Address == second.Address;

        public static bool IsIngameUiElementOpenCondition(Func<IngameUIElements, Element> panelSelector) =>
            panelSelector(GC?.Game?.IngameState?.IngameUi)?.IsVisible ?? false;

        /*
         * Note: State doesnt change if you run out of the currency you were using if holding shift
         * TODO: Must check before each click that we have that item in the stash tab when this is called (Solve performance issues at a later date for this)
         */

        public static bool TryGetCursorStateCondition(out MouseActionType cursorState) =>
            (cursorState = GC?.Game?.IngameState?.IngameUi?.Cursor?.Action ?? MouseActionType.Free) != MouseActionType.Free;

        public static async SyncTask<bool> AsyncExecuteNotSameElementWithCancellationHandling(Element elementToChange, int timeoutS, CancellationToken token)
        {
            using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            ctsTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutS));

            try
            {
                while (!ctsTimeout.Token.IsCancellationRequested)
                {
                    await GameHandler.AsyncWaitServerLatency(ctsTimeout.Token);

                    if (!ElementHandler.IsElementsSameCondition(elementToChange, ElementHandler.GetHoveredElementUIAction()))
                    {
                        Main.DebugPrint($"AsyncExecuteNotSameElementWithCancellationHandling Pass", WheresMyCraftAt.LogMessageType.Success);
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
    }
}