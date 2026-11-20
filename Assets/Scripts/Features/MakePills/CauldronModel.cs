// ===== CauldronModel.cs =====
// 炼丹炉数据模型：维护极坐标表达式 r(θ) = Σ aᵢ·cos(kᵢθ)。
// 放材料 = 往表达式追加一项 cos；点火焰 = 推进最新项的频率。
// 纯 C# 数据类（[Serializable]，非 MonoBehaviour），由 MakePillsRoot 持有。
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CultivationSect.MakePills
{
    [Serializable]
    public class CauldronModel
    {
        public List<CosTerm> terms = new List<CosTerm>();

        /// <summary>放一种材料：往表达式追加一个 cos 项。</summary>
        public void AddMaterial(MaterialDef m)
        {
            if (m == null) return;
            var t = new CosTerm
            {
                amplitude = m.amplitude,
                useSin = m.useSin,
                rangeStart = m.rangeStart,
                rangeEnd = m.rangeEnd,
            };
            t.Init();
            terms.Add(t);
        }

        /// <summary>点火焰：只推进最新一条 cos 的频率（逐条递进，符合"材料A→2、B→4"）。</summary>
        public bool AddHeat(float heatPerClick)
        {
            if (terms.Count == 0) return false;
            return terms[terms.Count - 1].AddHeat(heatPerClick);
        }

        public bool HasAnyTerm => terms.Count > 0;
        public CosTerm LatestTerm => terms.Count > 0 ? terms[terms.Count - 1] : null;

        /// <summary>最新一条是否仍在炼化。</summary>
        public bool IsRefining() => LatestTerm != null && !LatestTerm.done;

        public float Evaluate(float theta)
        {
            float r = 0f;
            foreach (var t in terms) r += t.Evaluate(theta);
            return r;
        }

        /// <summary>当前表达式文本：r(θ) = cos(0θ) + cos(2θ) ...（用于 UI 显示）</summary>
        public string GetExpressionText()
        {
            if (terms.Count == 0) return "r(θ) = 0";
            var sb = new StringBuilder("r(θ) = ");
            for (int i = 0; i < terms.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                sb.Append(terms[i].ToExprText());
            }
            return sb.ToString();
        }
    }
}
