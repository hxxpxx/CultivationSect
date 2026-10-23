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
    public int baseValue;                        // 基础估值
    public ItemRarity itemRarity;                    // 品级（白/绿/蓝/紫/金/红）
    [TextArea] public string description;
}