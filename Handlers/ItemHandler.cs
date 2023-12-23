using System.Collections.Generic;
using System.Threading;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using WheresMyCraftAt.Extensions;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class ItemHandler
{
    public static async SyncTask<bool> AsyncTryApplyOrbToSlot(Enums.WheresMyCraftAt.SpecialSlot slot, string orbName,
        CancellationToken token)
    {
        var asyncResult = await StashHandler.AsyncTryGetStashSpecialSlot(slot, token);

        if (!asyncResult.Item1)
            return false;

        return await asyncResult.Item2.AsyncTryApplyOrb(orbName, token);
    }

    public static async SyncTask<bool> AsyncWaitForItemOnCursor(CancellationToken token, int timeout = 2)
    {
        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            InventoryHandler.IsAnItemPickedUpCondition,
            timeout,
            HelperHandler.GetRandomTimeInRange(Main.Settings.MinMaxRandomDelay),
            token
        );
    }

    public static async SyncTask<bool> AsyncWaitForNoItemOnCursor(CancellationToken token, int timeout = 2)
    {
        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            IsCursorFree,
            timeout,
            HelperHandler.GetRandomTimeInRange(Main.Settings.MinMaxRandomDelay),
            token
        );
    }

    public static async SyncTask<bool> AsyncWaitForRightClickedItemOnCursor(CancellationToken token, int timeout = 2)
    {
        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(
            IsItemRightClickedCondition,
            timeout,
            HelperHandler.GetRandomTimeInRange(Main.Settings.MinMaxRandomDelay),
            token
        );
    }

    public static ItemRarity GetRarityFromItem(Entity item)
    {
        if (item.TryGetComponent<Mods>(out var modsComp))
        {
            Logging.Logging.Add(
                $"GetRarityFromItem: {modsComp.ItemRarity}",
                Enums.WheresMyCraftAt.LogMessageType.Special
            );

            return modsComp.ItemRarity;
        }

        Logging.Logging.Add(
            "GetRarityFromItem: Could not get mods component from item.",
            Enums.WheresMyCraftAt.LogMessageType.Error
        );

        return ItemRarity.Normal;
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

        GetHumanModListFromItem(item)
            .ForEach(itemMod => Logging.Logging.Add($"ItemMod: {itemMod}", Enums.WheresMyCraftAt.LogMessageType.Info));
    }

    public static string GetBaseNameFromItem(Entity item)
    {
        return GetBaseNameFromPath(item?.Path);
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