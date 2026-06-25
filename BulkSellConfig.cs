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

        [Label("$Mods.bulksell.Configs.BulkSellConfig.BlackList.Label")]
        [Tooltip("$Mods.bulksell.Configs.BulkSellConfig.BlackList.Tooltip")]
        public List<ItemDefinition> BlackList = new List<ItemDefinition>();

        // --- 第三部分：界面布局 ---
        [Header("$Mods.bulksell.Configs.BulkSellConfig.Headers.Layout")]
        
        [DefaultValue(700f)]
        [Range(0f, 3000f)]
        [Increment(1f)]
        [Label("$Mods.bulksell.Configs.BulkSellConfig.ButtonLeft.Label")]
        public float ButtonLeft = 700f;

        [DefaultValue(400f)]
        [Range(0f, 2000f)]
        [Increment(1f)]
        [Label("$Mods.bulksell.Configs.BulkSellConfig.ButtonTop.Label")]
        public float ButtonTop = 400f;

        [DefaultValue(180f)]
        [Range(40f, 1000f)] // 最小宽度40，防止滑条拉到最左变像素点
        [Increment(1f)]
        [Label("$Mods.bulksell.Configs.BulkSellConfig.ButtonWidth.Label")]
        public float ButtonWidth = 180f;

        [DefaultValue(40f)]
        [Range(20f, 500f)]  // 最小高度20
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
                    ButtonLeft = 700f;
                    ButtonTop = 400f;
                    ButtonWidth = 180f;
                    ButtonHeight = 40f;
                }
                _resetLayout = false;
            }
        }
    }
}

