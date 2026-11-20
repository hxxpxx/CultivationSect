// ===== MakePillsRules.cs =====
// 炼丹规则配置（Inspector 挂接在 MakePillsRoot 下）：可用材料 + 炼成结算。
using UnityEngine;

namespace CultivationSect.MakePills
{
    public class MakePillsRules : MonoBehaviour
    {
        [Header("可用材料（材料栏）")]
        public MaterialDef[] materials;

        [Header("炼成结算")]
        public string danItemID = "pill_huichun";   // 与 ItemDef.itemID 对应（若有）
        public string danName = "回春丹";
        public int danBaseValue = 120;
        public int creditReward = 50;              // 炼成奖励信用点
    }
}
