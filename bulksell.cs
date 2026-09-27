using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria.Localization;
using Microsoft.Xna.Framework.Input;

namespace bulksell
{
    public struct SoldItemEntry
    {
        public Item SoldItem;
        public long PriceReceived;
    }

    public static class SellLogic
    {
        public static List<SoldItemEntry> SellHistory = new List<SoldItemEntry>();

        private const int DefaultMaxHistory = 200;

        /// <summary>基准售价倍率：1.0 等于原版售价（物品价值的 1/5）。</summary>
        private const float DefaultPriceMultiplier = 1f;

        public static void TrimHistory(int maxHistory)
        {
            while (SellHistory.Count > maxHistory)
                SellHistory.RemoveAt(0);
        }

        /// <summary>清空售卖历史（离开世界时调用，避免跨世界"买回"）。</summary>
        public static void ClearHistory()
        {
            SellHistory.Clear();
        }

        public static void ExecuteBulkSell(Player player)
        {
            if (Main.keyState.IsKeyDown(Keys.LeftAlt)
                || Main.keyState.IsKeyDown(Keys.RightAlt)
                || Main.keyState.IsKeyDown(Keys.LeftShift))
                return;

            var config = ModContent.GetInstance<BulkSellConfig>();
            int maxHistory = config?.MaxHistory ?? DefaultMaxHistory;
            TrimHistory(maxHistory);

            // 卖出价格倍率：1.0 = 原版售价（物品价值的 1/5），0 = 免费清空背包
            float priceMultiplier = config?.SellPriceMultiplier ?? DefaultPriceMultiplier;
            int pricePercent = (int)Math.Round(priceMultiplier * 100.0, MidpointRounding.AwayFromZero);

            bool soldSomething = false;

            // 从后往前遍历，避免卖出后堆叠变化影响未处理的槽
            for (int i = 49; i >= 0; i--)
            {
                Item item = player.inventory[i];
                if (item == null || item.IsAir || item.favorited)
                    continue;

                // 钱币不参与批量出售，避免把钱币按售价低价卖掉
                if (item.IsACoin)
                    continue;

                // 有价值、或已被列入"强制售卖名单"的物品才会被卖掉
                if (!(item.value > 0 || IsInForceSellList(item, config)))
                    continue;

                // 记录快照（Clone 出的对象是独立的，后面的 TurnToAir 不会影响它）
                Item soldItemSnapshot = item.Clone();

                // 先转 long 再乘，避免高价值/模组物品溢出
                long unitPrice = (long)item.value * pricePercent / 500;
                long finalPrice = unitPrice * item.stack;

                item.TurnToAir(); // 清空槽位

                SellHistory.Add(new SoldItemEntry
                {
                    SoldItem = soldItemSnapshot,
                    PriceReceived = finalPrice
                });

                GiveMoney(player, finalPrice);
                soldSomething = true;
            }

            if (soldSomething)
            {
                BulkSellUIState.ForceRefreshNextUpdate = true;
                SoundEngine.PlaySound(SoundID.Coins);
            }
        }

        public static bool UndoSpecificSale(Player player, int index, bool quiet = false)
        {
            if (index < 0 || index >= SellHistory.Count) return false;
            var entry = SellHistory[index];

            bool hasSpace = false;
            for (int i = 0; i < 50; i++) {
                if (player.inventory[i] == null || player.inventory[i].IsAir) {
                    hasSpace = true;
                    break;
                }
            }

            if (!hasSpace) {
                if (!quiet) Main.NewText(Language.GetTextValue("Mods.bulksell.Messages.BagFullError"), Color.Yellow);
                return false;
            }

            if (!player.BuyItem(entry.PriceReceived))
            {
                if (!quiet) {
                    Main.NewText(Language.GetTextValue("Mods.bulksell.Messages.SingleMoneyError"), Color.Red);
                }
                return false;
            }

            int requestedStack = Math.Max(1, entry.SoldItem.stack);

            // 传 Clone 进 GetItem，避免它修改历史记录里的快照
            Item itemToReturn = entry.SoldItem.Clone();
            Item leftover = player.GetItem(player.whoAmI, itemToReturn, GetItemSettings.LootAllSettings);

            if (leftover != null && leftover.stack > 0)
            {
                int placedStack = requestedStack - leftover.stack;

                if (placedStack > 0)
                {
                    // 只放下了一部分：按"未取回的比例"退款，并把历史记录缩减成还没取回的那部分。
                    // 这样不会出现"部分取回却全额退款"的刷物品漏洞。
                    long refund = entry.PriceReceived * leftover.stack / requestedStack;

                    Item remainSnapshot = entry.SoldItem.Clone();
                    remainSnapshot.stack = leftover.stack;
                    SellHistory[index] = new SoldItemEntry
                    {
                        SoldItem = remainSnapshot,
                        PriceReceived = refund
                    };

                    GiveMoney(player, refund);
                }
                else
                {
                    // 完全没放下：全额退款
                    GiveMoney(player, entry.PriceReceived);
                }

                if (!quiet) Main.NewText(Language.GetTextValue("Mods.bulksell.Messages.ItemRetrieveError"), Color.Orange);
                return false;
            }

            SellHistory.RemoveAt(index);
            SoundEngine.PlaySound(SoundID.Grab);
            return true;
        }

