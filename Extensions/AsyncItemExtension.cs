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

                Logging.Add($"AsyncTryClick Button is {button}", LogMessageType.Success);

                if (!await MouseHandler.AsyncMoveMouse(clickPosition, token))
                    if (!ElementHandler.IsElementsSameCondition(item, ElementHandler.GetHoveredElementUIAction()))
                        return false;

                if (!await KeyHandler.AsyncButtonPress(button, token))
                    return false;

                var booleanCheck = false;

                if (cursorStateCondition == MouseActionType.UseItem || cursorStateCondition == MouseActionType.HoldItem)
                    booleanCheck = await ItemHandler.AsyncWaitForNoItemOnCursor(token);
                else if (cursorStateCondition == MouseActionType.Free)
                    if (rightClick)
                        booleanCheck = await ItemHandler.AsyncWaitForRightClickedItemOnCursor(token);
                    else
                        booleanCheck = await ItemHandler.AsyncWaitForItemOnCursor(token);

                if (!booleanCheck)
                    return false;

                return true;
            }
            catch (OperationCanceledException)
            {
                // store states later possibly and apply state correction based on the progress?
                return false;
            }
        }

        public static async SyncTask<bool> AsyncTryApplyOrb(this NormalInventoryItem item, Currency currencyType, CancellationToken token)
        {
            if (!TryGetCurrencyName(currencyType, out string currencyName) || string.IsNullOrEmpty(currencyName))
                return false;

            Logging.Add($"AsyncTryApplyOrb CurrencyName is {currencyName}", LogMessageType.Success);

            return await item.AsyncTryApplyOrb(currencyName, token);
        }

        public static async SyncTask<bool> AsyncTryApplyOrb(this NormalInventoryItem item, string currencyName, CancellationToken token)
        {
            try
            {
                var asyncResult = await StashHandler.AsyncTryGetItemInStash(currencyName, token);
                if (!asyncResult.Item1)
                {
                    Main.Stop();
                    return false;
                }

                var orbItem = asyncResult.Item2;

                if (!await orbItem.AsyncTryClick(rightClick: true, token))
                {
                    Main.Stop();
                    return false;
                }

                var elementone = item;

                if (!await item.AsyncTryClick(rightClick: false, token))
                {
                    Main.Stop();
                    return false;
                }

                if (!await ElementHandler.AsyncExecuteNotSameElementWithCancellationHandling(elementone, 3, token))
                {
                    Main.Stop();
                    return false;
                }

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