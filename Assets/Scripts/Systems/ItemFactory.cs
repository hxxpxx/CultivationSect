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

    // 内置兜底物品池：数据库为空或加载不全时使用，保证卖家客户始终能生成有名字、有价格的物品
    static readonly (string id, string name, ItemCategory cat, int value, ItemRarity rarity)[] FallbackPool =
    {
        ("fb_herb_1",  "赤焰椒",   ItemCategory.Herb,     10,  ItemRarity.Common),
        ("fb_herb_2",  "寒髓苔",   ItemCategory.Herb,     18,  ItemRarity.Uncommon),
        ("fb_herb_3",  "金魂花",   ItemCategory.Herb,     35,  ItemRarity.Rare),
        ("fb_ore_1",   "寒金石",   ItemCategory.Ore,      25,  ItemRarity.Uncommon),
        ("fb_pill_1",  "聚灵丹",   ItemCategory.Pill,     40,  ItemRarity.Rare),
        ("fb_pill_2",  "启丹",     ItemCategory.Pill,     80,  ItemRarity.Epic),
        ("fb_talisman","金元符",   ItemCategory.Talisman, 55,  ItemRarity.Rare),
        ("fb_weapon",  "飞剑",     ItemCategory.Weapon,   120, ItemRarity.Epic),
    };

    /// <summary>
    /// 随机生成一个物品（给 ClientGenerator 用）。
    /// 优先从数据库按权重抽取；数据库为空/加载不全导致取不到可用物品时，
    /// 回退到内置兜底物品池，保证生成器始终能产出有名字、有价格的物品。
    /// </summary>
    public ItemInstance CreateRandom(System.Func<ItemDef, float> weightMod = null)
    {
        // 1) 尝试数据库
        if (itemDatabase != null)
        {
            var def = itemDatabase.GetRandomByWeight(weightMod);
            if (def != null && !string.IsNullOrEmpty(def.itemID))
                return CreateFromDef(def);
        }

        // 2) 兜底：内置物品池
        var fb = FallbackPool[Random.Range(0, FallbackPool.Length)];
        Debug.LogWarning($"[ItemFactory] 数据库不可用(空/加载不全)，使用内置兜底物品: {fb.name}");
        return new ItemInstance
        {
            instanceID = System.Guid.NewGuid().ToString().Substring(0, 8),
            itemID = fb.id,
            itemName = fb.name,
            category = fb.cat,
            baseValue = fb.value,
            rarity = fb.rarity,
            isAuthentic = true,
            isStolen = false,
            isHazardous = false,
            customProperties = new System.Collections.Generic.Dictionary<string, object>()
        };
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
