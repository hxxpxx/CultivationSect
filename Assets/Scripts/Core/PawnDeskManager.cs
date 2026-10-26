// ===== PawnDeskManager.cs =====
// 修仙商人经营：买（收购客户物品）/ 卖（售出货架物品）/ 拒绝
// 已移除当铺遗留：PawnLedger / PawnTicket / RedemptionScheduler / 收当/赎当/绝当/黑市/报警/谈判
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    public int counterCapacity = 24;          // 柜台货架格子数

    // ============ 运行时子系统 ============
    [HideInInspector] public AppraisalValidator validator;
    [HideInInspector] public PricingEngine pricer;
    [HideInInspector] public DayScheduler dayScheduler;
    [HideInInspector] public GameStateRuntime gameState;

    // ============ 资金与货架（取代原 PawnLedger） ============
    [HideInInspector] public float shopFunds;               // 商栈资金
    [HideInInspector] public Inventory counterInventory;    // 柜台货架（收购入库/售出出库）

    // ============ 当前状态 ============
    [HideInInspector] public ClientProfile currentClient;
    [HideInInspector] public ItemInstance selectedItem;       // 客户携带、待收购的物品
    [HideInInspector] public ItemInstance selectedStockItem;  // 货架上待售的物品
    [HideInInspector] public InspectionReport lastReport;
    [HideInInspector] public float lastOffer;                 // 收购估价
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
        shopFunds = startingFunds;

        dayScheduler = new DayScheduler();
        gameState = new GameStateRuntime();


        // 初始化柜台货架容器（收购入库、售出出库）
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

        GameEvents.SafeInvoke(GameEvents.OnDayChanged, gameState.currentDay);
        GameEvents.SafeInvoke(GameEvents.OnFundsChanged, shopFunds, 0f, "日结算");

        OnDayEnded?.Invoke();

        Debug.Log($"=== 第 {gameState.currentDay} 天结束 ===");
        GameEvents.SafeInvoke(GameEvents.OnToastRequested, $"第 {gameState.currentDay} 天结束，资金: {shopFunds:F0}", "info", 3f);
    }

    public void AdvanceToNextDay()
    {
        gameState.currentDay++;
        StartNewDay();
    }

    // ============ 物品操作 ============

    /// <summary>选中客户携带的物品（用于收购）</summary>
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

    /// <summary>选中货架上的物品（用于售出）</summary>
    public void SelectStockItem(int index)
    {
        var items = GetCounterItems();
        if (index < 0 || index >= items.Count)
        {
            Debug.LogWarning("无效货架索引");
            return;
        }

        selectedStockItem = items[index];
        selectedItem = null;
        lastReport = null;
        lastOffer = 0;

        Debug.Log($"选中待售物品: {selectedStockItem.itemName}");
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

        Debug.Log($"扫描完成: {selectedItem.itemName}");
        if (lastReport.HasIssue)
        {
            foreach (var f in lastReport.findings)
            {
                Debug.Log($"  [异常] {f}");
                GameEvents.SafeInvoke(GameEvents.OnAIMessage, $"扫描异常: {f}");
            }
        }
        else
        {
            Debug.Log("  [正常] 未发现异常");
            GameEvents.SafeInvoke(GameEvents.OnAIMessage, "扫描完成，未发现异常");
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
        string resultMessage;
        switch (decision)
        {
            case DecisionType.Buy:  resultMessage = ExecuteBuy();   break;
            case DecisionType.Sell: resultMessage = ExecuteSell();  break;
            default:                resultMessage = ExecuteReject(); break;
        }

        OnDecisionMade?.Invoke(resultMessage);
        return resultMessage;
    }

    /// <summary>收购客户物品：付款并放入柜台货架</summary>
    string ExecuteBuy()
    {
        if (selectedItem == null) return "未选中物品，无法收购。";
        if (selectedItem.owner != ItemInstance.ItemOwnership.Customer) return "该物品不属于客户，无法收购。";
        if (lastOffer <= 0) return "无法估价，请先扫描物品。";
        if (shopFunds < lastOffer) return "资金不足，无法收购。";
        if (counterInventory == null || counterInventory.IsFull) return "货架已满，无法收购。";

        float cost = lastOffer;
        shopFunds -= cost;
        selectedItem.owner = ItemInstance.ItemOwnership.Merchant;

        if (!counterInventory.Add(selectedItem))
        {
            shopFunds += cost; // 入库失败回滚
            return "货架已满，无法收购。";
        }
        currentClient?.broughtItems.Remove(selectedItem);

        GameEvents.SafeInvoke(GameEvents.OnFundsChanged, shopFunds, -cost, "收购");
        GameEvents.SafeInvoke(GameEvents.OnPlaySFX, "buy_success", 0.8f);

        var bought = selectedItem;
        selectedItem = null;
        lastReport = null;
        lastOffer = 0;

        OnItemBought?.Invoke(bought);
        return $"收购成功！支出 {cost:F0}，已收入货架。";
    }

    /// <summary>售出货架物品：从货架移除并收款</summary>
    string ExecuteSell()
    {
        if (selectedStockItem == null) return "未选中待售物品。";
        if (currentClient == null) return "没有客户在店。";
        if (counterInventory == null || !counterInventory.Contains(selectedStockItem)) return "该物品不在货架上。";

        float price = pricer.EstimateSellPrice(selectedStockItem, dayScheduler.currentDayConfig);
        string itemName = selectedStockItem.itemName;

        if (!counterInventory.Remove(selectedStockItem))
            return "售出失败。";

        shopFunds += price;
        GameEvents.SafeInvoke(GameEvents.OnFundsChanged, shopFunds, price, "售出");
        GameEvents.SafeInvoke(GameEvents.OnPlaySFX, "sell_success", 0.8f);

        var sold = selectedStockItem;
        selectedStockItem = null;

        OnItemSold?.Invoke(sold);
        return $"售出 {itemName}！收入 {price:F0}。";
    }

    /// <summary>拒绝交易：清空当前选中</summary>
    string ExecuteReject()
    {
        selectedItem = null;
        selectedStockItem = null;
        lastReport = null;
        lastOffer = 0;
        return "已拒绝交易。";
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
            OnDecisionMade?.Invoke("融合成功！你获得了AI的部分能力。（扫描精度+20%，估值直觉解锁）");
        }
        else
        {
            gameState.wantedLevel += 20;
            GameEvents.SafeInvoke(GameEvents.OnWantedChanged, gameState.wantedLevel, 20f);
            GameEvents.SafeInvoke(GameEvents.OnScreenShake, 0.5f, 1f);
            OnDecisionMade?.Invoke("融合失败！AI试图夺取你的意识控制权！（通缉值+20）");
        }
        selectedItem = null;
    }

    // ============ 查询 ============
    public List<ItemInstance> GetCounterItems()
        => counterInventory != null ? counterInventory.GetAllItems() : new List<ItemInstance>();

    public int GetCounterCount()
        => counterInventory != null ? counterInventory.UsedSlots : 0;

    public float GetShopFunds() => shopFunds;
    public int GetCurrentDay() => gameState.currentDay;
    public float GetReputation() => gameState.reputation;
    public float GetWantedLevel() => gameState.wantedLevel;
    public float GetEnergy() => energyLevel;
}
