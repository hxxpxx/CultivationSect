// ===== ItemBatchCreator.cs =====
// Assets/Editor/ItemBatchCreator.cs
// 路线 B：CSV 仅作为编辑期输入，工具将其烘烤为 ItemDef(.asset) 资产；
// 运行时仍由 ItemDatabase 读取 .asset，不在运行时解析 CSV。
//
// CSV 列（表头可选，按名称映射，顺序可调）:
//   itemID, name, category, baseValue, rarity, description[, icon]
// category: Herb/Ore/Pet/Pill/Weapon/Talisman（或 灵草/灵矿/灵宠/丹药/武器/符箓）
// rarity  : Common/Uncommon/Rare/Epic/Legendary/Mythic（或 白/绿/蓝/紫/金/红），留空按 Common
// icon(可选): Sprite 资产路径，如 Assets/Resources/Items/Icons/herb_001.png，留空用默认
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class ItemBatchCreator : EditorWindow
{
    string csvText = "";
    string outputFolder = "Assets/Resources/Items";

    // 类别中文↔枚举映射（大小写不敏感）
    static readonly Dictionary<string, ItemCategory> CategoryMap = new Dictionary<string, ItemCategory>
    {
        { "herb", ItemCategory.Herb },     { "ore", ItemCategory.Ore },
        { "pet", ItemCategory.Pet },       { "pill", ItemCategory.Pill },
        { "weapon", ItemCategory.Weapon }, { "talisman", ItemCategory.Talisman },
        { "灵草", ItemCategory.Herb },     { "灵矿", ItemCategory.Ore },
        { "灵宠", ItemCategory.Pet },      { "丹药", ItemCategory.Pill },
        { "武器", ItemCategory.Weapon },   { "符箓", ItemCategory.Talisman },
    };

    // 品级中文↔枚举映射
    static readonly Dictionary<string, ItemRarity> RarityMap = new Dictionary<string, ItemRarity>
    {
        { "common", ItemRarity.Common },   { "uncommon", ItemRarity.Uncommon },
        { "rare", ItemRarity.Rare },       { "epic", ItemRarity.Epic },
        { "legendary", ItemRarity.Legendary }, { "mythic", ItemRarity.Mythic },
        { "白", ItemRarity.Common },       { "绿", ItemRarity.Uncommon },
        { "蓝", ItemRarity.Rare },         { "紫", ItemRarity.Epic },
        { "金", ItemRarity.Legendary },    { "红", ItemRarity.Mythic },
    };

    [MenuItem("DeepSpacePawn/Tools/Item Batch Creator")]
    public static void ShowWindow()
    {
        GetWindow<ItemBatchCreator>("物品批量创建");
    }

    void OnGUI()
    {
        GUILayout.Label("批量创建物品 (CSV → .asset，编辑期输入，运行时读资产)", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("CSV 列: itemID,name,category,baseValue,rarity,description[,icon]", EditorStyles.wordWrappedLabel);
        GUILayout.Label("category: Herb/Ore/Pet/Pill/Weapon/Talisman（或 灵草/灵矿/灵宠/丹药/武器/符箓）", EditorStyles.wordWrappedLabel);
        GUILayout.Label("rarity: Common/Uncommon/Rare/Epic/Legendary/Mythic（或 白/绿/蓝/紫/金/红，留空=Common）", EditorStyles.wordWrappedLabel);
        GUILayout.Label("icon(可选): Sprite 资产路径，如 Assets/Resources/Items/Icons/herb_001.png，留空用默认", EditorStyles.wordWrappedLabel);
        GUILayout.Label("示例: herb_lingshan_01,龙涎草,Herb,1200,Rare,,蕴含龙息的灵草", EditorStyles.wordWrappedLabel);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("从 CSV 文件加载", GUILayout.Height(30)))
            LoadFromFile();
        if (GUILayout.Button("从 CSV 创建", GUILayout.Height(30)))
            CreateFromCSV();
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        outputFolder = EditorGUILayout.TextField("输出目录", outputFolder);

        GUILayout.Space(5);
        csvText = EditorGUILayout.TextArea(csvText, GUILayout.Height(220));

        GUILayout.Space(5);
        if (GUILayout.Button("打开输出目录", GUILayout.Height(28)))
            System.Diagnostics.Process.Start(outputFolder);
    }

    // ============ 文件输入 ============

    void LoadFromFile()
    {
        string path = EditorUtility.OpenFilePanel("选择 CSV 文件", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            csvText = DecodeCsv(File.ReadAllBytes(path));
            Debug.Log($"[ItemBatchCreator] 已加载 CSV: {path} ({csvText.Length} 字符)");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ItemBatchCreator] 读取文件失败: {e.Message}");
        }
    }

    // UTF-8 BOM / 严格 UTF-8 / GBK(936) 编码兜底，避免 Excel 另存为乱码
    static string DecodeCsv(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        try { return new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException) { return Encoding.GetEncoding(936).GetString(bytes); }
    }

    // ============ 创建逻辑 ============

    void CreateFromCSV()
    {
        if (string.IsNullOrWhiteSpace(csvText))
        {
            Debug.LogWarning("[ItemBatchCreator] CSV 内容为空");
            return;
        }
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        var rows = ParseCsv(csvText);
        if (rows.Count == 0)
        {
            Debug.LogWarning("[ItemBatchCreator] 未解析到任何行");
            return;
        }

        // 用首行做列名映射（若首行像表头）
        var col = MapColumns(rows[0]);
        bool hasHeader = col.Count > 0;
        int startRow = hasHeader ? 1 : 0;
        if (!hasHeader)
            col = DefaultColumns();

        int created = 0, skipped = 0, failed = 0;
        for (int i = startRow; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count == 0) continue;
            if (row.Count == 1 && string.IsNullOrWhiteSpace(row[0])) continue; // 空行
            if (row[0].TrimStart().StartsWith("#")) continue;                    // 注释行

            if (!TryBuildDef(row, col, out var def, out string err))
            {
                Debug.LogWarning($"[ItemBatchCreator] 第 {i + 1} 行解析失败，跳过: {err}");
                failed++;
                continue;
            }

            string assetPath = $"{outputFolder}/{def.itemID}.asset";
            if (File.Exists(assetPath) || AssetDatabase.LoadAssetAtPath<ItemDef>(assetPath) != null)
            {
                skipped++;
                continue;
            }

            AssetDatabase.CreateAsset(def, assetPath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ItemBatchCreator] 完成! 创建: {created}, 跳过(已存在): {skipped}, 失败: {failed}");
        EditorUtility.DisplayDialog("完成", $"创建: {created}\n跳过(已存在): {skipped}\n失败: {failed}", "OK");
    }

    // 无表头时的固定列序
    static Dictionary<string, int> DefaultColumns() => new Dictionary<string, int>
    {
        { "itemID", 0 }, { "name", 1 }, { "category", 2 },
        { "baseValue", 3 }, { "rarity", 4 }, { "description", 5 }, { "icon", 6 },
    };

    // 根据首行识别表头并映射 列名→索引（大小写不敏感，支持中英文别名）
    static Dictionary<string, int> MapColumns(List<string> header)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Count; i++)
        {
            string h = (header[i] ?? "").Trim().ToLowerInvariant();
            string key = null;
            if (h == "id" || h.Contains("itemid")) key = "itemID";
            else if (h == "name" || h.Contains("itemname")) key = "name";
            else if (h.Contains("category") || h.Contains("分类")) key = "category";
            else if (h == "value" || h.Contains("basevalue") || h.Contains("价值")) key = "baseValue";
            else if (h.Contains("rarity") || h.Contains("品级") || h.Contains("品质")) key = "rarity";
            else if (h == "desc" || h.Contains("description") || h.Contains("描述")) key = "description";
            else if (h.Contains("icon") || h.Contains("图标")) key = "icon";

            if (key != null && !map.ContainsKey(key)) map[key] = i;
        }
        return map;
    }

    bool TryBuildDef(List<string> row, Dictionary<string, int> col, out ItemDef def, out string error)
    {
        def = null; error = null;

        string Get(string field)
            => col.TryGetValue(field, out var idx) && idx < row.Count ? (row[idx] ?? "").Trim() : "";

        string itemID = Get("itemID");
        if (string.IsNullOrEmpty(itemID)) { error = "缺少 itemID"; return false; }

        string name = Get("name");
        if (string.IsNullOrEmpty(name)) { error = $"itemID={itemID} 缺少 name"; return false; }

        string valueStr = Get("baseValue");
        if (!int.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int baseValue))
        { error = $"itemID={itemID} baseValue 非法: '{valueStr}'"; return false; }

        string categoryStr = Get("category");
        if (!TryEnum(categoryStr, CategoryMap, out ItemCategory category))
        { error = $"itemID={itemID} category 非法: '{categoryStr}'"; return false; }

        string rarityStr = Get("rarity");
        ItemRarity rarity = ItemRarity.Common;
        if (!string.IsNullOrEmpty(rarityStr) && !TryEnum(rarityStr, RarityMap, out rarity))
        { error = $"itemID={itemID} rarity 非法: '{rarityStr}'"; return false; }

        var d = ScriptableObject.CreateInstance<ItemDef>();
        d.itemID = itemID;
        d.itemName = name;
        d.itemCategory = category;
        d.baseValue = baseValue;
        d.itemRarity = rarity;
        d.description = Get("description");

        string iconPath = Get("icon");
        d.itemIcon = null;
        if (!string.IsNullOrEmpty(iconPath))
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite == null) Debug.LogWarning($"[ItemBatchCreator] itemID={itemID} 图标加载失败，使用默认: {iconPath}");
            else d.itemIcon = sprite;
        }

        def = d;
        return true;
    }

    // 优先查映射表（中文/小写），查不到再回退到 Enum.TryParse
    static bool TryEnum<TEnum>(string s, Dictionary<string, TEnum> map, out TEnum value) where TEnum : struct
    {
        value = default;
        if (string.IsNullOrEmpty(s)) return false;
        if (map.TryGetValue(s.Trim(), out value)) return true;
        return Enum.TryParse(s.Trim(), true, out value);
    }

    // ============ CSV 解析 ============
    // 支持引号包裹字段（描述含逗号/换行）、双引号转义、\r\n 与 \n

    static List<List<string>> ParseCsv(string text)
    {
        var rows = new List<List<string>>();
        if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1); // 去 BOM

        var row = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;
        int n = text.Length;

        for (int i = 0; i < n; i++)
        {
            char c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < n && text[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { row.Add(field.ToString()); field.Clear(); }
                else if (c == '\n')
                {
                    row.Add(field.ToString()); field.Clear();
                    rows.Add(row); row = new List<string>();
                }
                else if (c == '\r') { /* 换行统一按 \n 处理 */ }
                else field.Append(c);
            }
        }

        row.Add(field.ToString());
        rows.Add(row);
        return rows;
    }
}
