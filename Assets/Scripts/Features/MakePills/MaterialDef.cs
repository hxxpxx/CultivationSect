// ===== MaterialDef.cs =====
// 材料资产（ScriptableObject）：放进炼丹炉 = 往表达式追加一个 a·cos(kθ)。
// k 可在 [rangeStart, rangeEnd] 间被火焰按钮逐格推进（材料A: 0→2，材料B: 2→4）。
using UnityEngine;

namespace CultivationSect.MakePills
{
    [CreateAssetMenu(fileName = "Material", menuName = "MakePills/Material")]
    public class MaterialDef : ScriptableObject
    {
        public string materialName;      // 火精 / 风灵
        public float amplitude = 1f;     // 振幅 a
        public bool useSin = false;      // cos / sin
        public float rangeStart = 0f;    // 频率下限（材料A=0）
        public float rangeEnd = 2f;      // 频率上限（材料A=2, 材料B=4）
        public float heatPerClick = 0.1f;// 每次点火焰推进的频率量
        public int costSpirit = 5;       // 放入时消耗灵能
        public Sprite icon;              // 材料图标（可选）
    }
}
