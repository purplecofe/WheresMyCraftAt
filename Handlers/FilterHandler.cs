using ExileCore.PoEMemory.MemoryObjects;
using ItemFilterLibrary;
using WheresMyCraftAt.ItemFilterLibrary;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers
{
    public static class FilterHandler
    {
        public static bool IsMatchingCondition(ItemFilter filterQuery)
        {
            if (!StashHandler.TryGetStashSpecialSlot(SpecialSlot.CurrencyTab, out var item))
            {
                Logging.Add($"IsMatchingCondition found no item", LogMessageType.Error);
                return false;
            }

            return IsItemMatchingCondition(item.Item, filterQuery);
        }

        public static bool IsItemMatchingCondition(Entity item, ItemFilter filterQuery)
        {
            ItemHandler.PrintHumanModListFromItem(item);

            var itemData = new CustomItemData(item, Main.GameController);
            var result = filterQuery.Matches(itemData);

            Logging.Add($"IsItemMatchingCondition = {result}", LogMessageType.Special);

            return result;
        }
    }
}