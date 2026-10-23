// ===== PricingEngine.cs =====
using UnityEngine;

public class PricingEngine
{
    public float EstimateOffer(ItemInstance item, DayConfig day)
    {
        if (item == null || item == null) return 0f;

        float market = item.baseValue;
        //float cond = Mathf.Clamp01(item.condition);
        float rarityMul = GetRarityMultiplier(item.rarity);
        float risk = GetRiskDiscount(item, day);

        float baseOffer = market * rarityMul * day.baseDemandMultiplier * risk;
        return baseOffer * day.pawnPayoutRate;
    }

    float GetRarityMultiplier(ItemRarity r)
    {
        switch (r)
        {
            case ItemRarity.Common: return 1f;
            case ItemRarity.Uncommon: return 1.5f;
            case ItemRarity.Rare: return 2.5f;
            case ItemRarity.Epic: return 5f;
            case ItemRarity.Legendary: return 10f;
            default: return 1f;
        }
    }

    float GetRiskDiscount(ItemInstance item, DayConfig day)
    {
        if (item.isBanned) return 0f;
        if (item.isStolen && day.stolenDiscount > 0) return day.stolenDiscount;
        if (item.isHazardous) return day.hazardDiscount;
        return 1f;
    }

    // 估值修正（AI物品的特殊定价）
    public float EstimateAIItemOffer(ItemInstance item, DayConfig day)
    {
        if (item.aiParasite == null || !item.aiParasite.isPresent)
            return EstimateOffer(item, day);

        float baseOffer = EstimateOffer(item, day);

        // AI物品价值取决于多个因素
        float aiValueMultiplier = 1.0f;

        // 智力越高，黑市价值越高
        aiValueMultiplier += item.aiParasite.intelligence * 0.1f;

        // 但风险也越高
        float riskPenalty = (item.aiParasite.aggression * 0.05f) + (item.aiParasite.corruptionLevel * 0.3f);

        // 如果已发现且已控制，风险降低
        if (item.aiParasite.discoveredByPlayer && item.aiParasite.contained)
        {
            riskPenalty *= 0.2f;
            aiValueMultiplier *= 0.8f; // 控制后黑市价值降低
        }

        float finalOffer = baseOffer * aiValueMultiplier * (1f - riskPenalty);
        return Mathf.Max(0, finalOffer);
    }
}