using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System.Threading;
using WheresMyCraftAt.Extensions;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers
{
    public static class ItemHandler
    {
        public static async SyncTask<bool> AsyncChangeItemRarity(SpecialSlot slot, ItemRarity rarity, CancellationToken token)
        {
            if (!StashHandler.TryGetStashSpecialSlot(slot, out var slotItem))
                return false;

            return await slotItem.AsyncChangeItemRarity(rarity, token);
        }

        public static async SyncTask<bool> AsyncTryApplyOrbToSlot(SpecialSlot slot, string orbName, CancellationToken token)
        {
            if (!StashHandler.TryGetStashSpecialSlot(slot, out var slotItem))
                return false;

            return await slotItem.AsyncTryApplyOrb(orbName, token);
        }

        public static bool IsItemRarityFromSpecialSlotCondition(SpecialSlot slot, ItemRarity rarity)
        {
            if (!StashHandler.TryGetStashSpecialSlot(slot, out var specialItem))
                return false;

            var result = GetRarityFromItem(specialItem.Item) == rarity;

            Main.DebugPrint($"IsItemRarityFromSpecialSlotCondition = {result}", LogMessageType.Special);

            return result;
        }

        public static async SyncTask<bool> AsyncWaitForItemOffCursor(CancellationToken token, int timeout = 2)
        {
            return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                condition: () => !InventoryHandler.IsAnItemPickedUpCondition(),
                timeoutS: timeout,
                token: token
                );
        }

        public static async SyncTask<bool> AsyncWaitForItemOnCursor(CancellationToken token, int timeout = 2)
        {
            return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                condition: () => InventoryHandler.IsAnItemPickedUpCondition(),
                timeoutS: timeout,
                token: token
                );
        }

        public static async SyncTask<bool> AsyncWaitForRightClickedItemOffCursor(CancellationToken token, int timeout = 2)
        {
            return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                condition: () => !IsItemRightClickedCondition(),
                timeoutS: timeout,
                token: token
                );
        }

        public static async SyncTask<bool> AsyncWaitForRightClickedItemOnCursor(CancellationToken token, int timeout = 2)
        {
            return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
                condition: () => IsItemRightClickedCondition(),
                timeoutS: timeout,
                token: token
                );
        }

        public static ItemRarity GetRarityFromItem(Entity item)
        {
            if (item.TryGetComponent<Mods>(out var modsComp))
            {
                Main.DebugPrint($"GetRarityFromItem: {modsComp.ItemRarity}", LogMessageType.Special);
                return modsComp.ItemRarity;
            }
            else
            {
                Main.DebugPrint($"GetRarityFromItem: Could not get mods component from item.", LogMessageType.Error);
                return ItemRarity.Normal;
            }
        }

        public static string GetBaseNameFromItem(Entity item) =>
            GetBaseNameFromPath(item?.Path);

        public static string GetBaseNameFromItem(NormalInventoryItem item) =>
            GetBaseNameFromPath(item.Entity?.Path);

        public static string GetBaseNameFromPath(string path) =>
            Main.GameController?.Files.BaseItemTypes.Translate(path)?.BaseName ?? string.Empty;

        public static string GetPickedUpItemBaseName() =>
            InventoryHandler.TryGetPickedUpItem(out Entity pickedUpItem) ? GetBaseNameFromPath(pickedUpItem.Path) : string.Empty;

        public static bool IsItemOnLeftClickCondition() =>
            ElementHandler.TryGetCursorStateCondition(out var cursorState) && cursorState == MouseActionType.HoldItem;

        public static bool IsItemRightClickedCondition() =>
            ElementHandler.TryGetCursorStateCondition(out var cursorState) && cursorState == MouseActionType.UseItem;
    }
}