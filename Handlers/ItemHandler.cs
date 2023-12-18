using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System.Threading;

namespace WheresMyCraftAt.Handlers
{
    public static class ItemHandler
    {
        private static GameController GC;
        private static WheresMyCraftAt Main;

        public static void Initialize(WheresMyCraftAt main)
        {
            Main = main;
            GC = main.GameController;
        }

        public static async SyncTask<bool> AsyncWaitForItemOffCursor(CancellationToken token, int timeout = 2)
        {
            return await ExecuteHandler.ExecuteWithCancellationHandling(
                condition: () => !InventoryHandler.IsAnItemPickedUpCondition(),
                timeoutS: timeout,
                token: token
                );
        }

        public static async SyncTask<bool> AsyncWaitForItemOnCursor(CancellationToken token, int timeout = 2)
        {
            return await ExecuteHandler.ExecuteWithCancellationHandling(
                condition: () => InventoryHandler.IsAnItemPickedUpCondition(),
                timeoutS: timeout,
                token: token
                );
        }

        public static async SyncTask<bool> AsyncWaitForRightClickedItemOffCursor(CancellationToken token, int timeout = 2)
        {
            return await ExecuteHandler.ExecuteWithCancellationHandling(
                condition: () => !IsItemRightClickedCondition(),
                timeoutS: timeout,
                token: token
                );
        }

        public static async SyncTask<bool> AsyncWaitForRightClickedItemOnCursor(CancellationToken token, int timeout = 2)
        {
            return await ExecuteHandler.ExecuteWithCancellationHandling(
                condition: () => IsItemRightClickedCondition(),
                timeoutS: timeout,
                token: token
                );
        }

        public static string GetBaseNameFromItem(Entity item) => GetBaseNameFromPath(item?.Path);

        public static string GetBaseNameFromItem(NormalInventoryItem item) => GetBaseNameFromPath(item.Entity?.Path);

        public static string GetBaseNameFromPath(string path) => GC?.Files.BaseItemTypes.Translate(path)?.BaseName ?? string.Empty;

        public static string GetPickedUpItemBaseName() =>
            InventoryHandler.TryGetPickedUpItem(out Entity pickedUpItem) ? GetBaseNameFromPath(pickedUpItem.Path) : string.Empty;

        public static bool IsItemOnLeftClickCondition() =>
            ElementHandler.TryGetCursorStateCondition(out var cursorState) && cursorState == MouseActionType.HoldItem;

        public static bool IsItemRightClickedCondition() =>
            ElementHandler.TryGetCursorStateCondition(out var cursorState) && cursorState == MouseActionType.UseItem;
    }
}