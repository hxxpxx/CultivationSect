// ===== CosTerm.cs =====
// 炼丹炉的极坐标项：a·cos(k·θ)（或 a·sin(k·θ)）。
// 数学上，炉内表达式 r(θ) = Σ 各项，即 cos(θ)+cos(2θ)+... 的累加（一条曲线）。
// k 为当前频率，可被火焰按钮从 rangeStart 逐格推进到 rangeEnd（材料递进）。
using System;
using UnityEngine;

namespace CultivationSect.MakePills
{
    [Serializable]
    public class CosTerm
    {
        public float amplitude = 1f;    // 振幅 a
        public bool useSin = false;     // cos / sin
        public float rangeStart = 0f;   // 放入炉子时的频率起点（材料A=0）
        public float rangeEnd = 2f;     // 火焰推进目标（材料A=2, 材料B=4）

        [NonSerialized] public float k;        // 当前频率（运行时）
        [NonSerialized] public bool done;      // 是否已炼到 rangeEnd

        public void Init()
        {
            k = rangeStart;
            done = Mathf.Approximately(k, rangeEnd);
        }

        /// <summary>点火焰推进一格；返回是否推进了（未推进 = 已炼完）。</summary>
        public bool AddHeat(float heatPerClick)
        {
            if (done) return false;
            float next = Mathf.MoveTowards(k, rangeEnd, heatPerClick);
            bool moved = !Mathf.Approximately(k, next);
            k = next;
            if (Mathf.Approximately(k, rangeEnd)) done = true;
            return moved;
        }

        public float Evaluate(float theta)
            => amplitude * (useSin ? Mathf.Sin(k * theta) : Mathf.Cos(k * theta));

        /// <summary>当前项表达式文本（用于 UI 显示）。</summary>
        public string ToExprText()
            => $"{amplitude:0.###}{(useSin ? "·sin(" : "·cos(")}{k:0.###}θ)";
    }
}
