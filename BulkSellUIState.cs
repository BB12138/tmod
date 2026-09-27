using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.UI;
using Terraria.ModLoader;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI.Chat;
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

        /// <summary>按钮最小尺寸：现在只显示 4 个汉字，可以缩得比较小。</summary>
        private const float MinButtonWidth = 80f;
        private const float MinButtonHeight = 28f;

        private Vector2 _dragOffset;
        private bool _dragging = false;
        private bool _resizing = false;
        private bool _showHistory = false;
        private int _lastHistoryCount = -1;
        private int _pressTimer = 0;
        private bool _pressedOnButton = false;
        private Vector2 _startPos;

        private UIText _historyTitle;

        public override void OnInitialize()
        {
            Width.Set(0f, 1f); Height.Set(0f, 1f);

            var config = ModContent.GetInstance<BulkSellConfig>();

            bulkSellButton = new UIPanel();
            bulkSellButton.SetPadding(4);
            bulkSellButton.OverflowHidden = true;

            float w = config?.ButtonWidth ?? 80f;
            float h = config?.ButtonHeight ?? 28f;
            if (w < MinButtonWidth) w = MinButtonWidth;
            if (h < MinButtonHeight) h = MinButtonHeight;
            bulkSellButton.Left.Set(config?.ButtonLeft ?? 610f, 0f);
            bulkSellButton.Top.Set(config?.ButtonTop ?? 15f,0f);
            bulkSellButton.Width.Set(w, 0f);
            bulkSellButton.Height.Set(h, 0f);
            
            bulkSellButton.OnLeftMouseDown += (evt, element) => {
                _startPos = evt.MousePosition;
                _pressTimer = 0;
                _pressedOnButton = true;
                var dims = element.GetDimensions();
                // 缩放区随按钮尺寸自适应：否则按钮缩小时整块都变成缩放区，就再也拖不动了
                float grip = Math.Min(30f, Math.Max(9f, Math.Min(dims.Width, dims.Height) - 12f));
                if (evt.MousePosition.X > dims.X + dims.Width - grip && evt.MousePosition.Y > dims.Y + dims.Height - grip)
                    _resizing = true;
                else {
                    _dragOffset = evt.MousePosition - dims.Position();
                    _dragging = true;
                }
            };

            bulkSellButton.OnLeftMouseUp += (evt, element) => {
                bool layoutChanged = _dragging || _resizing;

                // 只有在按钮上按下、短促、且几乎没移动的点击才会触发出售
                if (_pressedOnButton && !_resizing && _pressTimer < 12 && Vector2.Distance(_startPos, evt.MousePosition) < 4f) {
                    SellLogic.ExecuteBulkSell(Main.LocalPlayer);
                }
                _dragging = false; _resizing = false; _pressedOnButton = false;

                if (layoutChanged) SaveToConfig();
            };

            bulkSellButton.OnRightClick += (evt, element) => {
                _showHistory = !_showHistory;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
            };

            buttonText = new UIText(Language.GetTextValue("Mods.bulksell.UI.ButtonHint"), ButtonTextBaseScale);
            // 横向：占满内宽 + TextOriginX 居中（这部分已验证正确，保持不动）
            buttonText.Width.Set(0f, 1f);
            buttonText.TextOriginX = 0.5f;
            // 竖向：元素高度在 UpdateButtonText() 里设成"UIText 的单行高度(16×字号)"，交给 UIElement 的 VAlign 线性公式居中。
            // 不能用 TextOriginY + 满高元素：UIText 的 origin 偏移不随盒子变高而变大，
            // 结果就是按钮越大、文字越往上飘，最后贴顶。
            buttonText.TextOriginY = 0f;
            buttonText.VAlign = 0.5f;
            // 盒子现在紧贴文字，别再裁掉自己的阴影（面板自身的 OverflowHidden 仍然生效）
            buttonText.OverflowHidden = false;
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
            // 保持加入顺序（不做自动排序），因为点击记录时要用列表下标去索引 SellHistory
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

            // 只在"按下发生在按钮上"时计时，避免在别处按住、移到按钮上松开时误触发
            if (Main.mouseLeft && _pressedOnButton) _pressTimer++;

            var config = ModContent.GetInstance<BulkSellConfig>();
            if (bulkSellButton.IsMouseHovering || historyPanel.IsMouseHovering || _dragging || _resizing)
                Main.LocalPlayer.mouseInterface = true;

            // 兜底：如果在按钮外松开（UI 事件不会触发），这里负责复位，避免按钮"黏住"鼠标
            if (!Main.mouseLeft && (_dragging || _resizing))
            {
                _dragging = false;
                _resizing = false;
                _pressedOnButton = false;
                SaveToConfig();
            }

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
                
                // 结合原有上下限和屏幕边界（用 Math.Max 兜底，避免夹紧区间上下限颠倒抛异常）
                float maxW = Math.Max(MinButtonWidth, Math.Min(200f, maxWidth));
                float maxH = Math.Max(MinButtonHeight, Math.Min(100f, maxHeight));

                float newWidth  = MathHelper.Clamp(Main.mouseX - dims.X, MinButtonWidth, maxW);
                float newHeight = MathHelper.Clamp(Main.mouseY - dims.Y, MinButtonHeight, maxH);
                
                bulkSellButton.Width.Set(newWidth, 0f);
                bulkSellButton.Height.Set(newHeight, 0f);
            } else {
                if (config.ButtonWidth > 10f && config.ButtonHeight > 10f) { // 防止配置文件被手动修改为过小的值
                    float w = config.ButtonWidth < MinButtonWidth ? MinButtonWidth : config.ButtonWidth;
                    float h = config.ButtonHeight < MinButtonHeight ? MinButtonHeight : config.ButtonHeight;
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

            // 鼠标悬停时显示操作说明（原版悬停文字，会带原生底框显示在光标旁）
            if (bulkSellButton.IsMouseHovering && !_dragging && !_resizing)
                Main.instance.MouseText(Language.GetTextValue("Mods.bulksell.UI.ButtonTooltip"));

            UpdateButtonText();
            
            if (_historyTitle != null && _historyTitle.Text != Language.GetTextValue("Mods.bulksell.UI.UndoAll"))
                _historyTitle.SetText(Language.GetTextValue("Mods.bulksell.UI.UndoAll"));
        }

        /// <summary>按钮标题字号上限（按钮被拖大时文字不会无限变大）。</summary>
        private const float ButtonTextBaseScale = 0.9f;

        private string _lastAppliedButtonText = null;
        private float _lastAppliedButtonTextScale = -1f;
        private string _measuredTextFor = null;
        private Vector2 _measuredTextSize = Vector2.Zero;

        /// <summary>
        /// 更新按钮标题：用实测文本尺寸算出"能完整放进按钮"的字号，元素高度用 UIText 的单行高度(16×字号)、由 VAlign 垂直居中。
        /// </summary>
        private void UpdateButtonText()
        {
            string text = Language.GetTextValue("Mods.bulksell.UI.ButtonHint");

            // 文本实测尺寸只跟文字内容有关，缓存起来避免每帧重复量
            if (_measuredTextFor != text)
            {
                _measuredTextFor = text;
                // 与 UIText 内部量文本的方式保持一致（能正确处理换行与颜色标签）
                _measuredTextSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, text, Vector2.One);
            }
            Vector2 textSize = _measuredTextSize;

            float scale = ButtonTextBaseScale;

            var inner = bulkSellButton.GetInnerDimensions();
            float availWidth = inner.Width - 6f;
            float availHeight = inner.Height - 4f;
            if (availWidth > 0f && availHeight > 0f && textSize.X > 0f && textSize.Y > 0f)
            {
                scale = Math.Min(scale, availWidth / textSize.X);
                scale = Math.Min(scale, availHeight / textSize.Y);
                scale = MathHelper.Clamp(scale, 0.5f, ButtonTextBaseScale);
            }

            // 只在文本或字号真的变化时才重建，避免每帧无谓开销
            if (_lastAppliedButtonText != text || Math.Abs(scale - _lastAppliedButtonTextScale) > 0.01f)
            {
                buttonText.SetText(text, scale, false);
                // 高度 = UIText 自己的单行高度（源码里就是 large ? 32f : 16f，也是它算 MinHeight 用的值）。
                // 不要用 ChatManager.GetStringSize().Y：中文那里返回的是"字形包围盒高"、比行高大，
                // 拿它当盒子高度会让盒子偏高、文字贴上沿，按钮越小越明显。
                buttonText.Height.Set(16f * scale, 0f);

                _lastAppliedButtonText = text;
                _lastAppliedButtonTextScale = scale;

                // 立刻按新高度重新布局，避免这一帧还按旧尺寸显示
                bulkSellButton.Recalculate();
            }
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
                // 这里不需要显式落盘：tModLoader 会在退出游戏/离开世界时把内存里的客户端配置写入 json。
                // （本版本没有公开的 ModConfig.Save / ConfigManager.Save 可用，改成 SaveAll 又会连带写其它配置，故保持原行为。）
            }
        }
    }
}