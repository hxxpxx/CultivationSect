//// ===== SaveManager.cs =====
//// Assets/Scripts/Core/SaveManager.cs
//using UnityEngine;
//using System.IO;
//using System;

//[Serializable]
//public class SaveData
//{
//    public int saveSlot;
//    public string saveTime;
//    public int day;
//    public int funds;
//    public int reputation;
//    public int wantedLevel;
//    public int energy;
//    public int totalPawns;
//    public int totalReports;
//    public int totalBlackMarket;
//    public string endingHint;        // 当前倾向的结局提示
//    public bool isEmpty = true;
//}

//public class SaveManager : MonoBehaviour
//{
//    public static SaveManager Instance { get; private set; }

//    public int maxSaveSlots = 3;
//    SaveData[] cachedSlots;

//    const string SAVE_DIR = "Saves";
//    const string SAVE_PREFIX = "save_slot_";

//    void Awake()
//    {
//        if (Instance != null && Instance != this)
//        {
//            Destroy(gameObject);
//            return;
//        }
//        Instance = this;
//        DontDestroyOnLoad(gameObject);
//        cachedSlots = new SaveData[maxSaveSlots];
//        RefreshCache();
//    }

//    // ============ 核心 API ============

//    public void SaveGame(int slot)
//    {
//        if (slot < 0 || slot >= maxSaveSlots)
//        {
//            Debug.LogError($"[SaveManager] 无效存档槽: {slot}");
//            return;
//        }

//        var data = new SaveData
//        {
//            saveSlot = slot,
//            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
//            day = GameStateRuntime.Instance != null ? GameStateRuntime.Instance.currentDay : 1,
//            funds = GameStateRuntime.Instance != null ? GameStateRuntime.Instance.funds : 0,
//            reputation = GameStateRuntime.Instance != null ? GameStateRuntime.Instance.reputation : 50,
//            wantedLevel = GameStateRuntime.Instance != null ? GameStateRuntime.Instance.wantedLevel : 0,
//            energy = GameStateRuntime.Instance != null ? GameStateRuntime.Instance.energy : 100,
//            totalPawns = PawnLedger.Instance != null ? PawnLedger.Instance.totalPawns : 0,
//            totalReports = PawnLedger.Instance != null ? PawnLedger.Instance.totalReports : 0,
//            totalBlackMarket = PawnLedger.Instance != null ? PawnLedger.Instance.totalBlackMarket : 0,
//            endingHint = GetCurrentEndingHint(),
//            isEmpty = false
//        };

//        string json = JsonUtility.ToJson(data, true);
//        string path = GetSavePath(slot);
//        Directory.CreateDirectory(Path.GetDirectoryName(path));
//        File.WriteAllText(path, json);

//        cachedSlots[slot] = data;
//        Debug.Log($"[SaveManager] 存档槽 {slot} 保存成功");
//        GameEvents.SafeInvoke(GameEvents.OnSaveComplete, slot);
//    }

//    public SaveData LoadGame(int slot)
//    {
//        if (slot < 0 || slot >= maxSaveSlots)
//        {
//            Debug.LogError($"[SaveManager] 无效存档槽: {slot}");
//            return null;
//        }

//        string path = GetSavePath(slot);
//        if (!File.Exists(path))
//        {
//            Debug.LogWarning($"[SaveManager] 存档槽 {slot} 不存在");
//            return null;
//        }

//        string json = File.ReadAllText(path);
//        var data = JsonUtility.FromJson<SaveData>(json);
//        cachedSlots[slot] = data;

//        // 恢复 GameState
//        if (GameStateRuntime.Instance != null)
//        {
//            GameStateRuntime.Instance.currentDay = data.day;
//            GameStateRuntime.Instance.funds = data.funds;
//            GameStateRuntime.Instance.reputation = data.reputation;
//            GameStateRuntime.Instance.wantedLevel = data.wantedLevel;
//            GameStateRuntime.Instance.energy = data.energy;
//        }

//        Debug.Log($"[SaveManager] 存档槽 {slot} 读取成功");
//        GameEvents.SafeInvoke(GameEvents.OnLoadComplete, slot);
//        return data;
//    }

//    public void DeleteSave(int slot)
//    {
//        if (slot < 0 || slot >= maxSaveSlots) return;

//        string path = GetSavePath(slot);
//        if (File.Exists(path))
//            File.Delete(path);

//        cachedSlots[slot] = CreateEmptySlot(slot);
//        Debug.Log($"[SaveManager] 存档槽 {slot} 已删除");
//        GameEvents.SafeInvoke(GameEvents.OnSaveDeleted, slot);
//    }

//    public SaveData GetSlotInfo(int slot)
//    {
//        if (slot < 0 || slot >= maxSaveSlots) return null;
//        if (cachedSlots[slot] == null)
//            cachedSlots[slot] = CreateEmptySlot(slot);
//        return cachedSlots[slot];
//    }

//    public bool HasSave(int slot)
//    {
//        if (slot < 0 || slot >= maxSaveSlots) return false;
//        string path = GetSavePath(slot);
//        return File.Exists(path) && !cachedSlots[slot]?.isEmpty == true;
//    }

//    public void RefreshCache()
//    {
//        for (int i = 0; i < maxSaveSlots; i++)
//        {
//            string path = GetSavePath(i);
//            if (File.Exists(path))
//            {
//                string json = File.ReadAllText(path);
//                cachedSlots[i] = JsonUtility.FromJson<SaveData>(json);
//            }
//            else
//            {
//                cachedSlots[i] = CreateEmptySlot(i);
//            }
//        }
//    }

//    // ============ 内部 ============

//    string GetSavePath(int slot)
//    {
//        return Path.Combine(Application.persistentDataPath, SAVE_DIR, $"{SAVE_PREFIX}{slot}.json");
//    }

//    SaveData CreateEmptySlot(int slot)
//    {
//        return new SaveData
//        {
//            saveSlot = slot,
//            saveTime = "",
//            day = 1,
//            funds = 0,
//            reputation = 50,
//            wantedLevel = 0,
//            energy = 100,
//            isEmpty = true
//        };
//    }

//    string GetCurrentEndingHint()
//    {
//        if (GameStateRuntime.Instance == null) return "未知";
//        int rep = GameStateRuntime.Instance.reputation;
//        int wanted = GameStateRuntime.Instance.wantedLevel;
//        int funds = GameStateRuntime.Instance.funds;

//        if (wanted >= 80) return "结局倾向：阶下囚";
//        if (rep >= 80 && funds >= 10000) return "结局倾向：深空大亨";
//        if (rep <= 20 && funds >= 5000) return "结局倾向：黑市之王";
//        if (rep >= 60 && wanted <= 10) return "结局倾向：模范当铺主";
//        return "结局倾向：未定";
//    }
//}