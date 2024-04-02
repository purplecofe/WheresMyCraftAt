using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ItemFilterLibrary;
using System.Threading;
using WheresMyCraftAt.ItemFilterLibrary;
using static WheresMyCraftAt.Enums.WheresMyCraftAt;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class FilterHandler
{
    public static bool IsMatchingCondition(ItemFilter filterQuery)
    {
        Logging.Logging.Add("Attempting to match item with filter query.", LogMessageType.Debug);

        if (StashHandler.TryGetStashSpecialSlot(SpecialSlot.CurrencyTab, out var item))
        {
            var isMatch = IsItemMatchingCondition(item.Item, filterQuery);
            Logging.Logging.Add($"Item match found: {isMatch}", LogMessageType.Info);
            return isMatch;
        }

        Logging.Logging.Add("No item found to match condition.", LogMessageType.Error);
        return false;
    }

    public static async SyncTask<(bool result, bool isMatch)> AsyncIsMatchingCondition(ItemFilter filterQuery,
        SpecialSlot slot, CancellationToken token)
    {
        Logging.Logging.Add("Attempting to match item with filter query.", LogMessageType.Debug);
        var asyncResult = await StashHandler.AsyncTryGetStashSpecialSlot(slot, token);

        if (asyncResult.Item1)
        {
            var isMatch = IsItemMatchingCondition(asyncResult.Item2.Item, filterQuery);
            Logging.Logging.Add($"Item match found: {isMatch}", LogMessageType.Info);
            return (asyncResult.Item1, isMatch);
        }

        Logging.Logging.Add("No item found to match condition.", LogMessageType.Error);
        return (asyncResult.Item1, false);
    }

    public static async SyncTask<(bool result, ItemData itemData)> AsyncRetrieveItemData(SpecialSlot slot,
        CancellationToken token)
    {
        Logging.Logging.Add("Attempting to get ItemData.", LogMessageType.ItemData);
        var asyncResult = await StashHandler.AsyncTryGetStashSpecialSlot(slot, token);

        if (asyncResult.Item1)
        {
            Logging.Logging.Add("ItemData found.", LogMessageType.ItemData);
            return (asyncResult.Item1, new ItemData(asyncResult.Item2.Item, Main.GameController));
        }

        Logging.Logging.Add("No item found to match condition.", LogMessageType.Error);
        return (asyncResult.Item1, null);
    }

    public static bool IsItemMatchingCondition(Entity item, ItemFilter filterQuery)
    {
        // Optionally uncomment the following line if you need to print mod list for debugging
        // ItemHandler.PrintHumanModListFromItem(item);
        var itemData = GetItemData(item);
        var result = filterQuery.Matches(itemData);
        Logging.Logging.Add($"Item match result is {result}", LogMessageType.Special);
        return result;
    }

    public static CustomItemData GetItemData(Entity item) => new(item, Main.GameController);
}