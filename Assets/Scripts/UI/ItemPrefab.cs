// ===== ItemPrefab.cs =====
// 物品UI预制体封装：接收 ItemInstance 并渲染到预制体，点击选中。
// 挂到物品按钮预制体/场景物品按钮根节点（如 ItemButtonPrefab / ItemsPrefab）。
// 方案B：不依赖“根节点自带 Button + 猜子TMP”的旧假设，由本脚本统一赋值。
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemPrefab : MonoBehaviour
{
    [Header("引用（Inspector 拖拽，可为空）")]
    public Image icon;                  // 物品图标
    public TextMeshProUGUI nameText;    // 物品名
    public TextMeshProUGUI offerText;   // 估价文本（窗口留空，柜台显示估价）
    public Button selectButton;         // 选中按钮（缺省取自身/子节点第一个 Button）

    public ItemInstance Item { get; private set; }

    void Awake()
    {
        if (selectButton == null)
            selectButton = GetComponent<Button>() ?? GetComponentInChildren<Button>();
        if (selectButton != null)
            selectButton.onClick.AddListener(OnClick);
    }

    /// <summary>用物品数据填充预制体。showOffer 为 true 时在 offerText 显示估价。</summary>
    public void Init(ItemInstance item, bool showOffer)
    {
        Item = item;

        if (nameText != null)
            nameText.text = item != null ? item.itemName : "";
        if (offerText != null)
            offerText.text = (showOffer && item != null) ? $"估价: {item.appraisedOffer:F0}" : "";

        // 可选：从 ItemDatabase 读取图标填充
        if (icon != null && item != null)
        {
            var def = ItemFactory.Instance != null
                ? ItemFactory.Instance.itemDatabase?.GetByID(item.itemID) : null;
            if (def != null && def.itemIcon != null)
                icon.sprite = def.itemIcon;
        }
    }

    void OnClick()
    {
        if (Item == null) return;
        var m = FindObjectOfType<PawnDeskManager>();
        if (m != null) m.SelectItem(Item);
    }
}
