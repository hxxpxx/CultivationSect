// ===== MakePillsRoot.cs =====
// 炼丹小游戏宿主：编排各子系统，对接跨场景共享状态（灵能/资金/库存）。
// 挂到场景2的根物体；子系统通过 Inspector 挂接，未挂接时自动 GetComponentInChildren 兜底。
using UnityEngine;

namespace CultivationSect.MakePills
{
    public class MakePillsRoot : MonoBehaviour
    {
        [Header("炉子数据")]
        public CauldronModel model = new CauldronModel();   // 极坐标表达式（多条 cos 累加）

        [Header("子系统（Inspector 挂接，可自动查找）")]
        public MakePillsRules rules;
        public PolarRenderer renderer;
        public FlameController flame;
        public MakePillsSession session;
        public MakePillsReward reward;
        public MakePillsUI ui;

        public GameStateRuntime State => GameStateRuntime.Instance;
        public MakePillsRules Rules => rules;

        void Awake()
        {

            // 注入依赖（null 安全，避免某子系统未挂时抛异常中断整个注入）
            if (renderer != null) renderer.model = model;
            else Debug.LogWarning("[MakePillsRoot] 缺少 PolarRenderer：CurveLine 需挂 PolarRenderer，并置于本物体子层级或拖入 renderer 槽。");

            if (flame != null) { flame.model = model; flame.renderer = renderer; flame.root = this; }
            if (reward != null) reward.Root = this;
        }

        void Start()
        {
            session.Begin();
            ui.RefreshSpirit(State.spiritEnergy);
            ui.RefreshExpression(model.GetExpressionText());
            ui.RefreshSession(session);
            renderer.Redraw();

            // 生成材料栏按钮：需要 ui.materialContainer + ui.materialButtonPrefab + rules.materials 都配好
            ui.RebuildMaterialPanel(rules.materials, OnMaterialAdded);
        }

        /// <summary>玩家放入一种材料（材料栏按钮调用）。</summary>
        public void OnMaterialAdded(MaterialDef m)
        {
            if (m == null) return;
            // 扣灵能（跨场景共享状态）
            State.spiritEnergy = Mathf.Max(0f, State.spiritEnergy - m.costSpirit);

            model.AddMaterial(m);
            flame?.SetCurrent(m);
            renderer?.Redraw();
            ui?.RefreshSpirit(State.spiritEnergy);
            ui?.RefreshExpression(model.GetExpressionText());
            ui?.RefreshProgress(model);
            GameEvents.SafeInvoke(GameEvents.OnToastRequested, $"放入「{m.materialName}」", "info", 1.5f);
        }

        /// <summary>火焰推进了一格（FlameController 回调）；最新项炼完则结算。</summary>
        public void NotifyHeat(MaterialDef m)
        {
            renderer?.Redraw();
            ui?.RefreshExpression(model.GetExpressionText());
            ui?.RefreshProgress(model);
            if (model.LatestTerm != null && model.LatestTerm.done)
                reward?.Settle(session, model);
        }

        /// <summary>结算完成回调（MakePillsReward 调用）。</summary>
        public void OnRefined()
        {
            ui?.ShowResult($"炼成「{rules.danName}」！资金+{rules.creditReward}");
            ui?.RefreshSpirit(State.spiritEnergy);
        }
    }
}
