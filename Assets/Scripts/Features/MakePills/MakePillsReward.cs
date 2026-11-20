// ===== MakePillsReward.cs =====
// 结算层：炼成后炼丹入库、发资金奖励。唯一读写共享状态的子系统。
// 依赖现有跨场景单例：GameStateRuntime（灵能/资金）、InventoryManager（同一份库存）。
using UnityEngine;

namespace CultivationSect.MakePills
{
    public class MakePillsReward : MonoBehaviour
    {
        public MakePillsRoot Root;

        /// <summary>炼成结算：炼丹入库 + 发资金。</summary>
        public void Settle(MakePillsSession session, CauldronModel model)
        {
            var state = Root.State;
            var rules = Root.Rules;
            var inv = InventoryManager.Instance.GetOrCreate("counter", "柜台货架", 24);

            // 炼丹入库（沿用场景1的同一份柜台库存）
            var pill = new ItemInstance
            {
                itemID = rules.danItemID,
                itemName = rules.danName,
                baseValue = rules.danBaseValue,
                owner = ItemInstance.ItemOwnership.Merchant,
                placement = ItemInstance.ItemPlacement.Shelf,
                isAuthentic = true,
            };
            if (inv.Add(pill))
                GameEvents.SafeInvoke(GameEvents.OnToastRequested, $"炼成「{rules.danName}」已入库！", "success", 2f);
            else
                GameEvents.SafeInvoke(GameEvents.OnToastRequested, "货架已满，丹无法入库！", "warning", 2f);

            // 发资金奖励
            state.shopFunds += rules.creditReward;
            GameEvents.SafeInvoke(GameEvents.OnFundsChanged, state.shopFunds, rules.creditReward, "炼丹奖励");

            session?.EndRefined();
            Root?.OnRefined();
        }
    }
}
