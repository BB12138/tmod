using System.Collections.Generic;
using System.ComponentModel;
using Terraria.ModLoader.Config;
using Newtonsoft.Json;

namespace bulksell
{
    public class BulkSellConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // --- 第一部分：使用说明 ---
        [Header("$Mods.bulksell.Configs.BulkSellConfig.Headers.Usage")]
        [JsonIgnore]
        [Label("$Mods.bulksell.Configs.BulkSellConfig.UsageGuide.Label")]
        [Tooltip("$Mods.bulksell.Configs.BulkSellConfig.UsageGuide.Tooltip")]
        public string UsageGuide => ""; 

        // --- 第二部分：功能设置 ---
        [Header("$Mods.bulksell.Configs.BulkSellConfig.Headers.Functional")]
        
        [DefaultValue(200)]
        [Range(10, 500)]
        [Increment(10)]
        [Label("$Mods.bulksell.Configs.BulkSellConfig.MaxHistory.Label")]
        [Tooltip("$Mods.bulksell.Configs.BulkSellConfig.MaxHistory.Tooltip")]
        public int MaxHistory;

        // 卖出价格倍率：1.0 = 原版售价（物品价值的 1/5），0 = 免费清空背包
        [DefaultValue(1f)]
        [Range(0f, 5f)]
        [Increment(0.1f)]
        [Label("$Mods.bulksell.Configs.BulkSellConfig.SellPriceMultiplier.Label")]
        [Tooltip("$Mods.bulksell.Configs.BulkSellConfig.SellPriceMultiplier.Tooltip")]
        public float SellPriceMultiplier = 1f;

        [Label("$Mods.bulksell.Configs.BulkSellConfig.BlackList.Label")]
        [Tooltip("$Mods.bulksell.Configs.BulkSellConfig.BlackList.Tooltip")]
        public List<ItemDefinition> BlackList = new List<ItemDefinition>();

        // --- 第三部分：界面布局 ---
        [Header("$Mods.bulksell.Configs.BulkSellConfig.Headers.Layout")]
        
        [DefaultValue(610f)]
        [Range(0f, 3000f)]
        [Increment(1f)]
        [Label("$Mods.bulksell.Configs.BulkSellConfig.ButtonLeft.Label")]
        public float ButtonLeft = 610f;

        [DefaultValue(15f)]
        [Range(0f, 2000f)]
        [Increment(1f)]
        [Label("$Mods.bulksell.Configs.BulkSellConfig.ButtonTop.Label")]
        public float ButtonTop = 15f;

        [DefaultValue(110f)]
        [Range(80f, 200f)] // 下限 80（按钮只有 4 个汉字可缩小）、上限与拖拽上限一致
        [Increment(1f)]
        [Label("$Mods.bulksell.Configs.BulkSellConfig.ButtonWidth.Label")]
        public float ButtonWidth = 110f;

        [DefaultValue(40f)]
        [Range(28f, 100f)]  // 下限 28、上限与拖拽上限一致
        [Increment(1f)]
        [Label("$Mods.bulksell.Configs.BulkSellConfig.ButtonHeight.Label")]
        public float ButtonHeight = 40f;

        // --- 第四部分：重置选项 ---
        private bool _resetLayout;
        [DefaultValue(false)]
        [Label("$Mods.bulksell.Configs.BulkSellConfig.ResetLayout.Label")]
        [Tooltip("$Mods.bulksell.Configs.BulkSellConfig.ResetLayout.Tooltip")]

        public bool ResetLayout
        {
            get => _resetLayout;
            set
            {
                if (value)
                {
                    ButtonLeft = 610f;
                    ButtonTop = 15f;
                    ButtonWidth = 110f;
                    ButtonHeight = 40f;
                }
                _resetLayout = false;
            }
        }
    }
}

