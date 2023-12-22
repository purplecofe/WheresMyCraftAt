using ExileCore.PoEMemory.MemoryObjects;
using ItemFilterLibrary;
using System.Threading;
using WheresMyCraftAt.ItemFilterLibrary;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers
{
    public static class FilterHandler
    {
        public static bool IsMatchingCondition(ItemFilter filterQuery)
        {
            if (!StashHandler.TryGetStashSpecialSlot(SpecialSlot.CurrencyTab, out var item))
                return false;

            var itemData = new CustomItemData(item.Item, Main.GameController);
            var result = filterQuery.Matches(itemData);

            Logging.Add($"IsMatchCondition = {result}", LogMessageType.Special);

            return result;
        }   

        public static bool IsItemMatchingCondition(Entity item, ItemFilter filterQuery)
        {
            var itemData = new CustomItemData(item, Main.GameController);
            var result = filterQuery.Matches(itemData);

            Logging.Add($"IsItemMatchCondition = {result}", LogMessageType.Special);

            return result;
        }
    }
}