        public static void UndoAll(Player player)
        {
            if (SellHistory.Count == 0) return;

            int itemsRetrieved = 0;
            bool spaceFull = false;

            // 从最新记录开始往前取回：与历史面板的显示顺序一致。
            // 取回成功时会移除下标 i 的记录，只会影响比 i 更大的下标（已经处理过了），所以倒序遍历是安全的。
            for (int i = SellHistory.Count - 1; i >= 0; i--)
            {
                bool currentHasSpace = false;
                for (int slot = 0; slot < 50; slot++) {
                    if (player.inventory[slot] == null || player.inventory[slot].IsAir) {
                        currentHasSpace = true;
                        break;
                    }
                }

                if (!currentHasSpace) {
                    Main.NewText(Language.GetTextValue("Mods.bulksell.Messages.InventoryFull"), Color.Yellow);
                    spaceFull = true;
                    break;
                }

                if (UndoSpecificSale(player, i, true)) {
                    itemsRetrieved++;
                }
            }

            if (itemsRetrieved > 0) {
                Main.NewText(Language.GetTextValue("Mods.bulksell.Messages.UndoSuccess", itemsRetrieved), Color.Green);
            } else if (!spaceFull) {
                Main.NewText(Language.GetTextValue("Mods.bulksell.Messages.MoneyInsufficient"), Color.Red);
            }
        }

        /// <summary>强制售卖名单（界面里的名字叫"强制售卖名单"，即使物品没有价值也会被卖掉）。</summary>
        private static bool IsInForceSellList(Item item, BulkSellConfig config) {
            if (config?.BlackList == null) return false;
            foreach(var d in config.BlackList) if(d.Type == item.type) return true;
            return false;
        }

        private static readonly int[] CoinTypeOrder = { ItemID.CopperCoin, ItemID.SilverCoin, ItemID.GoldCoin, ItemID.PlatinumCoin };

        private static void GiveMoney(Player player, long amount)
        {
            if (amount <= 0)
                return;

            int[] coins = Utils.CoinsSplit(amount);

            for (int i = 0; i < 4; i++)
            {
                int remaining = coins[i];
                if (remaining <= 0)
                    continue;

                Item coinPrototype = new Item();
                coinPrototype.SetDefaults(CoinTypeOrder[i]);
                int maxStack = coinPrototype.maxStack <= 0 ? remaining : coinPrototype.maxStack;

                // 优先按 maxStack 分堆放进背包；只有真放不下才掉在地上，避免钱丢失
                while (remaining > 0)
                {
                    int stack = Math.Min(remaining, maxStack);

                    Item coin = coinPrototype.Clone();
                    coin.stack = stack;

                    Item leftover = player.GetItem(player.whoAmI, coin, GetItemSettings.PickupItemFromWorld);
                    if (leftover != null && leftover.stack > 0)
                    {
                        player.QuickSpawnItem(player.GetSource_Misc("BulkSell"), leftover.type, leftover.stack);
                        remaining = 0; // 剩下的已经掉在地上了，结束这一种钱币
                    }
                    else
                    {
                        remaining -= stack;
                    }
                }
            }
        }
    }
}