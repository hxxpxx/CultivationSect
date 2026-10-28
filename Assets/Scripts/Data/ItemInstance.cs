// ===== ItemInstance.cs =====
// Assets/Scripts/Data/ItemInstance.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 物品实例（运行时数据）
/// 与 ItemDef（ScriptableObject 静态定义）分离
/// 代表当铺柜台上某一件具体的、有状态的物品
/// </summary>
[Serializable]
public class ItemInstance
{
    // ============ 身份 ============
    public string instanceID;           // 运行时唯一ID（Guid截取）
    public string itemID;               // 对应 ItemDef.itemID
    public string itemName;             // 缓存名称（避免每次查数据库）
    public ItemCategory category;       // 缓存分类
    public int baseValue;               // 缓存基础价值
    public ItemRarity rarity;           // 品级
    public string description;          // 描述

    // ============ 实例状态 ============
    public bool isAuthentic = true;          // 是否真品（false = 赝品）
    public bool isStolen = false;            // 是否赃货（触发报警）
    public bool isHazardous = false;         // 是否有辐射/危险（能量危害）
    public bool isQuarantined = false;       // 是否被隔离（AI寄生物品）
    public bool isBanned = false;             // 是否被禁止流通

    // ============ 归属关系 ============
    public enum ItemOwnership { None, Customer, Merchant }
    public ItemOwnership owner = ItemOwnership.None;   // 客户带来=Customer；被商栈收购=Merchant

    // ============ 放置区域（窗口 / 柜台） ============
    /// <summary>
    /// 客户携带物品的放置区域：
    ///   窗口(Window) = 顾客刚到时物品的落位，尚未估价；
    ///   柜台(Counter) = 玩家拖到柜台后，自动估价（appraisedOffer 生效）。
    /// 归属非 Customer 的物品（货架库存）不参与本字段。
    /// </summary>
    public enum ItemPlacement { Window, Counter }
    public ItemPlacement placement = ItemPlacement.Window;

    /// <summary>放上柜台后自动估价的结果（信用点）；未估价或放回窗口时为 0。</summary>
    [NonSerialized]
    public float appraisedOffer;

    // ============ AI 寄生数据 ============
    // 如果为 null 表示无 AI 寄生
    public AIParasiteData aiParasite;

    // ============ 扩展属性（运行时动态数据） ============
    // 注意：Dictionary 无法被 Unity JsonUtility 直接序列化
    // 存档时需要额外处理，这里标记 NonSerialized 仅作运行时使用
    [NonSerialized]
    public Dictionary<string, object> customProperties = new Dictionary<string, object>();

    // ============ 构造函数 ============

    public ItemInstance()
    {
        instanceID = Guid.NewGuid().ToString().Substring(0, 8);
        customProperties = new Dictionary<string, object>();
    }

    // ============ 核心方法 ============

    /// <summary>
    /// 计算实际价值（受成色、真伪影响）
    /// </summary>
    public int GetActualValue()
    {
        if (!isAuthentic)
            return 0; // 赝品分文不值

        return Mathf.RoundToInt(baseValue);
    }

    /// <summary>
    /// 计算当铺收当报价（可加入声望/黑市系数）
    /// </summary>
    public int GetPawnOffer(float reputationMultiplier = 1f, float moodMultiplier = 1f)
    {
        int actual = GetActualValue();
        if (actual <= 0) return 0;

        // 当铺通常按实际价值的 40%-70% 收当
        float baseRate = 0.55f;
        float finalRate = baseRate * reputationMultiplier * moodMultiplier;

        // 赃货打折
        if (isStolen)
            finalRate *= 0.6f;

        // 危险物打折
        if (isHazardous)
            finalRate *= 0.7f;

        return Mathf.RoundToInt(actual * Mathf.Clamp01(finalRate));
    }

    /// <summary>
    /// 计算黑市售价（比收当价高，比原价低）
    /// </summary>
    public int GetBlackMarketValue()
    {
        int actual = GetActualValue();
        if (actual <= 0) return 0;

        float rate = 0.75f; // 黑市按75%走
        if (isStolen) rate = 0.9f; // 赃货黑市反而值钱

        return Mathf.RoundToInt(actual * rate);
    }

