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

        public static void TrimHistory(int maxHistory)
        {
            while (SellHistory.Count > maxHistory)
                SellHistory.RemoveAt(0);
        }

        public static void ExecuteBulkSell(Player player)
        {
            if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.LeftShift))
                return;

            var config = ModContent.GetInstance<BulkSellConfig>();
            int maxHistory = config?.MaxHistory ?? DefaultMaxHistory;
            TrimHistory(maxHistory);

            bool soldSomething = false;

            // 从后往前遍历，避免卖出后堆叠变化影响未处理的槽
            for (int i = 49; i >= 0; i--)
            {
                Item item = player.inventory[i];
                if (item == null || item.IsAir || item.favorited)
                    continue;

                // 跳过鼠标抓取的物品（额外安全判断）
                if (Main.mouseItem == item)
                    continue;

                // 黑名单或价值判定
                if (!(item.value > 0 || IsInBlackList(item, config)))
                    continue;

                // 记录快照
                Item soldItemSnapshot = item.Clone();
                long finalPrice = (long)(item.value / 5) * item.stack;
                item.SetDefaults(0); // 清空槽位
                SellHistory.Add(new SoldItemEntry
                {
                    SoldItem = soldItemSnapshot,
                    PriceReceived = finalPrice
                });

                // 先给钱，再清空物品（顺序可互换，但确保钱到账）
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

            if (player.BuyItem(entry.PriceReceived))
            {
                Item result = player.GetItem(player.whoAmI, entry.SoldItem, GetItemSettings.LootAllSettings);
                
                if (result.stack <= 0) {
                    SellHistory.RemoveAt(index);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                    return true;
                } else {
                    GiveMoney(player, entry.PriceReceived);
                    if (!quiet) Main.NewText(Language.GetTextValue("Mods.bulksell.Messages.ItemRetrieveError"), Color.Orange);
                    return false;
                }
            }
            else
            {
                if (!quiet) {
                    Main.NewText(Language.GetTextValue("Mods.bulksell.Messages.SingleMoneyError"), Color.Red);
                }
                return false;
            }
        }

        public static void UndoAll(Player player)
        {
            if (SellHistory.Count == 0) return;

            int itemsRetrieved = 0;
            bool spaceFull = false;

            for (int i = 0; i < SellHistory.Count; i++)
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
                    i--; 
                }
            }

            if (itemsRetrieved > 0) {
                Main.NewText(Language.GetTextValue("Mods.bulksell.Messages.UndoSuccess", itemsRetrieved), Color.Green);
            } else if (!spaceFull) {
                // 已修复：去掉了原本多余的一层 Language.GetTextValue 嵌套
                Main.NewText(Language.GetTextValue("Mods.bulksell.Messages.MoneyInsufficient"), Color.Red);
            }
        }

        private static bool IsInBlackList(Item item, BulkSellConfig config) {
            if (config.BlackList == null) return false;
            foreach(var d in config.BlackList) if(d.Type == item.type) return true;
            return false;
        }
        private static void GiveMoney(Player player, long amount)
        {
            int[] coins = Utils.CoinsSplit(amount);
            for (int i = 0; i < 4; i++)
                if (coins[i] > 0) player.QuickSpawnItem(player.GetSource_Misc("BulkSell"), 
                    i == 3 ? ItemID.PlatinumCoin : i == 2 ? ItemID.GoldCoin : i == 1 ? ItemID.SilverCoin : ItemID.CopperCoin, coins[i]);
        }
    }
}