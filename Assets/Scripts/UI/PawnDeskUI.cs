// ===== PawnDeskUI.cs =====
// 挂载到 UI Canvas 或专门的 UIManager 物体上
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public Button playerPanelCloseButton; // 角色面板右上角 X 关闭按钮
    public TextMeshProUGUI reputationText;
    public TextMeshProUGUI wantedText;

    [Header("客户面板")]
    public GameObject clientPanel;
    public TextMeshProUGUI clientFactionText;
    public TextMeshProUGUI clientDialogueText;
    public TextMeshProUGUI clientInfoText;   // 可选：显示客户类型与预算（为空时追加到对白）
    public Image clientPortrait;


    [Header("物品面板")]
    //public GameObject itemsPanel;
    public Transform itemsContainer;      // 「窗口」：客户物品初到时的落位（未估价）
    public GameObject itemButtonPrefab;  // 含 Button + Text + Image

    public Color emptySlotColor = new Color(0.08f, 0.08f, 0.12f, 0.35f); // 空槽底色（网格参数由场景 GridLayoutGroup 控制）

    public const int CounterSlotCount = 10; // 柜台固定格子数（窗口格子数随客户物品数量）

    [Header("初始物品放置窗口")]
    public GameObject windowPanel;
    public Transform windowContainer;     // 窗口容器 = 场景 WindowPanel 的 transform（未挂接时由 windowPanel 兜底）

    [Header("柜台估价面板（客户物品自动估价）")]
    public GameObject counterPanel;
    public Transform counterContainer;     // 「柜台」：场景 CounterPanel 的 transform；未挂接时由 counterPanel 兜底
    public TextMeshProUGUI counterCountText;   // 可选：显示 柜台估价物品数

    [Header("货架面板")]
    public GameObject shelfPanel;
    public Transform shelfItemsContainer;
    //public TextMeshProUGUI shelfCountText;     // 可选，显示 货架占用/容量

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
    List<GameObject> shelfButtons = new List<GameObject>();
    Coroutine toastCoroutine;
    Coroutine refreshCoroutine;

    // 客户到店时的物品快照：作为固定槽位基准（下标即槽位号）。
    // 移动/收购不改变槽位 → 物品只更新对应槽内容，其余槽原地不动（不补位）。
    List<ItemInstance> clientSlots = new List<ItemInstance>();

    // ============ 生命周期 ============
    void Start()
    {
        manager = FindObjectOfType<PawnDeskManager>();

        BindButtons();
        SubscribeEvents();
        EnsurePlayerPanel();   // 玩家面板：把声望/通缉移入默认隐藏的面板，点开才显示
        RefreshTopBar();
        HideAllPanels();
        EnsureContainers();    // 确保 窗口/柜台 两个放置区域与拖拽回调
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
        // 客户到店：按携带物品数快照固定槽位（后续移动/收购不改变槽位数）
        clientSlots.Clear();
        if (client != null) clientSlots.AddRange(client.broughtItems);

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
    void RefreshShelfItems()
    {
        foreach (var btn in shelfButtons)
            if (btn != null) Destroy(btn);
        shelfButtons.Clear();

        var items = manager != null ? manager.GetCounterItems() : null;
        int capacity = manager?.counterInventory != null ? manager.counterInventory.capacity : 0;

        if (items != null)
        {
            var prefab = itemButtonPrefab;
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

                // 玩家(Merchant)库存物品同样可拖拽，归属校验只允许其落到 柜台/货架。
                var drag = btnObj.GetComponent<DraggableItemButton>();
                if (drag == null) drag = btnObj.AddComponent<DraggableItemButton>();
                drag.item = item;

                shelfButtons.Add(btnObj);
            }
        }
    }

    void OnShelfItemClicked(int index)
    {
        manager.SelectStockItem(index);
        ShowToast("已选中待售物品，点击「卖」成交", "info", 1.5f);
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
        // 右上角 X 关闭按钮
        if (playerPanelCloseButton != null)
            playerPanelCloseButton.onClick.AddListener(ClosePlayerPanel);
    }

    /// <summary>X 关闭按钮：直接关闭角色面板。</summary>
    void ClosePlayerPanel()
    {
        playerPanel.SetActive(false);
    }

    /// <summary>点开/关闭玩家面板；打开时刷新数值。</summary>
    void TogglePlayerPanel()
    {
        playerPanel.SetActive(true);
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

    /// <summary>窗口/柜台使用场景已挂接的面板，不代码创建容器；注册拖拽回调。</summary>
    void EnsureContainers()
    {
        // 优先用显式挂接的 windowContainer / counterContainer，其次用 windowPanel / counterPanel 面板。
        Transform windowT = windowContainer != null ? windowContainer
                          : (windowPanel != null ? windowPanel.transform : null);
        Transform counterT = counterContainer != null ? counterContainer
                          : (counterPanel != null ? counterPanel.transform : null);
        Transform shelfT = shelfItemsContainer != null ? shelfItemsContainer
                         : (shelfPanel != null ? shelfPanel.transform : null);

        // 统一窗口/柜台容器引用（RefreshClientItems 使用 itemsContainer/counterContainer）
        itemsContainer = windowT;
        counterContainer = counterT;

        if (itemsContainer == null || counterContainer == null)
        {
            Debug.LogWarning("[PawnDeskUI] windowContainer/counterContainer（或 windowPanel/counterPanel）未在场景挂接，客户物品将无法显示。");
            return;
        }

        AddDropZone(itemsContainer, ItemInstance.ItemPlacement.Window);
        AddDropZone(counterContainer, ItemInstance.ItemPlacement.Counter);
        if (shelfT != null)
            AddDropZone(shelfT, ItemInstance.ItemPlacement.Shelf);

        // 注册拖放回调：先按「归属 × 面板」做校验。
        //   玩家(Merchant)物品只能落 柜台/货架；
        //   客户(Customer)物品只能落 窗口/柜台；
        //   落点在三块面板之外、或面板与归属不符时直接拒绝 → 物品回到拖动前位置（数据不变）。
        CounterDrag.RequestMove = (item, target) =>
        {
            if (manager == null || item == null) return false;
            if (!CanDropTo(item, target))
            {
                GameEvents.SafeInvoke(GameEvents.OnToastRequested, "这里放不下该物品", "warning", 1.5f);
                return false;
            }

            // 归属面板校验通过后执行对应移动：
            //   客户物品：柜台=放上柜台自动估价；窗口=放回窗口撤销估价；
            //   玩家物品：柜台/货架均为其归属面板，库存本就位于货架，保持原位即可（无需改 placement）。
            if (item.owner == ItemInstance.ItemOwnership.Customer)
            {
                if (target == ItemInstance.ItemPlacement.Counter)
                    manager.PlaceItemOnCounter(item);
                else if (target == ItemInstance.ItemPlacement.Window)
                    manager.MoveItemBackToWindow(item);
            }
            return true;
        };

        // 按格落点（柜台格子）：仅客户物品可放入柜台指定格（拖到哪个格进哪个格）
        CounterDrag.RequestMoveToCell = (item, slot) =>
        {
            if (manager == null || item == null) return false;
            if (item.owner != ItemInstance.ItemOwnership.Customer) return false;   // 仅客户物品可入柜台格
            if (!CanDropTo(item, ItemInstance.ItemPlacement.Counter))
            {
                GameEvents.SafeInvoke(GameEvents.OnToastRequested, "这里放不下该物品", "warning", 1.5f);
                return false;
            }
            return manager.PlaceItemOnCounterAt(item, slot);
        };
    }

    /// <summary>归属 × 面板 校验：判定某归属的物品是否允许落到 target 面板。</summary>
    bool CanDropTo(ItemInstance item, ItemInstance.ItemPlacement target)
    {
        switch (item.owner)
        {
            case ItemInstance.ItemOwnership.Merchant:
                return target == ItemInstance.ItemPlacement.Counter
                    || target == ItemInstance.ItemPlacement.Shelf;
            case ItemInstance.ItemOwnership.Customer:
                return target == ItemInstance.ItemPlacement.Window
                    || target == ItemInstance.ItemPlacement.Counter;
            default:
                return false;   // 无归属物品默认不可放置
        }
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

    /// <summary>刷新客户窗口与柜台两个网格（固定槽位、按原始下标填充，移动后不补位）。</summary>
    void RefreshClientItems()
    {
        // 清空两个网格容器（连同上次的格子与内容一并销毁）
        ClearAllChildren(itemsContainer);
        ClearAllChildren(counterContainer);

        if (manager == null || manager.currentClient == null)
        {
            return;
        }

        // 窗口网格：格子数随客户带来的物品数量
        BuildGrid(itemsContainer, ItemInstance.ItemPlacement.Window, clientSlots.Count);

        // 柜台网格：固定 CounterSlotCount 格，只填已拖到「柜台」的物品
        BuildGrid(counterContainer, ItemInstance.ItemPlacement.Counter, CounterSlotCount);

        if (counterCountText != null)
            counterCountText.text = $"柜台估价: {manager.GetClientCounterCount()}";
    }

    /// <summary>销毁容器的全部子物体。</summary>
    void ClearAllChildren(Transform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
            if (child != null) Destroy(child.gameObject);
        }
    }

    /// <summary>在容器里建固定数量的格子，按归属/格位填充。窗口=客户物品数(按原始下标)；柜台=固定槽位数(按 counterSlot，拖到哪个格进哪个格)。</summary>
    void BuildGrid(Transform panel, ItemInstance.ItemPlacement target, int cellCount)
    {
        if (panel == null) return;

        var own = manager.currentClient.broughtItems;
        bool showOffer = target == ItemInstance.ItemPlacement.Counter;

        // 柜台：固定格数，每格按 counterSlot 填充，并各自成为可落点（拖到哪个格进哪个格）
        if (target == ItemInstance.ItemPlacement.Counter)
        {
            for (int k = 0; k < cellCount; k++)
            {
                GameObject cell = CreateSlotCell(panel, k, raycastable: true, cellIndex: k);
                var item = own.FirstOrDefault(i =>
                    i.placement == ItemInstance.ItemPlacement.Counter && i.counterSlot == k);
                if (item != null)
                    FillSlot(cell.transform, item, showOffer);
            }
            return;
        }

        // 窗口：格子数随客户物品，按原始下标填充（不补位）
        for (int i = 0; i < cellCount; i++)
        {
            GameObject cell = CreateSlotCell(panel, i);
            var item = (i < clientSlots.Count) ? clientSlots[i] : null;
            bool filled = item != null && own.Contains(item) && item.placement == target;
            if (filled)
                FillSlot(cell.transform, item, showOffer);
        }
    }

    /// <summary>创建一个槽位底（Image）。raycastable=true 时格子可接收按格拖放（柜台）；否则让拖拽射线穿过落到面板。</summary>
    GameObject CreateSlotCell(Transform parent, int index, bool raycastable = false, int cellIndex = -1)
    {
        var go = new GameObject($"Slot_{index}", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = emptySlotColor;
        if (raycastable)
        {
            img.raycastTarget = true;   // 柜台格子：接收拖放，按格位落子
            var dz = go.AddComponent<ItemDropZone>();
            dz.targetPlacement = ItemInstance.ItemPlacement.Counter;
            dz.cellIndex = cellIndex;
        }
        else
        {
            img.raycastTarget = false;   // 窗口格子：让拖拽射线穿过，落到面板的 ItemDropZone
        }
        return go;
    }

    /// <summary>把物品按钮实例化进该格，并拉伸填满格子；优先走 ItemPrefab.Init。</summary>
    void FillSlot(Transform cell, ItemInstance item, bool showOffer)
    {
        if (cell == null || itemButtonPrefab == null)
        {
            if (itemButtonPrefab == null)
                Debug.LogWarning("[物品] itemButtonPrefab 未挂接，无法生成物品按钮。");
            return;
        }

        var content = Instantiate(itemButtonPrefab, cell);
        if (!content.activeSelf) content.SetActive(true);

        // 拉伸填满格子（网格 cell 尺寸由 GridLayoutGroup 决定）
        var rt = content.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        var itemPfb = content.GetComponent<ItemPrefab>();
        if (itemPfb != null)
        {
            itemPfb.Init(item, showOffer);
        }
        else
        {
            // 兜底：未挂 ItemPrefab 时，沿用“文本 + Button”旧逻辑
            var btnText = content.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = showOffer ? $"{item.itemName}  估价:{item.appraisedOffer:F0}" : item.itemName;
            var btn = content.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => manager.SelectItem(item));
        }

        var drag = content.GetComponent<DraggableItemButton>();
        if (drag == null)
        {
            drag = content.AddComponent<DraggableItemButton>();
        }
        drag.item = item;
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
        // 货架改为常驻显示：不再隐藏，玩家库存物品可直接查看与拖拽
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