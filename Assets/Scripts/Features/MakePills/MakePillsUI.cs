// ===== MakePillsUI.cs =====
// 炼丹小游戏 UI：灵能、表达式、进度显示，火焰/材料/结算按钮。
// 所有引用通过 Inspector 手动挂接（场景结构不同，不运行时创建）。
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CultivationSect.MakePills
{
    public class MakePillsUI : MonoBehaviour
    {
        [Header("显示")]
        public TextMeshProUGUI spiritText;       // 顶部灵能
        public TextMeshProUGUI expressionText;   // r(θ) = cos(0θ) + cos(2θ)...
        public TextMeshProUGUI progressText;     // 火焰进度：k 0 → 2
        public TextMeshProUGUI roundText;        // 炉次

        [Header("按钮")]
        public Button flameButton;               // 火焰按钮（FlameController 也已监听）
        public Transform materialContainer;      // 材料按钮父物体（可选，运行时生成）
        public Button materialButtonPrefab;      // 材料按钮模板（可选）

        [Header("结算")]
        public GameObject resultPanel;
        public TextMeshProUGUI resultText;

        public void RefreshSpirit(float v)
        { if (spiritText) spiritText.text = $"灵能: {v:F0}"; }

        public void RefreshExpression(string expr)
        { if (expressionText) expressionText.text = expr; }

        public void RefreshSession(MakePillsSession s)
        { if (roundText && s) roundText.text = $"炉次 {s.round}"; }

        public void RefreshProgress(CauldronModel m)
        {
            if (!progressText) return;
            var t = m?.LatestTerm;
            progressText.text = t == null ? "火焰待命"
                : $"炼化中: k {t.k:0.###} → {t.rangeEnd:0.###}";
        }

        /// <summary>为每种材料生成一个按钮。若 materialContainer / materialButtonPrefab 未在 Inspector 配置，运行时自动创建。</summary>
        public void RebuildMaterialPanel(MaterialDef[] mats, System.Action<MaterialDef> onClick)
        {
            if (mats == null || mats.Length == 0) return;

            if (materialContainer == null || materialButtonPrefab == null) return;

            // 清空旧按钮（保留模板自身，模板是 inactive 的）
            for (int i = materialContainer.childCount - 1; i >= 0; i--)
            {
                var child = materialContainer.GetChild(i);
                if (materialButtonPrefab != null && child == materialButtonPrefab.transform) continue;
                Destroy(child.gameObject);
            }

            foreach (var m in mats)
            {
                if (m == null) continue;
                var btn = Instantiate(materialButtonPrefab, materialContainer);
                btn.gameObject.SetActive(true);
                var lbl = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (lbl) lbl.text = m.materialName;
                var b = btn.GetComponent<Button>();
                if (b) b.onClick.AddListener(() => onClick(m));
            }
        }

        public void ShowResult(string msg)
        {
            if (resultPanel) resultPanel.SetActive(true);
            if (resultText) resultText.text = msg;
        }
    }
}
