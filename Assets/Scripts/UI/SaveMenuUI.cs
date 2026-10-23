//// ===== SaveMenuUI.cs =====
//// Assets/Scripts/UI/SaveMenuUI.cs
//using UnityEngine;
//using UnityEngine.UI;
//using System;

//public class SaveMenuUI : MonoBehaviour
//{
//    [Header("面板引用")]
//    public GameObject saveMenuPanel;
//    public Transform slotContainer;
//    public GameObject saveSlotPrefab;

//    [Header("按钮")]
//    public Button closeButton;
//    public Button refreshButton;

//    [Header("标题")]
//    public Text titleText;

//    [Header("模式")]
//    public enum MenuMode { Save, Load }
//    [HideInInspector] public MenuMode currentMode = MenuMode.Save;

//    // 运行时
//    SaveSlotUI[] slotUIs;

//    void Start()
//    {
//        if (closeButton != null)
//            closeButton.onClick.AddListener(Hide);
//        if (refreshButton != null)
//            refreshButton.onClick.AddListener(RefreshSlots);

//        // 初始隐藏
//        if (saveMenuPanel != null)
//            saveMenuPanel.SetActive(false);
//    }

//    // ============ 公开方法 ============

//    public void ShowSaveMode()
//    {
//        currentMode = MenuMode.Save;
//        if (titleText != null)
//            titleText.text = "保存游戏";
//        RefreshSlots();
//        if (saveMenuPanel != null)
//            saveMenuPanel.SetActive(true);
//    }

//    public void ShowLoadMode()
//    {
//        currentMode = MenuMode.Load;
//        if (titleText != null)
//            titleText.text = "读取游戏";
//        RefreshSlots();
//        if (saveMenuPanel != null)
//            saveMenuPanel.SetActive(true);
//    }

//    public void Hide()
//    {
//        if (saveMenuPanel != null)
//            saveMenuPanel.SetActive(false);
//    }

//    public void RefreshSlots()
//    {
//        // 清理旧槽位
//        if (slotUIs != null)
//        {
//            foreach (var slot in slotUIs)
//            {
//                if (slot != null && slot.gameObject != null)
//                    Destroy(slot.gameObject);
//            }
//        }

//        int slotCount = SaveManager.Instance != null ? SaveManager.Instance.maxSaveSlots : 3;
//        slotUIs = new SaveSlotUI[slotCount];

//        for (int i = 0; i < slotCount; i++)
//        {
//            int slotIndex = i; // 闭包捕获
//            GameObject slotGO;

//            if (saveSlotPrefab != null)
//            {
//                slotGO = Instantiate(saveSlotPrefab, slotContainer);
//            }
//            else
//            {
//                // 动态创建简易槽位
//                slotGO = CreateDefaultSlot(slotIndex);
//            }

//            var slotUI = slotGO.GetComponent<SaveSlotUI>();
//            if (slotUI == null)
//                slotUI = slotGO.AddComponent<SaveSlotUI>();

//            // 填充数据
//            var data = SaveManager.Instance.GetSlotInfo(slotIndex);
//            slotUI.Setup(data, slotIndex, currentMode, OnSlotClicked);
//            slotUIs[slotIndex] = slotUI;
//        }
//    }

//    // ============ 内部 ============

//    void OnSlotClicked(int slotIndex)
//    {
//        if (SaveManager.Instance == null) return;

//        switch (currentMode)
//        {
//            case MenuMode.Save:
//                // 如果已有存档，弹出确认
//                if (SaveManager.Instance.HasSave(slotIndex))
//                {
//                    ShowOverwriteConfirm(slotIndex);
//                }
//                else
//                {
//                    SaveManager.Instance.SaveGame(slotIndex);
//                    RefreshSlots();
//                    ShowToast($"存档槽 {slotIndex + 1} 保存成功");
//                }
//                break;

//            case MenuMode.Load:
//                if (SaveManager.Instance.HasSave(slotIndex))
//                {
//                    ShowLoadConfirm(slotIndex);
//                }
//                else
//                {
//                    ShowToast($"存档槽 {slotIndex + 1} 为空");
//                }
//                break;
//        }
//    }

//    void ShowOverwriteConfirm(int slotIndex)
//    {
//        // 调用通用确认弹窗
//        var confirmPanel = FindObjectOfType<PawnDeskUI>();
//        if (confirmPanel != null)
//        {
//            confirmPanel.ShowConfirmDialog(
//                "覆盖存档",
//                $"存档槽 {slotIndex + 1} 已有数据，是否覆盖？",
//                () => {
//                    SaveManager.Instance.SaveGame(slotIndex);
//                    RefreshSlots();
//                    ShowToast($"存档槽 {slotIndex + 1} 已覆盖");
//                },
//                null
//            );
//        }
//    }

//    void ShowLoadConfirm(int slotIndex)
//    {
//        var confirmPanel = FindObjectOfType<PawnDeskUI>();
//        if (confirmPanel != null)
//        {
//            confirmPanel.ShowConfirmDialog(
//                "读取存档",
//                $"确定读取存档槽 {slotIndex + 1}？\n当前进度将丢失。",
//                () => {
//                    SaveManager.Instance.LoadGame(slotIndex);
//                    Hide();
//                    ShowToast($"已读取存档槽 {slotIndex + 1}");
//                },
//                null
//            );
//        }
//    }

//    void ShowToast(string msg)
//    {
//        var pawnUI = FindObjectOfType<PawnDeskUI>();
//        if (pawnUI != null)
//            pawnUI.ShowToast(msg);
//    }

//    GameObject CreateDefaultSlot(int index)
//    {
//        // 动态创建简易槽位物体
//        GameObject go = new GameObject($"SaveSlot_{index}");
//        go.transform.SetParent(slotContainer, false);

//        // 背景 Image
//        var img = go.AddComponent<Image>();
//        img.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

//        // RectTransform
//        var rt = go.GetComponent<RectTransform>();
//        rt.sizeDelta = new Vector2(500, 80);

//        return go;
//    }
//}