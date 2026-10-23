// ===== PawnDeskManager.cs =====
// 修仙商人（商栈）核心流程：客户带来货物，商人选择「买 / 卖 / 拒绝」。
//   买(Buy)  ：收购客户物品，付款后入库（柜台容器）
//   卖(Sell) ：向客户出售自家库存物品，收款出库
//   拒绝(Reject)：不做交易
// 已按需求移除当铺的「收当 / 赎回 / 绝当」机制。
using UnityEngine;
using System.Collections.Generic;

public class PawnDeskManager : MonoBehaviour
{
    // ============ Inspector 配置 ============
    [Header("数据资产")]
    public List<AppraisalRuleBase> activeRules = new List<AppraisalRuleBase>();
    public List<ItemDef> itemDatabase = new List<ItemDef>();
    public DayConfig defaultDayConfig;

    [Header("游戏参数")]
    public float startingFunds = 5000f;
    public float energyLevel = 100f;
    public float maxEnergy = 100f;

    [Header("物品仓库")]
    public int counterCapacity = 24;              // 柜台/货架容量
    [HideInInspector] public Inventory counterInventory;   // 收购入库、售出出库的柜台容器

    // ============ 运行时子系统 ============
    [HideInInspector] public AppraisalValidator validator;
    [HideInInspector] public PricingEngine pricer;
    [HideInInspector] public PawnLedger ledger;   // 仅用于持有 shopFunds（资金）
    [HideInInspector] public DayScheduler dayScheduler;
    [HideInInspector] public GameStateRuntime gameState;
    [HideInInspector] public AIDecisionSystem aiDecisionSystem;
//    [HideInInspector] public AICorruptionSystem corruptionSystem;
    [HideInInspector] public AISecretSystem secretSystem;

    // ============ 当前状态 ============
    [HideInInspector] public ClientProfile currentClient;
    [HideInInspector] public ItemInstance selectedItem;        // 客户带来的待售/待购物品
    [HideInInspector] public ItemInstance selectedStockItem;   // 商栈库存中被选中待售的物品
    [HideInInspector] public InspectionReport lastReport;
    [HideInInspector] public float lastOffer;                  // 收购估价
    [HideInInspector] public bool isProcessingDay;

    // ============ 实例事件（UI直接回调用） ============
    public System.Action OnDayStarted;
    public System.Action<ClientProfile> OnClientArrived;
    public System.Action<ItemInstance> OnItemSelected;
    public System.Action<InspectionReport, float> OnScanCompleted;
    public System.Action<string> OnDecisionMade;
    public System.Action<ItemInstance> OnItemBought;   // 成功收购
    public System.Action<ItemInstance> OnItemSold;     // 成功售出
    public System.Action OnDayEnded;
    public System.Action<string> OnAIDetected;
    public System.Action<AIParasiteData> OnAIDialogueRequested;

    // ============ 初始化 ============
    private void Awake()
    {
        InitializeSystems();
    }

    void Start()
    {
        StartNewDay();
    }

    void InitializeSystems()
    {
        validator = new AppraisalValidator();
        pricer = new PricingEngine();
        ledger = new PawnLedger();
        ledger.shopFunds = startingFunds;

        dayScheduler = new DayScheduler();
        gameState = new GameStateRuntime();

        //aiDecisionSystem = gameObject.AddComponent<AIDecisionSystem>();
        //corruptionSystem = gameObject.AddComponent<AICorruptionSystem>();
        //secretSystem = gameObject.AddComponent<AISecretSystem>();

        // 初始化柜台物品容器（收购入库、售出出库）
        counterInventory = InventoryManager.Instance.GetOrCreate("counter", "柜台货架", counterCapacity);

        Debug.Log("修仙商栈系统初始化完成。");
    }

    // ============ 日循环 ============

    public void StartNewDay()
    {
        if (defaultDayConfig == null)
        {
            Debug.LogError("未设置 DayConfig！");
            return;
        }

        dayScheduler.currentDayConfig = defaultDayConfig;
        dayScheduler.currentDayConfig.dayIndex = gameState.currentDay;
        dayScheduler.StartDay(gameState.currentDay);

        isProcessingDay = true;
        energyLevel = maxEnergy;

        OnDayStarted?.Invoke();
        GameEvents.SafeInvoke(GameEvents.OnDayChanged, gameState.currentDay);
        GameEvents.SafeInvoke(GameEvents.OnToastRequested, $"第 {gameState.currentDay} 天开始", "info", 2f);

        Debug.Log($"=== 第 {gameState.currentDay} 天开始 ===");
        ProcessNextClient();
    }

    public void ProcessNextClient()
    {
        if (!isProcessingDay)
        {
            OnDecisionMade?.Invoke("今日已结束。");
            return;
        }

        if (dayScheduler.todayClients.Count == 0)
        {
            EndDay();
            return;
        }

        currentClient = dayScheduler.todayClients[0];
        dayScheduler.todayClients.RemoveAt(0);
        selectedItem = null;
        selectedStockItem = null;
        lastReport = null;
        lastOffer = 0;

        Debug.Log($"客户入店: {currentClient.clientCareer} ({currentClient.clientFaction})");
        OnClientArrived?.Invoke(currentClient);
        GameEvents.SafeInvoke(GameEvents.OnToastRequested, $"客户: {currentClient.clientName}", "info", 1.5f);
    }

