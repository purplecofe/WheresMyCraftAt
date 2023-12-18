using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers
{
    public static class StashHandler
    {
        private static GameController GC;
        private static WheresMyCraftAt Main;

        public static void Initialize(WheresMyCraftAt main)
        {
            Main = main;
            GC = main.GameController;
        }

        public static async SyncTask<bool> AsyncWaitForStashOpen(CancellationToken token, int timeout = 2)
        {
            return await ExecuteHandler.ExecuteWithCancellationHandling(
                condition: () => IsStashPanelOpenCondition(),
                timeoutS: timeout,
                token: token
                );
        }

        public static InventoryType GetTypeOfCurrentVisibleStash() =>
            GC?.Game?.IngameState.IngameUi?.StashElement?.VisibleStash?.InvType ?? InventoryType.InvalidInventory;

        public static IList<NormalInventoryItem> GetVisibleStashInventory() =>
            GC?.Game?.IngameState.IngameUi?.StashElement?.VisibleStash?.VisibleInventoryItems;

        public static bool IsStashPanelOpenCondition() => ElementHandler.IsIngameUiElementOpenCondition(ui => ui.StashElement);

        public static bool IsVisibleStashValidCondition() =>
            GetTypeOfCurrentVisibleStash() != InventoryType.InvalidInventory;

        public static bool TryGetItemInStash(string baseName, out NormalInventoryItem foundItem)
        {
            foundItem = TryGetVisibleStashInventory(out var stashContents)
                        ? stashContents.FirstOrDefault(item => ItemHandler.GetBaseNameFromItem(item) == baseName)
                        : null;

            return foundItem != null;
        }

        public static bool TryGetStashSpecialSlot(SpecialSlot slotType, out NormalInventoryItem inventoryItem)
        {
            inventoryItem = TryGetVisibleStashInventory(out var stashContents)
                        ? stashContents.FirstOrDefault(item => item.Elem.Size == Main.specialSlotDimensionMap[slotType])
                        : null;

            return inventoryItem != null;
        }

        public static bool TryGetVisibleStashInventory(out IList<NormalInventoryItem> inventoryItems)
        {
            inventoryItems = IsVisibleStashValidCondition() ? GetVisibleStashInventory() : null;
            return inventoryItems != null;
        }
    }
}