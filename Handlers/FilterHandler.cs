using ExileCore.PoEMemory.MemoryObjects;
using ItemFilterLibrary;
using WheresMyCraftAt.ItemFilterLibrary;
using static WheresMyCraftAt.Enums.WheresMyCraftAt;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class FilterHandler
{
    public static bool IsMatchingCondition(ItemFilter filterQuery)
    {
        Logging.Logging.Add("IsMatchingCondition: Attempting to match item with filter query.", LogMessageType.Debug);

        if (StashHandler.TryGetStashSpecialSlot(SpecialSlot.CurrencyTab, out var item))
        {
            var isMatch = IsItemMatchingCondition(item.Item, filterQuery);
            Logging.Logging.Add($"IsMatchingCondition: Item match found: {isMatch}", LogMessageType.Info);
            return isMatch;
        }

        Logging.Logging.Add("IsMatchingCondition: No item found to match condition.", LogMessageType.Error);
        return false;
    }

    public static bool IsItemMatchingCondition(Entity item, ItemFilter filterQuery)
    {
        // Optionally uncomment the following line if you need to print mod list for debugging
        // ItemHandler.PrintHumanModListFromItem(item);
        var itemData = new CustomItemData(item, Main.GameController);
        var result = filterQuery.Matches(itemData);
        Logging.Logging.Add($"IsItemMatchingCondition: Item match result is {result}", LogMessageType.Special);
        return result;
    }
}