using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.UI;
using Terraria.ModLoader;
using Terraria.GameContent.UI.Elements;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.Localization;

namespace bulksell
{
    public class BulkSellUIState : UIState
    {
        private UIPanel bulkSellButton;
        private UIText buttonText;
        private UIPanel historyPanel;
        private UIList itemList;
        private UIScrollbar scrollbar;
        public static bool ForceRefreshNextUpdate = false;

        private Vector2 _dragOffset;
        private bool _dragging = false;
        private bool _resizing = false;
        private bool _showHistory = false;
        private int _lastHistoryCount = -1;
        private int _pressTimer = 0;
        private Vector2 _startPos;

        private UIText _historyTitle;

        public override void OnInitialize()
        {
            Width.Set(0f, 1f); Height.Set(0f, 1f);

            var config = ModContent.GetInstance<BulkSellConfig>();
            if (config != null){
                _ = config;
            }

            bulkSellButton = new UIPanel();
            bulkSellButton.SetPadding(4);
            bulkSellButton.OverflowHidden = true;

            float w = config?.ButtonWidth ?? 180f;
            float h = config?.ButtonHeight ?? 40f;
            if (w < 200f) w = 200f;
            if (h < 40f) h = 40f;
            bulkSellButton.Left.Set(config?.ButtonLeft ?? 700f, 0f);
            bulkSellButton.Top.Set(config?.ButtonTop ?? 400f,0f);
            bulkSellButton.Width.Set(w, 0f);
            bulkSellButton.Height.Set(h, 0f);
            
            bulkSellButton.OnLeftMouseDown += (evt, element) => {
                _startPos = evt.MousePosition;
                _pressTimer = 0;
                var dims = element.GetDimensions();
                if (evt.MousePosition.X > dims.X + dims.Width - 30 && evt.MousePosition.Y > dims.Y + dims.Height - 30)
                    _resizing = true;
                else {
                    _dragOffset = evt.MousePosition - dims.Position();
                    _dragging = true;
                }
            };

            bulkSellButton.OnLeftMouseUp += (evt, element) => {
                if (!_resizing && _pressTimer < 12 && Vector2.Distance(_startPos, evt.MousePosition) < 4f) {
                    SellLogic.ExecuteBulkSell(Main.LocalPlayer);
                }
                _dragging = false; _resizing = false;
                SaveToConfig();
            };

            bulkSellButton.OnRightClick += (evt, element) => {
                _showHistory = !_showHistory;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
            };

            buttonText = new UIText(Language.GetTextValue("Mods.bulksell.UI.ButtonHint"), 0.82f);
            buttonText.HAlign = 0.5f; buttonText.VAlign = 0.5f;
            buttonText.Width.Set(-10f, 1f);
            buttonText.OverflowHidden = true;
            buttonText.TextOriginX = 0.5f;
            buttonText.TextOriginY = 0.5f;
            bulkSellButton.Append(buttonText);
            Append(bulkSellButton);
            bulkSellButton.Recalculate();

            historyPanel = new UIPanel();
            historyPanel.SetPadding(10);
            historyPanel.Width.Set(260f, 0f);
            historyPanel.Height.Set(300f, 0f);
            historyPanel.BackgroundColor = new Color(33, 43, 79) * 0.95f;

            _historyTitle = new UIText(Language.GetTextValue("Mods.bulksell.UI.UndoAll"), 0.8f);
            _historyTitle.TextColor = Color.Yellow; _historyTitle.HAlign = 0.5f;
            _historyTitle.OnLeftClick += (evt, element) => {
                SellLogic.UndoAll(Main.LocalPlayer);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen);
            };
            _historyTitle.OnMouseOver += (evt, element) => _historyTitle.TextColor = Color.Orange;
            _historyTitle.OnMouseOut += (evt, element) => _historyTitle.TextColor = Color.Yellow;
            historyPanel.Append(_historyTitle);

            itemList = new UIList();
            itemList.Top.Set(35f, 0f);
            itemList.Width.Set(-25f, 1f);
            itemList.Height.Set(-40f, 1f);
            itemList.ManualSortMethod = (items) => { }; 
            historyPanel.Append(itemList);

            scrollbar = new UIScrollbar();
            scrollbar.SetView(100f, 1000f);
            scrollbar.Height.Set(-45f, 1f);
            scrollbar.Top.Set(40f, 0f);
            scrollbar.Left.Set(-15f, 1f);
            itemList.SetScrollbar(scrollbar);
            historyPanel.Append(scrollbar);

        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Main.mouseLeft) _pressTimer++;

            var config = ModContent.GetInstance<BulkSellConfig>();
            if (bulkSellButton.IsMouseHovering || historyPanel.IsMouseHovering || _dragging || _resizing)
                Main.LocalPlayer.mouseInterface = true;

