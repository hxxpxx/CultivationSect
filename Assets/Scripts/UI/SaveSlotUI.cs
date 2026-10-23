//// ===== SaveSlotUI.cs =====
//// Assets/Scripts/UI/SaveSlotUI.cs
//using UnityEngine;
//using UnityEngine.UI;

//public class SaveSlotUI : MonoBehaviour
//{
//    [Header("子物体引用（自动查找）")]
//    public Text slotNumberText;
//    public Text dayText;
//    public Text fundsText;
//    public Text timeText;
//    public Text endingText;
//    public Button slotButton;
//    public Button deleteButton;
//    public GameObject emptyLabel;

//    int slotIndex;
//    SaveMenuUI.MenuMode mode;
//    System.Action<int> onClickCallback;

//    public void Setup(SaveData data, int index, SaveMenuUI.MenuMode currentMode, System.Action<int> onClick)
//    {
//        slotIndex = index;
//        mode = currentMode;
//        onClickCallback = onClick;

//        // 自动查找子组件（如果未手动绑定）
//        if (slotNumberText == null)
//            slotNumberText = transform.Find("SlotNumberText")?.GetComponent<Text>();
//        if (dayText == null)
//            dayText = transform.Find("DayText")?.GetComponent<Text>();
//        if (fundsText == null)
//            fundsText = transform.Find("FundsText")?.GetComponent<Text>();
//        if (timeText == null)
//            timeText = transform.Find("TimeText")?.GetComponent<Text>();
//        if (endingText == null)
//            endingText = transform.Find("EndingText")?.GetComponent<Text>();
//        if (slotButton == null)
//            slotButton = GetComponent<Button>() ?? transform.Find("SlotButton")?.GetComponent<Button>();
//        if (deleteButton == null)
//            deleteButton = transform.Find("DeleteButton")?.GetComponent<Button>();
//        if (emptyLabel == null)
//            emptyLabel = transform.Find("EmptyLabel")?.gameObject;

//        // 填充内容
//        if (slotNumberText != null)
//            slotNumberText.text = $"槽位 {index + 1}";

//        if (data.isEmpty)
//        {
//            // 空槽
//            if (emptyLabel != null) emptyLabel.SetActive(true);
//            if (dayText != null) dayText.text = "— 空 —";
//            if (fundsText != null) fundsText.text = "";
//            if (timeText != null) timeText.text = "";
//            if (endingText != null) endingText.text = "";
//            if (deleteButton != null) deleteButton.gameObject.SetActive(false);
//        }
//        else
//        {
//            // 有数据
//            if (emptyLabel != null) emptyLabel.SetActive(false);
//            if (dayText != null) dayText.text = $"第 {data.day} 天";
//            if (fundsText != null) fundsText.text = $"资金: {data.funds}";
//            if (timeText != null) timeText.text = data.saveTime;
//            if (endingText != null) endingText.text = data.endingHint;
//            if (deleteButton != null) deleteButton.gameObject.SetActive(true);
//        }

//        // 按钮事件
//        if (slotButton != null)
//        {
//            slotButton.onClick.RemoveAllListeners();
//            slotButton.onClick.AddListener(() => onClickCallback?.Invoke(slotIndex));
//        }

//        if (deleteButton != null)
//        {
//            deleteButton.onClick.RemoveAllListeners();
//            deleteButton.onClick.AddListener(() => OnDeleteClicked());
//        }
//    }

//    void OnDeleteClicked()
//    {
//        var saveMenu = FindObjectOfType<SaveMenuUI>();
//        var pawnUI = FindObjectOfType<PawnDeskUI>();

//        if (pawnUI != null)
//        {
//            pawnUI.ShowConfirmDialog(
//                "删除存档",
//                $"确定删除存档槽 {slotIndex + 1}？此操作不可撤销。",
//                () => {
//                    SaveManager.Instance.DeleteSave(slotIndex);
//                    if (saveMenu != null)
//                        saveMenu.RefreshSlots();
//                    pawnUI.ShowToast($"存档槽 {slotIndex + 1} 已删除");
//                },
//                null
//            );
//        }
//    }
//}