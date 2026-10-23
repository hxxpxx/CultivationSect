// ===== ItemDef.cs (ScriptableObject) =====
using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(menuName = "CultivationSectLeader/ItemDef")]
public class ItemDef : ScriptableObject
{
    public string itemID;
    public string itemName;
    public Sprite itemIcon;
    public ItemCategory itemCategory;            // 灵草/矿石/丹药/武器/
    public int baseValue;                // 基础估值
    //public float volatility;                   // 价格波动系数
    //public Rarity rarity;
    //public float legalRiskWeight = 0.1f;       // 0~1，法律风险系数
    [TextArea] public string description;
}