            if (_dragging) {
                float maxLeft = Main.screenWidth - bulkSellButton.Width.Pixels;
                float maxTop  = Main.screenHeight - bulkSellButton.Height.Pixels;
                float newLeft = MathHelper.Clamp(Main.mouseX - _dragOffset.X, 0f, maxLeft);
                float newTop  = MathHelper.Clamp(Main.mouseY - _dragOffset.Y, 0f, maxTop);
                bulkSellButton.Left.Set(newLeft, 0f);
                bulkSellButton.Top.Set(newTop, 0f);
            } else if (_resizing) {
                var dims = bulkSellButton.GetDimensions();
                float maxWidth  = Main.screenWidth - dims.X;
                float maxHeight = Main.screenHeight - dims.Y;
                
                // 结合原有上下限（130~500, 45~300）和屏幕边界
                float newWidth  = MathHelper.Clamp(Main.mouseX - dims.X, 200f, Math.Min(500f, maxWidth));
                float newHeight = MathHelper.Clamp(Main.mouseY - dims.Y, 45f,  Math.Min(300f, maxHeight));
                
                bulkSellButton.Width.Set(newWidth, 0f);
                bulkSellButton.Height.Set(newHeight, 0f);
            } else {
                if (config.ButtonWidth > 10f && config.ButtonHeight > 10f) { // 防止配置文件被手动修改为过小的值
                    float w = config.ButtonWidth < 200f ? 200f : config.ButtonWidth;
                    float h = config.ButtonHeight < 40f ? 40f : config.ButtonHeight;
                    bulkSellButton.Left.Set(config.ButtonLeft, 0f);
                    bulkSellButton.Top.Set(config.ButtonTop, 0f);
                    bulkSellButton.Width.Set(w, 0f);
                    bulkSellButton.Height.Set(h, 0f);
                }
            }

            bulkSellButton.Recalculate();

            if (_showHistory) {
                if (historyPanel.Parent == null) Append(historyPanel);
                historyPanel.Left.Set(bulkSellButton.Left.Pixels + bulkSellButton.Width.Pixels + 10, 0f);
                historyPanel.Top.Set(bulkSellButton.Top.Pixels, 0f);
                
                if (_lastHistoryCount != SellLogic.SellHistory.Count || ForceRefreshNextUpdate) {
                    RefreshHistoryList();
                    _lastHistoryCount = SellLogic.SellHistory.Count;
                    ForceRefreshNextUpdate = false;
                }
            } else {
                historyPanel.Remove();
            }

            if (bulkSellButton.IsMouseHovering) bulkSellButton.BackgroundColor = new Color(100, 118, 184);
            else bulkSellButton.BackgroundColor = new Color(73, 94, 171) * 0.9f;

            if (buttonText.Text != Language.GetTextValue("Mods.bulksell.UI.ButtonHint"))
                buttonText.SetText(Language.GetTextValue("Mods.bulksell.UI.ButtonHint"));
            
            if (_historyTitle != null && _historyTitle.Text != Language.GetTextValue("Mods.bulksell.UI.UndoAll"))
                _historyTitle.SetText(Language.GetTextValue("Mods.bulksell.UI.UndoAll"));
        }

        private void RefreshHistoryList()
        {
            var config = ModContent.GetInstance<BulkSellConfig>();
            SellLogic.TrimHistory(config.MaxHistory);
            
            itemList.Clear();
            if (SellLogic.SellHistory.Count == 0) {
                itemList.Add(new UIText(Language.GetTextValue("Mods.bulksell.UI.NoHistory"), 0.75f) { TextColor = Color.Gray });
                return;
            }

            for (int i = SellLogic.SellHistory.Count - 1; i >= 0; i--)
            {
                int logicIndex = i;
                var entry = SellLogic.SellHistory[logicIndex];
                int displayID = logicIndex + 1;

                string name = $"{displayID}. {entry.SoldItem.Name}";
                
                if (entry.SoldItem.stack > 1)
                    name += $" x{entry.SoldItem.stack}";

                UIText itemLine = new UIText(name, 0.75f);
                itemLine.Width.Set(0f, 1f);
                itemLine.Height.Set(24f, 0f);
                itemLine.TextOriginX = 0f;

                itemLine.OnLeftClick += (evt, element) => {
                    SellLogic.UndoSpecificSale(Main.LocalPlayer, logicIndex);
                };

                itemLine.OnMouseOver += (evt, element) => itemLine.TextColor = Color.LightGreen;
                itemLine.OnMouseOut += (evt, element) => itemLine.TextColor = Color.White;

                itemList.Add(itemLine);
            }
            itemList.Recalculate();
        }
        private void SaveToConfig() {
            var config = ModContent.GetInstance<BulkSellConfig>();
            if (config != null) {
                config.ButtonLeft = bulkSellButton.Left.Pixels;
                config.ButtonTop = bulkSellButton.Top.Pixels;
                config.ButtonWidth = bulkSellButton.Width.Pixels;
                config.ButtonHeight = bulkSellButton.Height.Pixels;
                // 注意：这里不需要调用 config.Save()，tModLoader 会在退出游戏或离开世界时自动将内存写入硬盘 json
            }
        }
    }
}