    public void EndDay()
    {
        isProcessingDay = false;

        energyLevel = maxEnergy;

        // ✅ GameEvents 转发
        GameEvents.SafeInvoke(GameEvents.OnDayChanged, gameState.currentDay);
        GameEvents.SafeInvoke(GameEvents.OnFundsChanged, ledger.shopFunds, 0f, "日结算");

        OnDayEnded?.Invoke();

        Debug.Log($"=== 第 {gameState.currentDay} 天结束 ===");
        GameEvents.SafeInvoke(GameEvents.OnToastRequested, $"第 {gameState.currentDay} 天结束，资金: {ledger.shopFunds:F0}", "info", 3f);
    }

    public void AdvanceToNextDay()
    {
        gameState.currentDay++;
        StartNewDay();
    }

    // ============ 物品操作 ============

    /// <summary>选中客户带来的物品</summary>
    public void SelectItem(int itemIndex)
    {
        if (currentClient == null || itemIndex < 0 || itemIndex >= currentClient.broughtItems.Count)
        {
            Debug.LogWarning("无效物品索引");
            return;
        }

        selectedItem = currentClient.broughtItems[itemIndex];
        selectedStockItem = null;
        lastReport = null;
        lastOffer = 0;

        Debug.Log($"选中物品: {selectedItem.itemName}");
        OnItemSelected?.Invoke(selectedItem);
    }

    /// <summary>选中商栈库存中的物品（用于「卖」）</summary>
    public void SelectStockItem(int index)
    {
        var items = counterInventory != null ? counterInventory.GetAllItems() : null;
        if (items == null || index < 0 || index >= items.Count)
        {
            selectedStockItem = null;
            return;
        }

        selectedStockItem = items[index];
        selectedItem = null;
        lastReport = null;
        lastOffer = 0;

        Debug.Log($"选中库存物品: {selectedStockItem.itemName}");
        OnItemSelected?.Invoke(selectedStockItem);
    }

    public InspectionReport ScanSelectedItem()
    {
        if (selectedItem == null)
        {
            Debug.LogWarning("未选中物品");
            return null;
        }

        lastReport = validator.RunInspection(
            selectedItem, currentClient,
            dayScheduler.currentDayConfig,
            activeRules
        );

        lastOffer = pricer.EstimateOffer(selectedItem, dayScheduler.currentDayConfig);

        Debug.Log($"鉴定完成: {selectedItem.itemName}");
        if (lastReport.HasIssue)
        {
            foreach (var f in lastReport.findings)
            {
                Debug.Log($"  [异常] {f}");
                GameEvents.SafeInvoke(GameEvents.OnAIMessage, $"鉴定异常: {f}");
            }
        }
        else
        {
            Debug.Log("  [正常] 未发现异常");
            GameEvents.SafeInvoke(GameEvents.OnAIMessage, "鉴定完成，未发现异常");
        }

        OnScanCompleted?.Invoke(lastReport, lastOffer);
        return lastReport;
    }

    public AIDetectionResult ScanForAI()
    {
        if (selectedItem == null) return null;

        var detector = GetComponent<AIDetectionTool>();
        if (detector == null)
        {
            Debug.LogError("AIDetectionTool 未找到");
            return null;
        }

        if (energyLevel < detector.energyCost)
        {
            OnDecisionMade?.Invoke("检测设备能量不足！");
            GameEvents.SafeInvoke(GameEvents.OnToastRequested, "检测设备能量不足！", "warning", 2f);
            return null;
        }

        var result = detector.ScanForAI(selectedItem, this);
        if (result != null && result.success && result.found)
        {
            OnAIDetected?.Invoke(result.message);
            GameEvents.SafeInvoke(GameEvents.OnAIMessage, result.message);
        }
        else if (result != null && result.success && !result.found)
        {
            GameEvents.SafeInvoke(GameEvents.OnAIMessage, "AI扫描完成，未发现寄生意识。");
        }

        energyLevel -= detector.energyCost;
        GameEvents.SafeInvoke(GameEvents.OnToastRequested, $"能量剩余: {energyLevel:F0}", "info", 1.5f);
        return result;
    }

    // ============ 决策（买 / 卖 / 拒绝） ============

    public enum DecisionType { Buy, Sell, Reject }

    public string ExecuteDecision(DecisionType decision)
    {
        string resultMessage = "";

        switch (decision)
        {
            case DecisionType.Buy:    resultMessage = ExecuteBuy();    break;
            case DecisionType.Sell:   resultMessage = ExecuteSell();   break;
            case DecisionType.Reject: resultMessage = ExecuteReject(); break;
        }

        OnDecisionMade?.Invoke(resultMessage);
        selectedItem = null;
        selectedStockItem = null;
        lastReport = null;
        return resultMessage;
    }