    /// <summary>
    /// 获取物品的风险等级（用于报警判定）
    /// </summary>
    public float GetRiskLevel()
    {
        float risk = 0f;
        if (isStolen) risk += 0.5f;
        if (isHazardous) risk += 0.3f;
        if (aiParasite != null) risk += aiParasite.corruptionLevel * 0.01f;
        if (!isAuthentic) risk += 0.1f;
        return Mathf.Clamp01(risk);
    }

    /// <summary>
    /// 克隆实例（深拷贝，用于需要复制物品的场合）
    /// </summary>
    public ItemInstance Clone()
    {
        var clone = new ItemInstance
        {
            itemID = this.itemID,
            itemName = this.itemName,
            category = this.category,
            baseValue = this.baseValue,
            isAuthentic = this.isAuthentic,
            isStolen = this.isStolen,
            isHazardous = this.isHazardous,
            isQuarantined = this.isQuarantined,
            owner = this.owner,
        };

        // 复制自定义属性（浅拷贝 value）
        foreach (var kvp in this.customProperties)
            clone.customProperties[kvp.Key] = kvp.Value;

        return clone;
    }

    /// <summary>
    /// 获取简要描述（用于 UI Tooltip）
    /// </summary>
    public string GetSummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<b>{itemName}</b>");
        sb.AppendLine($"类别: {category}");
        sb.AppendLine($"价值: {GetActualValue()} (原价 {baseValue})");
        sb.AppendLine($"真品: {(isAuthentic ? "是" : "否(赝品!)")}");
        if (isStolen) sb.AppendLine("<color=red>⚠ 赃货</color>");
        if (isHazardous) sb.AppendLine("<color=orange>☢ 危险</color>");
        if (aiParasite != null) sb.AppendLine($"<color=cyan>🤖 AI寄生 (腐蚀:{aiParasite.corruptionLevel:F0}%)</color>");
        return sb.ToString();
    }

    // ============ 存档序列化辅助 ============
    // 注意：Unity JsonUtility 不支持 Dictionary 和 某些嵌套类型
    // 存档时调用 ToSerializableData() 转为纯数据类

    /// <summary>
    /// 转为可序列化数据结构（用于 SaveManager）
    /// </summary>
    public SerializableItemInstance ToSerializableData()
    {
        return new SerializableItemInstance
        {
            instanceID = this.instanceID,
            itemID = this.itemID,
            itemName = this.itemName,
            category = (int)this.category,
            baseValue = this.baseValue,
            isAuthentic = this.isAuthentic,
            isStolen = this.isStolen,
            isHazardous = this.isHazardous,
            isQuarantined = this.isQuarantined,
            owner = (int)this.owner,
            aiParasiteJSON = this.aiParasite != null ? JsonUtility.ToJson(this.aiParasite) : ""
        };
    }

    /// <summary>
    /// 从可序列化数据恢复
    /// </summary>
    public static ItemInstance FromSerializableData(SerializableItemInstance data)
    {
        if (data == null) return null;

        var instance = new ItemInstance
        {
            instanceID = data.instanceID,
            itemID = data.itemID,
            itemName = data.itemName,
            category = (ItemCategory)data.category,
            baseValue = data.baseValue,
            isAuthentic = data.isAuthentic,
            isStolen = data.isStolen,
            isHazardous = data.isHazardous,
            isQuarantined = data.isQuarantined,
            owner = (ItemOwnership)data.owner
        };

        if (!string.IsNullOrEmpty(data.aiParasiteJSON))
        {
            instance.aiParasite = JsonUtility.FromJson<AIParasiteData>(data.aiParasiteJSON);
        }

        return instance;
    }
}

/// <summary>
/// 可序列化物品数据（用于 JsonUtility 存档）
/// 纯值类型，无 Dictionary，无复杂嵌套
/// </summary>
[Serializable]
public class SerializableItemInstance
{
    public string instanceID;
    public string itemID;
    public string itemName;
    public int category;           // ItemCategory 枚举的 int 值
    public int baseValue;
    public float currentCondition;
    public bool isAuthentic;
    public bool isStolen;
    public bool isHazardous;
    public bool isQuarantined;
    public int owner;          // ItemOwnership 枚举的 int 值
    public string aiParasiteJSON;  // AIParasiteData 的 JSON 字符串
}