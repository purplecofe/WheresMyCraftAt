using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class StashHandler
{
    public static async SyncTask<bool> AsyncWaitForStashOpen(CancellationToken token, int timeout = 2)
    {
        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            IsStashPanelOpenCondition,
            timeout,
            token
        );
    }

    public static InventoryType GetTypeOfCurrentVisibleStash()
    {
        return Main.GameController?.Game?.IngameState.IngameUi?.StashElement?.VisibleStash?.InvType ??
               InventoryType.InvalidInventory;
    }

    public static IList<NormalInventoryItem> GetVisibleStashInventory()
    {
        return Main.GameController?.Game?.IngameState.IngameUi?.StashElement?.VisibleStash?.VisibleInventoryItems;
    }

    public static bool IsStashPanelOpenCondition()
    {
        return ElementHandler.IsIngameUiElementOpenCondition(ui => ui.StashElement);
    }

    public static bool IsVisibleStashValidCondition()
    {
        return GetTypeOfCurrentVisibleStash() != InventoryType.InvalidInventory;
    }

    public static bool TryGetItemInStash(string baseName, out NormalInventoryItem foundItem)
    {
        foundItem = TryGetVisibleStashInventory(out var stashContents)
            ? stashContents.FirstOrDefault(item => ItemHandler.GetBaseNameFromItem(item) == baseName)
            : null;

        return foundItem != null;
    }


    public static async SyncTask<Tuple<bool, NormalInventoryItem>> AsyncTryGetItemInStash(string currencyName,
        CancellationToken token)
    {
        NormalInventoryItem orbItem = null;

        var result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            () => TryGetItemInStash(currencyName, out orbItem),
            2,
            1,
            token
        );

        return Tuple.Create(result, orbItem);
    }

    public static async SyncTask<Tuple<bool, NormalInventoryItem>> AsyncTryGetStashSpecialSlot(Enums.WheresMyCraftAt.SpecialSlot slotType,
        CancellationToken token)
    {
        NormalInventoryItem inventoryItem = null;

        var result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            () => TryGetStashSpecialSlot(slotType, out inventoryItem),
            2,
            1,
            token
        );

        return Tuple.Create(result, inventoryItem);
    }

    public static bool TryGetStashSpecialSlot(Enums.WheresMyCraftAt.SpecialSlot slotType, out NormalInventoryItem inventoryItem)
    {
        inventoryItem = TryGetVisibleStashInventory(out var stashContents)
            ? stashContents.FirstOrDefault(item => item.Elem.Size == Main.SpecialSlotDimensionMap[slotType])
            : null;

        return inventoryItem != null;
    }

    public static bool TryGetVisibleStashInventory(out IList<NormalInventoryItem> inventoryItems)
    {
        inventoryItems = IsVisibleStashValidCondition() ? GetVisibleStashInventory() : null;
        return inventoryItems != null;
    }
}