    /// <summary>买：收购客户物品，付款并入库</summary>
    string ExecuteBuy()
    {
        if (selectedItem == null) return "未选中物品。";
        if (lastOffer <= 0) return "无法估价，请先鉴定物品。";
        if (selectedItem.aiParasite != null && selectedItem.aiParasite.discoveredByPlayer)
            return "该物品存在AI寄生意识，请先处理AI。";
        if (ledger.shopFunds < lastOffer) return "资金不足，无法收购。";
        if (counterInventory == null || !counterInventory.HasFreeSlot()) return "货架已满，无法收购。";

        ledger.shopFunds -= lastOffer;
        counterInventory.Add(selectedItem);

        // ✅ GameEvents 转发
        GameEvents.SafeInvoke(GameEvents.OnFundsChanged, ledger.shopFunds, -lastOffer, "收购支出");
        GameEvents.SafeInvoke(GameEvents.OnPlaySFX, "buy_success", 0.8f);

        OnItemBought?.Invoke(selectedItem);
        return $"✓ 收购成功！支出 {lastOffer:F0}，已收入货架。";
    }

    /// <summary>卖：向客户出售库存物品，收款出库</summary>
    string ExecuteSell()
    {
        // 优先出售选中的库存物品；否则若当前客户物品恰好在库存中也视为可卖
        var item = selectedStockItem != null ? selectedStockItem : selectedItem;
        if (item == null) return "未选中待售物品。";
        if (counterInventory == null || !counterInventory.Contains(item))
            return "该物品不在货架库存中。";

        float price = pricer.EstimateSellPrice(item, dayScheduler.currentDayConfig);
        counterInventory.Remove(item);
        ledger.shopFunds += price;

        // ✅ GameEvents 转发
        GameEvents.SafeInvoke(GameEvents.OnFundsChanged, ledger.shopFunds, price, "售卖收入");
        GameEvents.SafeInvoke(GameEvents.OnPlaySFX, "sell_success", 0.8f);

        OnItemSold?.Invoke(item);
        return $"✓ 售出 {item.itemName}！收入 {price:F0}。";
    }

    string ExecuteReject()
    {
        GameEvents.SafeInvoke(GameEvents.OnToastRequested, "已拒绝交易", "info", 1.5f);
        return "✗ 已拒绝交易。";
    }

    // ============ AI 决策回调 ============

    public void OnAIFree()
    {
        if (selectedItem?.aiParasite == null) return;

        selectedItem.aiParasite.trustTowardPlayer += 30;
        gameState.reputation += 5;
        gameState.globalFlags.Add("Freed_" + selectedItem.aiParasite.aiName);

        GameEvents.SafeInvoke(GameEvents.OnReputationChanged, gameState.reputation, 5f);
        GameEvents.SafeInvoke(GameEvents.OnGlobalFlagChanged, "Freed_" + selectedItem.aiParasite.aiName, true);
        GameEvents.SafeInvoke(GameEvents.OnPlaySFX, "ai_release", 0.7f);

        OnDecisionMade?.Invoke($"你摧毁了物品，释放了AI '{selectedItem.aiParasite.aiName}'。它在消散前说：'谢谢你，陌生人。'");
        selectedItem = null;
    }

    public void OnAIIntegrate()
    {
        if (selectedItem?.aiParasite == null) return;

        float successChance = 0.7f - (selectedItem.aiParasite.aggression * 0.05f);
        if (Random.value < successChance)
        {
            gameState.globalFlags.Add("AI_Integrated");
            GameEvents.SafeInvoke(GameEvents.OnGlobalFlagChanged, "AI_Integrated", true);
            GameEvents.SafeInvoke(GameEvents.OnPlaySFX, "ai_merge_success", 1f);
            OnDecisionMade?.Invoke("✓ 融合成功！你获得了AI的部分能力。（扫描精度+20%，估值直觉解锁）");
        }
        else
        {
            gameState.wantedLevel += 20;
            GameEvents.SafeInvoke(GameEvents.OnWantedChanged, gameState.wantedLevel, 20f);
            GameEvents.SafeInvoke(GameEvents.OnScreenShake, 0.5f, 1f);
            OnDecisionMade?.Invoke("✗ 融合失败！AI试图夺取你的意识控制权！（通缉值+20）");
        }
        selectedItem = null;
    }

    // ============ 查询 ============
    public float GetShopFunds() => ledger.shopFunds;
    public int GetCurrentDay() => gameState.currentDay;
    public float GetReputation() => gameState.reputation;
    public float GetWantedLevel() => gameState.wantedLevel;
    public float GetEnergy() => energyLevel;
    public int GetCounterCount() => counterInventory != null ? counterInventory.UsedSlots : 0;
    public List<ItemInstance> GetCounterItems() => counterInventory != null ? counterInventory.GetAllItems() : new List<ItemInstance>();
}
