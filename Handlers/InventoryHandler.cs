using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace WheresMyCraftAt.Handlers
{
    public static class InventoryHandler
    {
        private static GameController GC;
        private static WheresMyCraftAt Main;

        public static void Initialize(WheresMyCraftAt main)
        {
            Main = main;
            GC = main.GameController;
        }

        public static async SyncTask<bool> AsyncWaitForInventoryOpen(CancellationToken token, int timeout = 2)
        {
            return await ExecuteHandler.ExecuteWithCancellationHandling(
                condition: () => IsInventoryPanelOpenCondition(),
                timeoutS: timeout,
                token: token
                );
        }

        public static IList<Entity> GetItemsFromAnInventory(InventorySlotE invSlot) =>
            GC?.Game?.IngameState?.ServerData?.PlayerInventories[(int)invSlot]?.Inventory?.Items;

        public static bool IsAnItemPickedUpCondition() =>
            GC?.Game?.IngameState?.ServerData?.PlayerInventories[(int)InventorySlotE.Cursor1]?.Inventory?.ItemCount > 0;

        public static bool IsInventoryPanelOpenCondition() => ElementHandler.IsIngameUiElementOpenCondition(ui => ui.InventoryPanel);
        public static bool TryGetPickedUpItem(out Entity pickedUpItem)
        {
            pickedUpItem = IsAnItemPickedUpCondition()
                           ? GetItemsFromAnInventory(InventorySlotE.Cursor1).FirstOrDefault()
                           : null;

            return pickedUpItem != null;
        }
    }
}