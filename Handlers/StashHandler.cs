using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers
{
    public static class StashHandler
    {
        public static async SyncTask<bool> AsyncWaitForStashOpen(CancellationToken token, int timeout = 2)
        {
            return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                condition: () => IsStashPanelOpenCondition(),
                timeoutS: timeout,
                token: token
                );
        }

        public static InventoryType GetTypeOfCurrentVisibleStash() =>
            Main.GameController?.Game?.IngameState.IngameUi?.StashElement?.VisibleStash?.InvType ?? InventoryType.InvalidInventory;

        public static IList<NormalInventoryItem> GetVisibleStashInventory() =>
            Main.GameController?.Game?.IngameState.IngameUi?.StashElement?.VisibleStash?.VisibleInventoryItems;

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


        public static async SyncTask<Tuple<bool, NormalInventoryItem>> AsyncTryGetItemInStash(string currencyName, CancellationToken token)
        {
            NormalInventoryItem orbItem = null;

            bool result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                condition: () => TryGetItemInStash(currencyName, out orbItem),
                timeoutS: 2,
                loopDelay: 1,
                token: token
            );

            return Tuple.Create(result, orbItem);
        }
        public static async SyncTask<Tuple<bool, NormalInventoryItem>> AsyncTryGetStashSpecialSlot(SpecialSlot slotType, CancellationToken token)
        {
            NormalInventoryItem inventoryItem = null;

            bool result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                condition: () => TryGetStashSpecialSlot(slotType, out inventoryItem),
                timeoutS: 2,
                loopDelay: 1,
                token: token
            );

            return Tuple.Create(result, inventoryItem);
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