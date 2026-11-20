// ===== FlameController.cs =====
// 火焰按钮：点击后推进最新一条 cos 的频率并重绘；炼完通知 MakePillsRoot 结算。
using UnityEngine;
using UnityEngine.UI;

namespace CultivationSect.MakePills
{
    public class FlameController : MonoBehaviour
    {
        public CauldronModel model;
        public PolarRenderer renderer;
        public MakePillsRoot root;          // 用于通知结算
        public Button flameButton;
        public MaterialDef currentMat;     // 当前点火焰用的材料（决定 heatPerClick）

        void Awake()
        {
            if (flameButton != null) flameButton.onClick.AddListener(OnFlame);
        }

        public void SetCurrent(MaterialDef m) { currentMat = m; }

        public void OnFlame()
        {
            if (model == null || currentMat == null || !model.IsRefining()) return;
            model.AddHeat(currentMat.heatPerClick);
            renderer?.Redraw();
            root?.NotifyHeat(currentMat);
        }
    }
}
