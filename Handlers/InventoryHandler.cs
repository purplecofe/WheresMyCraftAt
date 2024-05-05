using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static ExileCore.PoEMemory.MemoryObjects.ServerInventory;
using static WheresMyCraftAt.Enums.WheresMyCraftAt;
using static WheresMyCraftAt.WheresMyCraftAt;
using Vector2 = System.Numerics.Vector2;

namespace WheresMyCraftAt.Handlers;

public static class InventoryHandler
{
    public static async SyncTask<bool> AsyncWaitForInventoryOpen(CancellationToken token, int timeout = 2)
    {
        Logging.Logging.Add("Waiting for inventory to open.", LogMessageType.Info);

        var result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(IsInventoryPanelOpenCondition, timeout, token);

        Logging.Logging.Add($"Inventory open status: {result}.", LogMessageType.Info);
        return result;
    }

    public static IList<Entity> GetItemsFromAnInventory(InventorySlotE invSlot) =>
        Main.GameController?.Game?.IngameState?.ServerData?.PlayerInventories[(int)invSlot]?.Inventory?.Items;

    public static IList<InventSlotItem> GetInventorySlotItemsFromAnInventory(InventorySlotE invSlot) =>
        Main.GameController?.Game?.IngameState?.ServerData?.PlayerInventories[(int)invSlot]?.Inventory?.InventorySlotItems;

    public static IList<InventSlotItem> TryGetValidCraftingItemsFromAnInventory(InventorySlotE invSlot)
    {
        var items = GetInventorySlotItemsFromAnInventory(invSlot)
            .Where(item => item.Item.IsValid 
                           && item.GetClientRect().Size != Size2F.Zero 
                           && item.Item.TryGetComponent<Base>(out var baseComp)
                           && baseComp.Address != 0
                           && item.Item.TryGetComponent<Mods>(out var modComp)
                           && modComp.Address != 0)
            .ToList();

        return items;
    }

    public static bool IsAnItemPickedUpCondition() =>
        Main.GameController?.Game?.IngameState?.ServerData?.PlayerInventories[(int)InventorySlotE.Cursor1]?.Inventory?.ItemCount > 0;

    public static bool IsInventoryPanelOpenCondition()
    {
        return ElementHandler.IsInGameUiElementVisibleCondition(ui => ui.InventoryPanel);
    }

    public static bool TryGetInventoryItemFromSlot(Vector2 invSlot, out InventSlotItem inventoryItem)
    {
        var items = TryGetValidCraftingItemsFromAnInventory(InventorySlotE.MainInventory1);
        inventoryItem = items is { Count: > 0 }
            ? items.FirstOrDefault(item => item.InventoryPositionNum == invSlot)
            : null;

        return inventoryItem != null;
    }

    public static async SyncTask<Tuple<bool, InventSlotItem>> AsyncTryGetInventoryItemFromSlot(Vector2 invSlot, CancellationToken token)
    {
        InventSlotItem inventoryItem = null;

        Logging.Logging.Add($"Attempting to find slot '{invSlot}' in inventory.", LogMessageType.Info);

        var result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(() => TryGetInventoryItemFromSlot(invSlot, out inventoryItem),
            2,
            HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS),
            token);

        switch (result)
        {
            case false:
                Logging.Logging.Add($"AsyncTryGetInventoryItemFromSlot: Slot '{invSlot}' found status: {result}.", LogMessageType.Error);
                Main.Stop();
                return Tuple.Create(result, inventoryItem);

            default:
                Logging.Logging.Add($"AsyncTryGetInventoryItemFromSlot: Slot '{invSlot}' found status: {result}.", LogMessageType.Info);
                return Tuple.Create(result, inventoryItem);
        }
    }

    public static bool TryGetPickedUpItem(out Entity pickedUpItem)
    {
        pickedUpItem = IsAnItemPickedUpCondition() ? GetItemsFromAnInventory(InventorySlotE.Cursor1).FirstOrDefault() : null;

        if (pickedUpItem != null)
        {
            Logging.Logging.Add("An item is picked up.", LogMessageType.Info);
        }
        else
        {
            Logging.Logging.Add("No item is picked up.", LogMessageType.Warning);
        }

        return pickedUpItem != null;
    }
}