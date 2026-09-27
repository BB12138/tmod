using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace bulksell
{
    public class BulkSellSystem : ModSystem
    {
        internal UserInterface bulkSellUserInterface;
        internal BulkSellUIState bulkSellUI;

        // 保存最近一次的 GameTime 供 Draw 使用（不能传 new GameTime()，否则时间增量恒为 0）
        private GameTime _lastGameTime = new GameTime();

        public override void Load()
        {
            if (!Main.dedServ)
            {
                bulkSellUI = new BulkSellUIState();
                bulkSellUI.Activate();
                bulkSellUserInterface = new UserInterface();
                bulkSellUserInterface.SetState(bulkSellUI);
            }
        }

        public override void Unload()
        {
            SellLogic.ClearHistory();
            BulkSellUIState.ForceRefreshNextUpdate = false;
            bulkSellUI = null;
            bulkSellUserInterface = null;
        }

        public override void OnWorldUnload()
        {
            // 离开/切换世界时清空售卖历史，避免在新世界里"买回"上个世界的物品
            SellLogic.ClearHistory();
            BulkSellUIState.ForceRefreshNextUpdate = false;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            _lastGameTime = gameTime;
            if (Main.npcShop > 0 && Main.playerInventory)
                bulkSellUserInterface?.Update(gameTime);
        }
        
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryIndex != -1)
            {
                layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
                    "BulkSellMod: Bulk Sell UI",
                    delegate
                    {
                        if (Main.npcShop > 0 && Main.playerInventory && bulkSellUserInterface?.CurrentState != null)
                        {
                            bulkSellUserInterface?.Draw(Main.spriteBatch, _lastGameTime);
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}