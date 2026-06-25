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
            bulkSellUI = null;
            bulkSellUserInterface = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
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
                            bulkSellUserInterface?.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}