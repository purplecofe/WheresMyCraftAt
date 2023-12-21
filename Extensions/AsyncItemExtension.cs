using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using System;
using System.Threading;
using System.Windows.Forms;
using WheresMyCraftAt.Handlers;
using static WheresMyCraftAt.Extensions.CurrencyExtensions;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Extensions
{
    public static class ItemExtensions
    {
        public static async SyncTask<bool> AsyncTryClick(this NormalInventoryItem item, bool rightClick, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                ElementHandler.TryGetCursorStateCondition(out var cursorStateCondition);

                var clickPosition = item.GetClientRectCache.Center.ToVector2Num();
                var button = !rightClick ? Keys.LButton : Keys.RButton;

                Main.DebugPrint($"AsyncTryClick Button is {button}", LogMessageType.Success);

                if (!await MouseHandler.AsyncMoveMouse(clickPosition, token)
                    || !ElementHandler.IsElementsSameCondition(item, ElementHandler.GetHoveredElementUIAction()))
                    return false;

                token.ThrowIfCancellationRequested();

                if (!await KeyHandler.AsyncButtonPress(button, token))
                    return false;

                token.ThrowIfCancellationRequested();

                var itemOnCursor = false;

                if (cursorStateCondition == MouseActionType.UseItem || cursorStateCondition == MouseActionType.HoldItem)
                {
                    if (rightClick)
                        itemOnCursor = await ItemHandler.AsyncWaitForItemOffCursor(token);
                    else
                        itemOnCursor = await ItemHandler.AsyncWaitForRightClickedItemOffCursor(token);
                }
                else if (cursorStateCondition == MouseActionType.Free)
                {
                    if (rightClick)
                        itemOnCursor = await ItemHandler.AsyncWaitForRightClickedItemOnCursor(token);
                    else
                        itemOnCursor = await ItemHandler.AsyncWaitForItemOnCursor(token);
                }

                if (!itemOnCursor)
                    return false;

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public static async SyncTask<bool> AsyncTryApplyOrb(this NormalInventoryItem item, Currency currencyType, CancellationToken token)
        {
            if (!TryGetCurrencyName(currencyType, out string currencyName) || string.IsNullOrEmpty(currencyName))
                return false;

            Main.DebugPrint($"AsyncTryApplyOrb CurrencyName is {currencyName}", LogMessageType.Success);

            return await item.AsyncTryApplyOrb(currencyName, token);
        }

        public static async SyncTask<bool> AsyncTryApplyOrb(this NormalInventoryItem item, string currencyName, CancellationToken token)
        {
            try
            {
                if (!StashHandler.TryGetItemInStash(currencyName, out var orbItem))
                    return false;

                Main.DebugPrint($"AsyncTryApplyOrb OrbItem is {ItemHandler.GetBaseNameFromItem(orbItem)}", LogMessageType.Success);

                if (!await orbItem.AsyncTryClick(true, token))
                    return false;

                Main.DebugPrint($"AsyncTryApplyOrb OrbItem Clicked", LogMessageType.Success);

                var elementone = item;

                if (!await item.AsyncTryClick(false, token))
                    return false;

                if (!await ElementHandler.AsyncExecuteNotSameElementWithCancellationHandling(elementone, 3, token))
                    return false;

                return true;
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation logic or state correction here if necessary
                return false;
            }
        }

        public static async SyncTask<bool> AsyncChangeItemRarity(this NormalInventoryItem item, ItemRarity targetRarity, CancellationToken token) =>
            await AsyncChangeItemRarity(item, targetRarity, false, token);

        public static async SyncTask<bool> AsyncChangeItemRarity(this NormalInventoryItem item, ItemRarity targetRarity, bool preferScouring, CancellationToken token)
        {
            try
            {
                var itemRarity = ItemHandler.GetRarityFromItem(item.Entity);

                if (itemRarity == ItemRarity.Unique)
                    return false;

                switch (itemRarity)
                {
                    case ItemRarity.Normal:
                        if (targetRarity == ItemRarity.Normal)
                        {
                            // cannot possibly turn a normal into a normal without massive recursion setting in.
                            return false;
                        }
                        else if (targetRarity == ItemRarity.Magic)
                        {
                            if (!await item.AsyncTryApplyOrb(Currency.OrbOfTransmutation, token))
                                return false;
                            else if (targetRarity == ItemRarity.Rare)
                            {
                                if (!await item.AsyncTryApplyOrb(Currency.OrbOfAlchemy, token))
                                    return false;
                            }
                        }
                        else if (targetRarity == ItemRarity.Rare)
                        {
                            if (!await item.AsyncTryApplyOrb(Currency.OrbOfAlchemy, token))
                                return false;
                        }

                        break;

                    case ItemRarity.Magic:
                        if (targetRarity == ItemRarity.Rare)
                        {
                            if (preferScouring)
                            {
                                if (!await item.AsyncTryApplyOrb(Currency.OrbOfScouring, token))
                                    return false;

                                if (!StashHandler.TryGetStashSpecialSlot(SpecialSlot.CurrencyTab, out var specialItem))
                                    return false;

                                if (!await specialItem.AsyncChangeItemRarity(ItemRarity.Rare, token))
                                    return false;
                            }
                            else
                            {
                                if (!await item.AsyncTryApplyOrb(Currency.RegalOrb, token))
                                    return false;
                            }
                        }
                        else if (targetRarity == ItemRarity.Normal &&
                            !await item.AsyncTryApplyOrb(Currency.OrbOfScouring, token))
                            return false;
                        else if (targetRarity == ItemRarity.Magic &&
                            !await item.AsyncTryApplyOrb(Currency.OrbOfAlteration, token))
                            return false;
                        break;

                    case ItemRarity.Rare:
                        if (targetRarity == ItemRarity.Normal &&
                            !await item.AsyncTryApplyOrb(Currency.OrbOfScouring, token))
                            return false;
                        else if (targetRarity == ItemRarity.Magic)
                        {
                            if (!await item.AsyncTryApplyOrb(Currency.OrbOfScouring, token))
                                return false;

                            if (!StashHandler.TryGetStashSpecialSlot(SpecialSlot.CurrencyTab, out var specialItem))
                                return false;

                            if (!await specialItem.AsyncChangeItemRarity(ItemRarity.Magic, token))
                                return false;
                        }
                        else if (targetRarity == ItemRarity.Rare)
                        {
                            if (!await item.AsyncTryApplyOrb(Currency.ChaosOrb, token))
                                return false;
                        }
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            return true;
        }
    }
}