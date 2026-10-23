// ===== DayConfig.cs (ScriptableObject) =====
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "CultivationSectLeader/DayConfig")]
public class DayConfig : ScriptableObject
{
    public int dayIndex = 1;
    [TextArea] public string announcement;
    public List<ItemCategory> bannedCategories = new List<ItemCategory>();
    public float pawnPayoutRate = 0.6f;      // 折当率
    public float stolenDiscount = 0.3f;      // 被盗物品折价
    public float hazardDiscount = 0.5f;      // 危险品折价
    public float legalPressure = 0.2f;       // 法律压力系数
    public float baseDemandMultiplier = 1.0f;
}