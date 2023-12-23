using ExileCore.PoEMemory.MemoryObjects;
using ItemFilterLibrary;
using WheresMyCraftAt.ItemFilterLibrary;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class FilterHandler
{
    public static bool IsMatchingCondition(ItemFilter filterQuery)
    {
        if (StashHandler.TryGetStashSpecialSlot(Enums.WheresMyCraftAt.SpecialSlot.CurrencyTab, out var item))
            return IsItemMatchingCondition(item.Item, filterQuery);

        Logging.Logging.Add("IsMatchingCondition found no item", Enums.WheresMyCraftAt.LogMessageType.Error);
        return false;

    }

    public static bool IsItemMatchingCondition(Entity item, ItemFilter filterQuery)
    {
        //ItemHandler.PrintHumanModListFromItem(item);
        var itemData = new CustomItemData(item, Main.GameController);
        var result = filterQuery.Matches(itemData);
        Logging.Logging.Add($"IsItemMatchingCondition = {result}", Enums.WheresMyCraftAt.LogMessageType.Special);
        return result;
    }
}