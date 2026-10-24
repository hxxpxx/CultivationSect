// ===== ItemFactory.cs (改造版) =====
using UnityEngine;

public class ItemFactory : MonoBehaviour
{
    private static ItemFactory _instance;

    /// <summary>懒加载单例：场景中不存在时自动创建，避免空引用</summary>
    public static ItemFactory Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ItemFactory>();
                if (_instance == null)
                {
                    var go = new GameObject("ItemFactory");
                    _instance = go.AddComponent<ItemFactory>(); // 立即触发 Awake
                    Object.DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("数据库引用")]
    public ItemDatabase itemDatabase;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // 自动初始化数据库
        if (itemDatabase == null)
            itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");
        if (itemDatabase != null)
            itemDatabase.Initialize();
    }

    /// <summary>
    /// 按ID创建物品实例
    /// </summary>
    public ItemInstance CreateFromID(string itemID)
    {
        if (itemDatabase == null) return null;
        var def = itemDatabase.GetByID(itemID);
        if (def == null)
        {
            Debug.LogWarning($"[ItemFactory] 找不到物品: {itemID}");
            return null;
        }
        return CreateFromDef(def);
    }

    /// <summary>
    /// 按定义创建实例
    /// </summary>
    public ItemInstance CreateFromDef(ItemDef def)
    {
        if (def == null) return null;

        var instance = new ItemInstance
        {
            instanceID = System.Guid.NewGuid().ToString().Substring(0, 8),
            itemID = def.itemID,
            itemName = def.itemName,
            category = def.itemCategory,
            rarity = def.itemRarity,
            baseValue = def.baseValue,
            isAuthentic = true,
            isStolen = false,
            isHazardous = false,
            customProperties = new System.Collections.Generic.Dictionary<string, object>()
        };

        return instance;
    }

    /// <summary>
    /// 随机生成一个物品（给 ClientGenerator 用）
    /// </summary>
    public ItemInstance CreateRandom(System.Func<ItemDef, float> weightMod = null)
    {
        if (itemDatabase == null) return null;
        var def = itemDatabase.GetRandomByWeight(weightMod);
        if (def == null) return null;
        return CreateFromDef(def);
    }

    /// <summary>
    /// 按类别随机生成
    /// </summary>
    public ItemInstance CreateRandomByCategory(ItemCategory category)
    {
        if (itemDatabase == null) return null;
        var list = itemDatabase.GetByCategory(category);
        if (list.Count == 0) return null;
        var def = list[Random.Range(0, list.Count)];
        return CreateFromDef(def);
    }
}