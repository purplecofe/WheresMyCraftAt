using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static WheresMyCraftAt.Enums.WheresMyCraftAt;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class InventoryHandler
{
    public static async SyncTask<bool> AsyncWaitForInventoryOpen(CancellationToken token, int timeout = 2)
    {
        Logging.Logging.Add("AsyncWaitForInventoryOpen: Waiting for inventory to open.", LogMessageType.Info);

        var result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            IsInventoryPanelOpenCondition,
            timeout,
            token
        );

        Logging.Logging.Add($"AsyncWaitForInventoryOpen: Inventory open status: {result}.", LogMessageType.Info);
        return result;
    }

    public static IList<Entity> GetItemsFromAnInventory(InventorySlotE invSlot)
    {
        return Main.GameController?.Game?.IngameState?.ServerData?.PlayerInventories[(int)invSlot]?.Inventory?.Items;
    }

    public static bool IsAnItemPickedUpCondition()
    {
        return Main.GameController?.Game?.IngameState?.ServerData?.PlayerInventories[(int)InventorySlotE.Cursor1]
                   ?.Inventory?.ItemCount > 0;
    }

    public static bool IsInventoryPanelOpenCondition()
    {
        return ElementHandler.IsInGameUiElementOpenCondition(ui => ui.InventoryPanel);
    }

    public static bool TryGetPickedUpItem(out Entity pickedUpItem)
    {
        pickedUpItem = IsAnItemPickedUpCondition()
            ? GetItemsFromAnInventory(InventorySlotE.Cursor1).FirstOrDefault()
            : null;

        if (pickedUpItem != null)
            Logging.Logging.Add("TryGetPickedUpItem: An item is picked up.", LogMessageType.Info);
        else
            Logging.Logging.Add("TryGetPickedUpItem: No item is picked up.", LogMessageType.Debug);

        return pickedUpItem != null;
    }
}