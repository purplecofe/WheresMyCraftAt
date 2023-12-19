using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared;
using ExileCore.Shared.Helpers;
using System;
using System.Threading;
using System.Windows.Forms;
using WheresMyCraftAt.Handlers;

namespace WheresMyCraftAt.Extensions
{
    public static class ItemExtensions
    {
        private static GameController GC;
        private static WheresMyCraftAt Main;

        public static void Initialize(WheresMyCraftAt main)
        {
            Main = main;
            GC = main.GameController;
        }

        public static async SyncTask<bool> AsyncTryClick(this NormalInventoryItem item, bool rightClick, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                var clickPosition = item.GetClientRectCache.Center.ToVector2Num();
                var button = !rightClick ? Keys.LButton : Keys.RButton;

                if (!await MouseHandler.AsyncMoveMouse(clickPosition, token)
                    || ElementHandler.IsElementsSameCondition(item, ElementHandler.GetHoveredElementUIAction()))
                    return false;

                token.ThrowIfCancellationRequested();

                if (!await KeyHandler.AsyncButtonPress(button, token))
                    return false;

                token.ThrowIfCancellationRequested();

                var itemOnCursor = !rightClick ? 
                    await ItemHandler.AsyncWaitForItemOnCursor(token) : 
                    await ItemHandler.AsyncWaitForRightClickedItemOnCursor(token);

                Main.DebugPrint($"itemOnCursor = {itemOnCursor}", WheresMyCraftAt.LogMessageType.Info);

                if (!itemOnCursor)
                    return false;

                return true;
            }
            catch (OperationCanceledException)
            {
                // store states later possibly and apply state correction based on the progress?
                return false;
            }
        }
    }
}