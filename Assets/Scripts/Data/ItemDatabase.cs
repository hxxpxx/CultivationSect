// ===== ItemDatabase.cs =====
// Assets/Scripts/Data/ItemDatabase.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(menuName = "CultivationSectLeader/ItemDatabase", fileName = "ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [Header("自动扫描配置")]
    public string scanFolder = "Items";          // Resources/Items/ 下的子目录
    public bool autoScanOnEnable = true;

    [Header("手动注册（少量特殊物品）")]
    public List<ItemDef> manualEntries = new List<ItemDef>();

    // ============ 运行时索引 ============
    Dictionary<string, ItemDef> byID = new Dictionary<string, ItemDef>();
    Dictionary<ItemCategory, List<ItemDef>> byCategory = new Dictionary<ItemCategory, List<ItemDef>>();
    List<ItemDef> allItems = new List<ItemDef>();
    bool initialized = false;

    // ============ 生命周期 ============

    void OnEnable()
    {
        if (autoScanOnEnable)
            Initialize();
    }

    public void Initialize()
    {
        if (initialized) return;
        byID.Clear();
        byCategory.Clear();
        allItems.Clear();

        // 1. 加载 Resources 目录下所有 ItemDef
        LoadFromResources();

        // 2. 合并手动注册
        foreach (var def in manualEntries)
        {
            if (def != null && !byID.ContainsKey(def.itemID))
                Register(def);
        }

        initialized = true;
        Debug.Log($"[ItemDatabase] 初始化完成，共加载 {allItems.Count} 个物品定义");
    }

    void LoadFromResources()
    {
#if UNITY_EDITOR
        // 编辑器下用 AssetDatabase 全扫描（支持子目录）
        string[] guids = UnityEditor.AssetDatabase.FindAssets(
            "t:ItemDef",
            new[] { $"Assets/Resources/{scanFolder}" }
        );
        foreach (var guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var def = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDef>(path);
            if (def != null)
                Register(def);
        }
#else
        // 运行时用 Resources.LoadAll
        var loaded = Resources.LoadAll<ItemDef>(scanFolder);
        foreach (var def in loaded)
            Register(def);
#endif
    }

    void Register(ItemDef def)
    {
        if (def == null || string.IsNullOrEmpty(def.itemID)) return;

        byID[def.itemID] = def;
        allItems.Add(def);

        if (!byCategory.ContainsKey(def.itemCategory))
            byCategory[def.itemCategory] = new List<ItemDef>();
        byCategory[def.itemCategory].Add(def);
    }

    // ============ 查询 API ============

    public ItemDef GetByID(string itemID)
    {
        if (!initialized) Initialize();
        byID.TryGetValue(itemID, out var def);
        return def;
    }

    public List<ItemDef> GetByCategory(ItemCategory category)
    {
        if (!initialized) Initialize();
        return byCategory.TryGetValue(category, out var list) ? list : new List<ItemDef>();
    }

    public List<ItemDef> GetAll()
    {
        if (!initialized) Initialize();
        return new List<ItemDef>(allItems);
    }

    /// <summary>
    /// 按权重随机抽一个（用于 ClientGenerator 生成客户携带物品）
    /// </summary>
    public ItemDef GetRandomByWeight(System.Func<ItemDef, float> weightModifier = null)
    {
        if (!initialized) Initialize();
        if (allItems.Count == 0) return null;

        float totalWeight = 0f;
        var weights = new float[allItems.Count];
        for (int i = 0; i < allItems.Count; i++)
        {
            float w = allItems[i].baseValue; // 默认用 baseValue 作权重
            if (weightModifier != null)
                w *= weightModifier(allItems[i]);
            weights[i] = w;
            totalWeight += w;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < allItems.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
                return allItems[i];
        }
        return allItems[allItems.Count - 1];
    }

    /// <summary>
    /// 按条件过滤
    /// </summary>
    public List<ItemDef> Filter(System.Func<ItemDef, bool> predicate)
    {
        if (!initialized) Initialize();
        return allItems.Where(predicate).ToList();
    }

    /// <summary>
    /// 分页查询（UI列表用，避免一次生成几百个按钮）
    /// </summary>
    public List<ItemDef> GetPaged(int page, int pageSize, ItemCategory? category = null)
    {
        if (!initialized) Initialize();

        var source = category.HasValue ? GetByCategory(category.Value) : allItems;
        int start = page * pageSize;
        if (start >= source.Count) return new List<ItemDef>();

        int count = Mathf.Min(pageSize, source.Count - start);
        return source.GetRange(start, count);
    }

    public int GetTotalCount(ItemCategory? category = null)
    {
        if (!initialized) Initialize();
        if (category.HasValue)
            return byCategory.TryGetValue(category.Value, out var list) ? list.Count : 0;
        return allItems.Count;
    }
}