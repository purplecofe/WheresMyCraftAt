using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class StashHandler
{
    public static async SyncTask<bool> AsyncWaitForStashOpen(CancellationToken token, int timeout = 2)
    {
        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(IsStashPanelOpenCondition, timeout, token);
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
        return ElementHandler.IsInGameUiElementOpenCondition(ui => ui.StashElement);
    }

    public static bool IsVisibleStashValidCondition()
    {
        return GetTypeOfCurrentVisibleStash() != InventoryType.InvalidInventory;
    }

    public static bool TryGetItemInStash(string baseName, out NormalInventoryItem foundItem)
    {
        foundItem = null;
        Logging.Logging.Add($"Trying to find item '{baseName}' in stash.", Enums.WheresMyCraftAt.LogMessageType.Info);

        if (TryGetVisibleStashInventory(out var stashContents))
        {
            Logging.Logging.Add($"Items in stash: {stashContents.Count}", Enums.WheresMyCraftAt.LogMessageType.Info);
            foundItem = stashContents.FirstOrDefault(item => ItemHandler.GetBaseNameFromItem(item) == baseName);
        }

        if (foundItem == null)
            Logging.Logging.Add($"Could not find '{baseName}' in stash.", Enums.WheresMyCraftAt.LogMessageType.Error);
        else
            Logging.Logging.Add(
                $"Found '{baseName}' [W:{foundItem.Width}, H:{foundItem.Height}] in stash.",
                Enums.WheresMyCraftAt.LogMessageType.Info
            );

        return foundItem != null;
    }

    public static async SyncTask<Tuple<bool, NormalInventoryItem>> AsyncTryGetItemInStash(string currencyName,
        CancellationToken token)
    {
        NormalInventoryItem orbItem = null;

        Logging.Logging.Add(
            $"Attempting to find item '{currencyName}' in stash.",
            Enums.WheresMyCraftAt.LogMessageType.Info
        );

        var result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            () => TryGetItemInStash(currencyName, out orbItem),
            2,
            Main.ServerLatency,
            token
        );

        Logging.Logging.Add(
            $"Item '{currencyName}' found status: {result}.",
            Enums.WheresMyCraftAt.LogMessageType.Info
        );

        return Tuple.Create(result, orbItem);
    }

    public static async SyncTask<Tuple<bool, NormalInventoryItem>> AsyncTryGetStashSpecialSlot(
        Enums.WheresMyCraftAt.SpecialSlot slotType, CancellationToken token)
    {
        NormalInventoryItem inventoryItem = null;

        Logging.Logging.Add(
            $"Attempting to find special slot '{slotType}' in stash.",
            Enums.WheresMyCraftAt.LogMessageType.Info
        );

        var result = await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            () => TryGetStashSpecialSlot(slotType, out inventoryItem),
            2,
            Main.ServerLatency,
            token
        );

        Logging.Logging.Add(
            $"Special slot '{slotType}' found status: {result}.",
            Enums.WheresMyCraftAt.LogMessageType.Info
        );

        return Tuple.Create(result, inventoryItem);
    }

    public static bool TryGetStashSpecialSlot(Enums.WheresMyCraftAt.SpecialSlot slotType,
        out NormalInventoryItem inventoryItem)
    {
        inventoryItem = TryGetVisibleStashInventory(out var stashContents)
            ? stashContents.FirstOrDefault(item => item.Elem.Size == Main.SpecialSlotDimensionMap[slotType])
            : null;

        if (inventoryItem == null)
            Logging.Logging.Add(
                $"Special slot '{slotType}' not found.",
                Enums.WheresMyCraftAt.LogMessageType.Warning
            );
        else
            Logging.Logging.Add(
                $"Found special slot '{slotType}'.",
                Enums.WheresMyCraftAt.LogMessageType.Info
            );

        return inventoryItem != null;
    }

    public static bool TryGetVisibleStashInventory(out IList<NormalInventoryItem> inventoryItems)
    {
        inventoryItems = IsVisibleStashValidCondition()
            ? GetVisibleStashInventory()
            : null;

        if (inventoryItems == null)
            Logging.Logging.Add(
                "Visible stash inventory not found.",
                Enums.WheresMyCraftAt.LogMessageType.Warning
            );
        else
            Logging.Logging.Add(
                "Visible stash inventory retrieved.",
                Enums.WheresMyCraftAt.LogMessageType.Info
            );

        return inventoryItems != null;
    }
}