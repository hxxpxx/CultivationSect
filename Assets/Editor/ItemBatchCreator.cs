// ===== ItemBatchCreator.cs =====
// Assets/Editor/ItemBatchCreator.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class ItemBatchCreator : EditorWindow
{
    string csvText = "";
    string outputFolder = "Assets/Resources/Items";

    [MenuItem("DeepSpacePawn/Tools/Item Batch Creator")]
    public static void ShowWindow()
    {
        GetWindow<ItemBatchCreator>("物品批量创建");
    }

    void OnGUI()
    {
        GUILayout.Label("批量创建物品 (CSV格式)", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("CSV 格式: itemID,name,category,baseValue,rarity,description");
        GUILayout.Label("category: Material/Weapon/Tech/AI/Contraband/Unknown");
        GUILayout.Label("示例: laser_core,激光核心,Weapon,2500,Rare,高能激光装置核心");
        GUILayout.Space(5);

        csvText = EditorGUILayout.TextArea(csvText, GUILayout.Height(200));

        GUILayout.Space(10);
        outputFolder = EditorGUILayout.TextField("输出目录", outputFolder);

        GUILayout.Space(10);
        if (GUILayout.Button("从 CSV 创建", GUILayout.Height(40)))
        {
            CreateFromCSV();
        }

        GUILayout.Space(5);
        if (GUILayout.Button("打开输出目录"))
        {
            System.Diagnostics.Process.Start(outputFolder);
        }
    }

    void CreateFromCSV()
    {
        if (string.IsNullOrEmpty(csvText))
        {
            Debug.LogWarning("CSV 内容为空");
            return;
        }

        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        string[] lines = csvText.Split('\n');
        int created = 0;
        int skipped = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] parts = line.Split(',');
            if (parts.Length < 6)
            {
                Debug.LogWarning($"第 {i + 1} 行格式错误，跳过: {line}");
                skipped++;
                continue;
            }

            string itemID = parts[0].Trim();
            string name = parts[1].Trim();
            string categoryStr = parts[2].Trim();
            int baseValue = int.Parse(parts[3].Trim());
            string rarityStr = parts[4].Trim();
            string desc = parts[5].Trim();

            // 检查是否已存在
            string assetPath = $"{outputFolder}/{itemID}.asset";
            if (File.Exists(assetPath))
            {
                skipped++;
                continue;
            }

            var def = ScriptableObject.CreateInstance<ItemDef>();
            def.itemId = itemID;
            def.itemName = name;
            def.description = desc;
            def.baseValue = baseValue;

            // 枚举解析
            if (System.Enum.TryParse<ItemCategory>(categoryStr, out var cat))
                def.itemCategory = cat;
            if (System.Enum.TryParse<ItemRarity>(rarityStr, out var rar))
                def.rarity = rar;

            AssetDatabase.CreateAsset(def, assetPath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ItemBatchCreator] 完成! 创建: {created}, 跳过: {skipped}");
        EditorUtility.DisplayDialog("完成", $"创建: {created}\n跳过: {skipped}", "OK");
    }
}