// ===== PolarRenderer.cs =====
// 极坐标曲线渲染：对炉内表达式求值 r(θ)，采样画 LineRenderer。
// 挂在场景中的 LineRenderer 物体（CurveLine）上。
using UnityEngine;

namespace CultivationSect.MakePills
{
    public class PolarRenderer : MonoBehaviour
    {
        public CauldronModel model;
        public LineRenderer line;
        public int samples = 512;

        void Reset()
        {
            line = GetComponent<LineRenderer>();
        }

        void Awake()
        {
            // 运行时兜底：未在 Inspector 手拖 line 时，自动取本物体上的 LineRenderer
            if (line == null) line = GetComponent<LineRenderer>();
        }

        public void Redraw()
        {
            if (line == null) line = GetComponent<LineRenderer>();
            if (line == null)
            {
                Debug.LogWarning("[PolarRenderer] 缺少 LineRenderer：请给 CurveLine 物体 Add Component → Line Renderer。");
                return;
            }
            if (model == null)
            {
                Debug.LogWarning("[PolarRenderer] model 未注入：把挂有 PolarRenderer 的物体放到 MakePillsRoot 子层级，或把 PolarRenderer 拖进 MakePillsRoot.renderer。");
                return;
            }

            // 始终用本地坐标，避免 LineRenderer 默认世界坐标导致曲线跑到原点/偏移
            line.useWorldSpace = false;

            if (!model.HasAnyTerm)
            {
                // 炉内没有材料：清空曲线（未放材料时本来就无图案）
                line.positionCount = 0;
                return;
            }

            int n = Mathf.Max(3, samples);
            var pts = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float th = (float)i / n * 2f * Mathf.PI;
                float r = model.Evaluate(th);
                pts[i] = new Vector3(r * Mathf.Cos(th), r * Mathf.Sin(th), 0f);
            }
            line.loop = true;
            line.positionCount = n;
            line.SetPositions(pts);
        }
    }
}
