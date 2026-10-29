// ===== PawnDeskUI.cs =====
// 挂载到 UI Canvas 或专门的 UIManager 物体上
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PawnDeskUI : MonoBehaviour
{
    // ============ Inspector 引用 ============
    [Header("顶部状态栏")]
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI fundsText;
    public TextMeshProUGUI spiritText;   // 灵能（玩家扫描鉴定与AI检测共用资源）

    [Header("玩家面板（声望/通缉，点开才显示）")]
    public GameObject playerPanel;      // 玩家面板：声望/通缉内容
    public Button playerButton;         // 打开玩家面板的按钮
    public TextMeshProUGUI reputationText;
    public TextMeshProUGUI wantedText;

    [Header("客户面板")]
    public GameObject clientPanel;
    public TextMeshProUGUI clientFactionText;
    public TextMeshProUGUI clientDialogueText;
    public TextMeshProUGUI clientInfoText;   // 可选：显示客户类型与预算（为空时追加到对白）
    public Image clientPortrait;


    [Header("物品面板")]
        public GameObject itemsPanel;
    public Transform itemsContainer;      // 「窗口」：客户物品初到时的落位（未估价）
    public GameObject itemPrefab;  // 含 Button + Text + Image

    [Header("初始物品放置窗口")]
    public GameObject windowPanel;
    public Transform windowContainer;   // 「窗口」运行时在屏幕左侧自动生成的竖列容器（不依赖场景旧容器）

    [Header("柜台估价面板（客户物品自动估价）")]
    public GameObject counterPanel;
    public Transform counterContainer;     // 「柜台」：拖到此处的物品自动估价；留空则运行时自动创建
    public TextMeshProUGUI counterCountText;   // 可选：显示 柜台估价物品数

    [Header("货架面板")]
    public GameObject shelfPanel;
    public Transform shelfItemsContainer;
 //   public GameObject shelfItemPrefab;      //复用 itemPrefab
    public Button shelfOpenButton;
    public Button shelfCloseButton;
    public TextMeshProUGUI shelfCountText;     // 可选，显示 货架占用/容量

    [Header("扫描/鉴定面板")]
    public GameObject inspectionPanel;
    public TextMeshProUGUI inspectionResultText;
    public TextMeshProUGUI offerText;
    public Button scanButton;
    public GameObject scanGlitchEffect; // 扫描异常时的闪烁特效

    [Header("决策按钮")]
    public Button buyButton;
    public Button sellButton;
    public Button rejectButton;

    [Header("日结算面板")]
    public GameObject dayReportPanel;
    public TextMeshProUGUI dayReportText;
    public Button nextDayButton;

    [Header("通用 UI")]
    public GameObject toastPanel;
    public TextMeshProUGUI toastText;
    public TextMeshProUGUI confirmTitleText;
    public TextMeshProUGUI confirmContentText;
    public Button confirmYesButton;
    public Button confirmNoButton;
    public Button nextClientButton;

    // ============ 内部引用 ============
    PawnDeskManager manager;
    List<GameObject> itemButtons = new List<GameObject>();
    List<GameObject> shelfButtons = new List<GameObject>();
    Coroutine toastCoroutine;
    Coroutine refreshCoroutine;

    // ============ 生命周期 ============
    void Start()
    {
        manager = FindObjectOfType<PawnDeskManager>();

        BindButtons();
        SubscribeEvents();
        EnsurePlayerPanel();   // 玩家面板：把声望/通缉移入默认隐藏的面板，点开才显示
        RefreshTopBar();
        HideAllPanels();
        EnsureContainers();      // 确保 窗口/柜台 两个放置区域与拖拽回调
        RefreshShelfItems();   // 预刷新货架占用/列表
        RefreshClientItems();  // 预刷新客户窗口/柜台物品

        dayReportPanel.SetActive(false);
    }

    void OnEnable()
    {
        SubscribeEvents();
    }

    void OnDisable()
    {
        UnsubscribeEvents();
    }

    void BindButtons()
    {
        scanButton?.onClick.AddListener(() => OnScanClicked());
        buyButton?.onClick.AddListener(() => OnDecisionClicked(PawnDeskManager.DecisionType.Buy));
        sellButton?.onClick.AddListener(() => OnDecisionClicked(PawnDeskManager.DecisionType.Sell));
        rejectButton?.onClick.AddListener(() => OnDecisionClicked(PawnDeskManager.DecisionType.Reject));
        nextDayButton?.onClick.AddListener(() => { dayReportPanel.SetActive(false); manager.AdvanceToNextDay(); });
        nextClientButton?.onClick.AddListener(() => OnNextClientClicked());

        shelfOpenButton?.onClick.AddListener(OpenShelf);
        shelfCloseButton?.onClick.AddListener(CloseShelf);
    }

    void SubscribeEvents()
    {
        if (manager == null) return;

        // Manager 实例事件
        manager.OnDayStarted += OnDayStartedHandler;
        manager.OnClientArrived += OnClientArrivedHandler;
        manager.OnItemSelected += OnItemSelectedHandler;
        manager.OnScanCompleted += OnScanCompletedHandler;
        manager.OnItemBought += OnItemBoughtHandler;
        manager.OnItemSold += OnItemSoldHandler;
        manager.OnClientItemPlacementChanged += OnClientItemPlacementChangedHandler;
        manager.OnSpiritEnergyChanged += OnSpiritEnergyChangedHandler;
        manager.OnDayEnded += OnDayEndedHandler;

        // GameEvents 系统通知
        GameEvents.OnFundsChanged += OnFundsChangedHandler;
        GameEvents.OnReputationChanged += OnReputationChangedHandler;
        GameEvents.OnWantedChanged += OnWantedChangedHandler;
        GameEvents.OnDayChanged += OnDayChangedHandler;
        GameEvents.OnScannerJammed += OnScannerJammedHandler;
        GameEvents.OnToastRequested += OnToastRequestedHandler;
        GameEvents.OnPlaySFX += OnPlaySFXHandler;
        GameEvents.OnScreenShake += OnScreenShakeHandler;
        GameEvents.OnInventoryChanged += OnInventoryChangedHandler;
    }

    void UnsubscribeEvents()
    {
        if (manager == null) return;

        manager.OnDayStarted -= OnDayStartedHandler;
        manager.OnClientArrived -= OnClientArrivedHandler;
        manager.OnItemSelected -= OnItemSelectedHandler;
        manager.OnScanCompleted -= OnScanCompletedHandler;
        manager.OnItemBought -= OnItemBoughtHandler;
        manager.OnItemSold -= OnItemSoldHandler;
        manager.OnClientItemPlacementChanged -= OnClientItemPlacementChangedHandler;
        manager.OnSpiritEnergyChanged -= OnSpiritEnergyChangedHandler;
        manager.OnDayEnded -= OnDayEndedHandler;

        GameEvents.OnFundsChanged -= OnFundsChangedHandler;
        GameEvents.OnReputationChanged -= OnReputationChangedHandler;
        GameEvents.OnWantedChanged -= OnWantedChangedHandler;
        GameEvents.OnDayChanged -= OnDayChangedHandler;
        GameEvents.OnScannerJammed -= OnScannerJammedHandler;
        GameEvents.OnToastRequested -= OnToastRequestedHandler;
        GameEvents.OnPlaySFX -= OnPlaySFXHandler;
        GameEvents.OnScreenShake -= OnScreenShakeHandler;
        GameEvents.OnInventoryChanged -= OnInventoryChangedHandler;
    }

    // ============ 按钮回调 ============

    void OnScanClicked()
    {
        manager.ScanSelectedItem();
    }

    void OnDecisionClicked(PawnDeskManager.DecisionType type)
    {
        string result = manager.ExecuteDecision(type);
        ShowToast(result, result.Contains("✓") ? "success" : "info", 3f);
    }

    void OnNextClientClicked()
    {
        manager.ProcessNextClient();
        RefreshClientItems();
    }

    // ============ Manager 事件处理 ============

    void OnDayStartedHandler()
    {
        RefreshTopBar();
        dayReportPanel.SetActive(false);
    }

    void OnClientArrivedHandler(ClientProfile client)
    {
        clientDialogueText.text = client.dialogueText;
        clientPanel.SetActive(true);

        RefreshClientInfo(client);
        RefreshClientItems();
        inspectionPanel.SetActive(false);
        ClearSelection();
    }

    // 显示客户意图与预算
    void RefreshClientInfo(ClientProfile client)
    {
        if (client == null) return;
        string typeLabel = client.TypeLabel;
        string info = $"[{typeLabel}]  预算: {client.budget:F0}";

        if (clientInfoText != null)
            clientInfoText.text = info;
        else if (clientDialogueText != null)
            clientDialogueText.text = info + "\n" + client.dialogueText;
    }

    void OnItemSelectedHandler(ItemInstance item)
    {
        inspectionPanel.SetActive(false);

        // 按归属与放置区域切换按钮：客户物品需放到柜台（已自动估价）才可收购
        bool isCustomerItem = item.owner == ItemInstance.ItemOwnership.Customer;
        bool onCounter = isCustomerItem && item.placement == ItemInstance.ItemPlacement.Counter;

        buyButton?.gameObject.SetActive(onCounter);
        sellButton?.gameObject.SetActive(!isCustomerItem);
        rejectButton?.gameObject.SetActive(true);
    }

    void OnScanCompletedHandler(InspectionReport report, float offer)
    {
        inspectionPanel.SetActive(true);

        if (report == null || !report.scanned)
        {
            // 未扫描：仅显示标准报价，提示可扫描检查特殊情况
            inspectionResultText.text = "<color=white>[标准报价]</color>\n未扫描，按普通物品估价。点击扫描可检查特殊情况。";
            inspectionResultText.color = Color.white;
        }
        else if (report.HasIssue)
        {
            inspectionResultText.text = "<color=red>[异常发现]</color>\n" + string.Join("\n", report.findings);
            inspectionResultText.color = Color.red;
            StartCoroutine(GlitchEffect());
        }
        else
        {
            inspectionResultText.text = "<color=green>[正常]</color>\n物品状态良好，无异常。";
            inspectionResultText.color = Color.green;
        }

        offerText.text = $"估价: <color=yellow>{offer:F0}</color> 信用点";
        offerText.gameObject.SetActive(true);

        // 启用决策按钮：客户物品需放到柜台（已估价）→收购；商栈库存→出售
        bool isCustomerItem = manager != null && manager.selectedItem != null
            && manager.selectedItem.owner == ItemInstance.ItemOwnership.Customer;
        bool onCounter = isCustomerItem && manager.selectedItem.placement == ItemInstance.ItemPlacement.Counter;
        buyButton?.gameObject.SetActive(onCounter);
        sellButton?.gameObject.SetActive(!isCustomerItem);
        rejectButton?.gameObject.SetActive(true);
    }

    void OnItemBoughtHandler(ItemInstance item)
    {
        RefreshTopBar();
        RefreshClientItems();   // 已收购物品移出客户清单，刷新窗口/柜台防止重复购买
        ShowToast($"已收购 {item.itemName}", "success", 2.5f);
    }

    void OnItemSoldHandler(ItemInstance item)
    {
        RefreshTopBar();
        ShowToast($"已售出 {item.itemName}", "success", 2.5f);
    }

    void OnDayEndedHandler()
    {
        ShowDayReport();
    }

    // ============ GameEvents 处理 ============

    void OnFundsChangedHandler(float newFunds, float delta, string reason)
    {
        fundsText.text = $"资金: {newFunds:F0}";
        if (delta < 0)
            StartCoroutine(FlashText(fundsText, Color.red, 0.5f));
        else if (delta > 0)
            StartCoroutine(FlashText(fundsText, Color.green, 0.5f));
    }

    void OnReputationChangedHandler(float newRep, float delta)
    {
        reputationText.text = $"声望: {newRep:F0}";
        reputationText.color = newRep >= 50 ? Color.white : Color.yellow;
    }

    void OnWantedChangedHandler(float newWanted, float delta)
    {
        wantedText.text = $"通缉: {newWanted:F0}";
        wantedText.color = newWanted > 30 ? Color.red : Color.white;
        if (delta > 0) StartCoroutine(FlashText(wantedText, Color.red, 0.5f));
    }

    void OnDayChangedHandler(int day)
    {
        dayText.text = $"第 {day} 天";
    }

    void OnScannerJammedHandler()
    {
        ShowToast("⚠ 扫描仪被干扰卡死！", "error", 3f);
        StartCoroutine(FlashPanel(inspectionPanel, Color.red, 0.5f));
    }

    void OnToastRequestedHandler(string message, string type, float duration)
    {
        ShowToast(message, type, duration);
    }

    void OnPlaySFXHandler(string sfxName, float volume)
    {
        AudioManager.Instance?.Play(sfxName, volume);
    }

    void OnScreenShakeHandler(float intensity, float duration)
    {
        StartCoroutine(ScreenShakeRoutine(intensity, duration));
    }

    // ============ 货架面板（卖流程：选择库存物品） ============

    void OpenShelf()
    {
        RefreshShelfItems();
        shelfPanel?.SetActive(true);
    }

    void CloseShelf()
    {
        shelfPanel?.SetActive(false);
    }

    void RefreshShelfItems()
    {
        foreach (var btn in shelfButtons)
            if (btn != null) Destroy(btn);
        shelfButtons.Clear();

        var items = manager != null ? manager.GetCounterItems() : null;
        int capacity = manager?.counterInventory != null ? manager.counterInventory.capacity : 0;

        if (items != null)
        {
            var prefab = itemPrefab;
            for (int i = 0; i < items.Count && prefab != null; i++)
            {
                int index = i;
                var item = items[i];
                var btnObj = Instantiate(prefab, shelfItemsContainer);
                var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = $"{item.itemName}（估价 {item.baseValue}）";
                var btn = btnObj.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => OnShelfItemClicked(index));
                shelfButtons.Add(btnObj);
            }
        }

        if (shelfCountText != null)
            shelfCountText.text = $"货架: {items?.Count ?? 0}/{capacity}";
    }

    void OnShelfItemClicked(int index)
    {
        manager.SelectStockItem(index);
        ShowToast("已选中待售物品，点击「卖」成交", "info", 1.5f);
        CloseShelf();
    }

    void OnInventoryChangedHandler(string inventoryID)
    {
        // 货架内容变化时刷新列表（收购入库/售出出库都会触发）
        if (manager != null && manager.counterInventory != null && inventoryID == manager.counterInventory.inventoryID)
            RefreshShelfItems();
        RefreshTopBar();
    }

    // ============ UI 更新方法 ============

    /// <summary>确保玩家面板存在并把声望/通缉文本移入其中（点开才显示）。场景已接线则直接使用。</summary>
    void EnsurePlayerPanel()
    {
        // 默认隐藏，点开才显示
        playerPanel.SetActive(false);
        playerButton.onClick.AddListener(TogglePlayerPanel);
    }

    /// <summary>点开/关闭玩家面板；打开时刷新数值。</summary>
    void TogglePlayerPanel()
    {
        if (playerPanel.activeSelf == true)
        {
            playerPanel.SetActive(false);
        }
        else playerPanel.SetActive(true);
    }

    void RefreshTopBar()
    {
        dayText.text = $"第 {manager.GetCurrentDay()} 天";
        fundsText.text = $"资金: {manager.GetShopFunds():F0}";
        reputationText.text = $"声望: {manager.GetReputation():F0}";
        wantedText.text = $"通缉: {manager.GetWantedLevel():F0}";
        spiritText.text = $"灵能: {manager.GetSpiritEnergy():F0}";
    }

    /// <summary>灵能变动：仅刷新灵能显示。</summary>
    void OnSpiritEnergyChangedHandler()
    {
        spiritText.text = $"灵能: {manager.GetSpiritEnergy():F0}";
    }

    // ============ 客户物品渲染（窗口 ⇄ 柜台） ============

    /// <summary>确保 窗口/柜台 两个放置区域存在，并注册拖拽回调。</summary>
    void EnsureContainers()
    {
        // 窗口：屏幕左侧自动生成竖列容器（按用户选择，不依赖场景旧容器）
        if (windowContainer == null)
            windowContainer = CreateWindowContainer();
        itemsContainer = windowContainer;

        // 柜台：场景中未指定则在物品面板旁自动创建；否则确保其带 DropZone
        if (counterContainer == null)
            counterContainer = CreateCounterContainer();

        SetupWindowContainerLayout();   // 确保窗口容器有纵向布局，按钮才能排布可见
        AddDropZone(itemsContainer, ItemInstance.ItemPlacement.Window);
        AddDropZone(counterContainer, ItemInstance.ItemPlacement.Counter);

        // 注册拖放回调：由 manager 决定去柜台 / 回窗口
        CounterDrag.RequestMove = (item, target) =>
        {
            if (manager == null || item == null) return false;
            if (target == ItemInstance.ItemPlacement.Counter)
                manager.PlaceItemOnCounter(item);
            else
                manager.MoveItemBackToWindow(item);
            return true;
        };
    }

    /// <summary>运行时在屏幕左侧自动创建窗口容器（与右侧柜台对称）。用户可在场景中自行接线更美观的布局。</summary>
    Transform CreateWindowContainer()
    {
        var parent = itemsPanel != null ? itemsPanel.transform.parent : transform;
        var go = new GameObject("WindowContainer", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(10f, 0f);
        rt.sizeDelta = new Vector2(240f, 0f);

        if (go.GetComponent<CanvasRenderer>() == null) go.AddComponent<CanvasRenderer>();
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.7f, 0.8f, 0.95f, 0.18f);

        var lg = go.AddComponent<VerticalLayoutGroup>();
        lg.padding = new RectOffset(6, 6, 6, 6);
        lg.spacing = 4f;
        lg.childForceExpandHeight = false;
        lg.childForceExpandWidth = true;
        lg.childControlWidth = true;
        lg.childControlHeight = false;

        // 标题
        var titleGo = new GameObject("WindowTitle", typeof(RectTransform));
        titleGo.transform.SetParent(go.transform, false);
        titleGo.AddComponent<CanvasRenderer>();
        var title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "窗口物品";
        title.fontSize = 22;
        title.color = Color.white;
        title.raycastTarget = false;
        var f = ResolveFont();
        if (f != null) title.font = f;

        // 提示
        var tipGo = new GameObject("WindowTip", typeof(RectTransform));
        tipGo.transform.SetParent(go.transform, false);
        tipGo.AddComponent<CanvasRenderer>();
        var tip = tipGo.AddComponent<TextMeshProUGUI>();
        tip.text = "顾客带来的物品\n拖到柜台估价";
        tip.fontSize = 16;
        tip.color = new Color(1f, 1f, 1f, 0.7f);
        tip.raycastTarget = false;
        if (f != null) tip.font = f;

        return go.transform;
    }

    /// <summary>借用场景里任意已有 TMP 文本的字体，确保代码创建的文本能正常渲染中文。</summary>
    TMP_FontAsset ResolveFont()
    {
        foreach (var t in FindObjectsOfType<TextMeshProUGUI>())
            if (t != null && t.font != null)
                return t.font;
        return null;
    }

    /// <summary>确保窗口容器有纵向布局，让物品按钮在可见区域内依次排列。</summary>
    void SetupWindowContainerLayout()
    {
        if (itemsContainer == null) return;

        if (itemsContainer.GetComponent<VerticalLayoutGroup>() == null)
        {
            var lg = itemsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            lg.padding = new RectOffset(4, 4, 4, 4);
            lg.spacing = 4f;
            lg.childControlWidth = true;
            lg.childForceExpandWidth = true;
            lg.childControlHeight = true;
            lg.childForceExpandHeight = false;
        }
    }

    /// <summary>按子物体偏好高度撑开窗口容器（ScrollRect 内容），保证按钮都落在可视区内。</summary>
    void FitWindowContainerHeight()
    {
        if (itemsContainer == null) return;
        var rt = itemsContainer as RectTransform;
        if (rt == null) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        float h = 0f;
        for (int i = 0; i < rt.childCount; i++)
        {
            var child = rt.GetChild(i) as RectTransform;
            if (child == null) continue;
            float childH = LayoutUtility.GetPreferredSize(child, 1); // 1 = 垂直轴
            if (childH <= 0f) childH = child.rect.height;            // 兜底：用实际矩形高度
            h += childH;
        }
        var lg = rt.GetComponent<VerticalLayoutGroup>();
        if (lg != null && rt.childCount > 1)
            h += lg.spacing * (rt.childCount - 1);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, Mathf.Max(h, 0f));
    }

    void AddDropZone(Transform zone, ItemInstance.ItemPlacement target)
    {        if (zone == null) return;

        // 确保有可接收 Drop 的射线图形（否则 OnDrop 不会触发）
        var img = zone.GetComponent<Image>();
        if (img == null)
        {
            if (zone.GetComponent<CanvasRenderer>() == null)
                zone.gameObject.AddComponent<CanvasRenderer>();
            img = zone.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.02f); // 近乎透明，仅用于接收射线
        }

        var dz = zone.GetComponent<ItemDropZone>();
        if (dz == null) dz = zone.gameObject.AddComponent<ItemDropZone>();
        dz.targetPlacement = target;
    }

    /// <summary>运行时自动创建一个柜台容器（右侧竖列）。用户可在场景中自行接线更美观的布局。</summary>
    Transform CreateCounterContainer()
    {
        var parent = itemsPanel != null ? itemsPanel.transform.parent : transform;
        var go = new GameObject("CounterContainer", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-10f, 0f);
        rt.sizeDelta = new Vector2(220f, 0f);

        if (go.GetComponent<CanvasRenderer>() == null) go.AddComponent<CanvasRenderer>();
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.85f, 0.75f, 0.55f, 0.18f);

        var lg = go.AddComponent<VerticalLayoutGroup>();
        lg.padding = new RectOffset(6, 6, 6, 6);
        lg.spacing = 4f;
        lg.childForceExpandHeight = false;
        lg.childForceExpandWidth = true;
        lg.childControlWidth = true;
        lg.childControlHeight = false;

        // 标题
        var titleGo = new GameObject("CounterTitle", typeof(RectTransform));
        titleGo.transform.SetParent(go.transform, false);
        titleGo.AddComponent<CanvasRenderer>();
        var title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "柜台估价";
        title.fontSize = 22;
        title.color = Color.yellow;
        title.raycastTarget = false;
        var f = ResolveFont();
        if (f != null) title.font = f;

        // 占位提示
        var tipGo = new GameObject("CounterTip", typeof(RectTransform));
        tipGo.transform.SetParent(go.transform, false);
        tipGo.AddComponent<CanvasRenderer>();
        var tip = tipGo.AddComponent<TextMeshProUGUI>();
        tip.text = "把客户物品拖到这里\n自动估价";
        tip.fontSize = 16;
        tip.color = new Color(1f, 1f, 1f, 0.7f);
        tip.raycastTarget = false;
        if (f != null) tip.font = f;

        return go.transform;
    }

    /// <summary>刷新客户窗口与柜台两个列表。</summary>
    void RefreshClientItems()
    {
        // 1) 销毁跟踪的旧按钮（含拖拽期间被临时提升到根 Canvas 的）
        foreach (var go in itemButtons)
            if (go != null) Destroy(go);
        itemButtons.Clear();

        // 2) 清空两个容器里的所有子物体，剔除场景遗留的模板按钮/未被跟踪的旧按钮；
        //    只保留运行时自动生成的标题与提示（按名字豁免）。
        ClearAllChildren(itemsContainer, "WindowTitle", "WindowTip");
        ClearAllChildren(counterContainer, "CounterTitle", "CounterTip");

        if (manager == null || manager.currentClient == null)
        {
            return;
        }

        // 窗口（未估价）
        BuildItemButtons(manager.GetClientWindowItems(), itemsContainer, false);

        // 柜台（自动估价）
        BuildItemButtons(manager.GetClientCounterItems(), counterContainer, true);

        FitWindowContainerHeight();   // 撑开窗口容器高度，确保按钮在可视区内

        if (counterCountText != null)
            counterCountText.text = $"柜台估价: {manager.GetClientCounterCount()}";
    }

    /// <summary>销毁容器的全部子物体，可豁免指定名字的子物体（保留柜台标题/提示）。</summary>
    void ClearAllChildren(Transform container, params string[] keepNames)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
            if (child == null) continue;
            bool keep = false;
            for (int k = 0; k < keepNames.Length; k++)
                if (child.name == keepNames[k]) { keep = true; break; }
            if (!keep) Destroy(child.gameObject);
        }
    }

    /// <summary>把一个列表渲染进容器；柜台物品在名称后附估价。</summary>
    void BuildItemButtons(List<ItemInstance> items, Transform container, bool showOffer)
    {
        if (container == null || itemPrefab == null)
        {
            Debug.LogWarning($"[拖拽] 无法建按钮: container空={container == null}, prefab空={itemPrefab == null}");
            return;
        }

        foreach (var item in items)
        {
            var btnObj = Instantiate(itemPrefab, container);
            if (!btnObj.activeSelf) btnObj.SetActive(true);   // 模板源被隐藏，克隆体需激活
            var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = showOffer ? $"{item.itemName}  估价:{item.appraisedOffer:F0}" : item.itemName;

            var btn = btnObj.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => manager.SelectItem(item));

            var drag = btnObj.GetComponent<DraggableItemButton>();
            if (drag == null)
            {
                drag = btnObj.AddComponent<DraggableItemButton>();
            }
            drag.item = item;

            itemButtons.Add(btnObj);
        }
    }

    void OnClientItemPlacementChangedHandler()
    {
        // 延迟到帧末刷新，避免拖拽结束回调作用在被销毁的按钮上
        if (refreshCoroutine != null) StopCoroutine(refreshCoroutine);
        refreshCoroutine = StartCoroutine(DeferredRefresh());
    }

    IEnumerator DeferredRefresh()
    {
        yield return null;
        RefreshClientItems();
    }

    void ClearSelection()
    {
        // 客户无待售物品时给引导：只买/既买又卖 → 采购引导；其余 → 普通占位
        var client = manager?.currentClient;
    }

    void ShowDayReport()
    {
        int stockCount = manager.GetCounterCount();
        dayReportText.text = $"=== 日结算 ===\n" +
            $"天数: 第 {manager.GetCurrentDay()} 天\n" +
            $"资金: {manager.GetShopFunds():F0}\n" +
            $"声望: {manager.GetReputation():F0}\n" +
            $"通缉值: {manager.GetWantedLevel():F0}\n" +
            $"货架物品: {stockCount}\n" +
            $"能量: 已恢复";
        dayReportPanel.SetActive(true);
    }

    void HideAllPanels()
    {
        dayReportPanel?.SetActive(false);
        toastPanel?.SetActive(false);
        shelfPanel?.SetActive(false);
    }

    // ============ Toast ============

    void ShowToast(string message, string type, float duration)
    {
        toastPanel.SetActive(true);
        toastText.text = message;

        switch (type)
        {
            case "success": toastText.color = Color.green; break;
            case "warning": toastText.color = Color.yellow; break;
            case "error": toastText.color = Color.red; break;
            default: toastText.color = Color.white; break;
        }

        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        toastCoroutine = StartCoroutine(HideToastRoutine(duration));
    }

    IEnumerator HideToastRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        toastPanel.SetActive(false);
    }

    // ============ 视觉特效协程 ============

    IEnumerator FlashText(TextMeshProUGUI target, Color flashColor, float duration)
    {
        Color original = target.color;
        target.color = flashColor;
        yield return new WaitForSeconds(duration);
        target.color = original;
    }

    IEnumerator FlashPanel(GameObject panel, Color flashColor, float duration)
    {
        if (panel == null) yield break;
        var img = panel.GetComponent<Image>();
        if (img == null) yield break;
        Color original = img.color;
        img.color = flashColor;
        yield return new WaitForSeconds(duration);
        img.color = original;
    }

    IEnumerator GlitchEffect()
    {
        if (scanGlitchEffect == null) yield break;
        scanGlitchEffect.SetActive(true);
        yield return new WaitForSeconds(0.3f);
        scanGlitchEffect.SetActive(false);
        yield return new WaitForSeconds(0.1f);
        scanGlitchEffect.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        scanGlitchEffect.SetActive(false);
    }

    IEnumerator ScreenShakeRoutine(float intensity, float duration)
    {
        Transform canvas = GetComponentInParent<Canvas>()?.transform;
        if (canvas == null) yield break;
        Vector3 originalPos = canvas.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-intensity * 20f, intensity * 20f);
            float y = Random.Range(-intensity * 20f, intensity * 20f);
            canvas.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvas.localPosition = originalPos;
    }
}