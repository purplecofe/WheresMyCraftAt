using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System.Collections.Generic;
using System.Threading;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class ItemHandler
{
    public static async SyncTask<bool> AsyncTryApplyOrbToSlot(Enums.WheresMyCraftAt.SpecialSlot slot, string orbName,
        CancellationToken token)
    {
        Logging.Logging.Add(
            $"Attempting to apply orb '{orbName}' to slot '{slot}'.",
            Enums.WheresMyCraftAt.LogMessageType.Info
        );

        var asyncResult = await StashHandler.AsyncTryGetStashSpecialSlot(slot, token);

        if (!asyncResult.Item1)
        {
            Logging.Logging.Add(
                $"Failed to get stash slot '{slot}' for orb '{orbName}'.",
                Enums.WheresMyCraftAt.LogMessageType.Error
            );

            return false;
        }

        Logging.Logging.Add(
            $"Stash slot '{slot}' retrieved successfully. Applying orb '{orbName}'.",
            Enums.WheresMyCraftAt.LogMessageType.Info
        );

        return await asyncResult.Item2.AsyncTryApplyOrb(orbName, token);
    }

    public static async SyncTask<bool> AsyncWaitForItemOnCursor(CancellationToken token, int timeout = 2)
    {
        Logging.Logging.Add("Waiting for an item to be on the cursor.", Enums.WheresMyCraftAt.LogMessageType.Info);

        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            InventoryHandler.IsAnItemPickedUpCondition,
            timeout,
            HelperHandler.GetRandomTimeInRange(Main.Settings.MinMaxRandomDelay),
            token
        );
    }

    public static async SyncTask<bool> AsyncWaitForNoItemOnCursor(CancellationToken token, int timeout = 2)
    {
        Logging.Logging.Add("Waiting for no item to be on the cursor.", Enums.WheresMyCraftAt.LogMessageType.Info);

        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            IsCursorFree,
            timeout,
            HelperHandler.GetRandomTimeInRange(Main.Settings.MinMaxRandomDelay),
            token
        );
    }

    public static async SyncTask<bool> AsyncWaitForRightClickedItemOnCursor(CancellationToken token, int timeout = 2)
    {
        Logging.Logging.Add(
            "Waiting for a right-clicked item to be on the cursor.",
            Enums.WheresMyCraftAt.LogMessageType.Info
        );

        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            IsItemRightClickedCondition,
            timeout,
            HelperHandler.GetRandomTimeInRange(Main.Settings.MinMaxRandomDelay),
            token
        );
    }

    public static List<string> GetHumanModListFromItem(Entity item)
    {
        return item.TryGetComponent<Mods>(out var modsComp) && modsComp.HumanStats.Count != 0
            ? modsComp.HumanStats
            : [];
    }

    public static void PrintHumanModListFromItem(Entity item)
    {
        Logging.Logging.Add($"Items Mods for: {item.Path}", Enums.WheresMyCraftAt.LogMessageType.Info);
        var modsList = GetHumanModListFromItem(item);

        if (modsList.Count != 0)
            foreach (var itemMod in modsList)
                Logging.Logging.Add($"ItemMod: {itemMod}", Enums.WheresMyCraftAt.LogMessageType.Info);
        else
            Logging.Logging.Add("No mods found on the item.", Enums.WheresMyCraftAt.LogMessageType.Info);
    }

    public static string GetBaseNameFromItem(NormalInventoryItem item)
    {
        return GetBaseNameFromPath(item.Entity?.Path);
    }

    public static string GetBaseNameFromPath(string path)
    {
        return Main.GameController?.Files.BaseItemTypes.Translate(path)?.BaseName ?? string.Empty;
    }

    public static bool IsItemOnLeftClickCondition()
    {
        return ElementHandler.TryGetCursorStateCondition(out var cursorState) &&
               cursorState == MouseActionType.HoldItem;
    }

    public static bool IsItemRightClickedCondition()
    {
        return ElementHandler.TryGetCursorStateCondition(out var cursorState) && cursorState == MouseActionType.UseItem;
    }

    public static bool IsCursorFree()
    {
        return ElementHandler.TryGetCursorStateCondition(out var cursorState) && cursorState == MouseActionType.Free;
    }
}