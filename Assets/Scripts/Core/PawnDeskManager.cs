// ===== PawnDeskManager.cs =====
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

    // ============ 运行时子系统 ============
    [HideInInspector] public AppraisalValidator validator;
    [HideInInspector] public PricingEngine pricer;
    [HideInInspector] public PawnLedger ledger;
    [HideInInspector] public DayScheduler dayScheduler;
    [HideInInspector] public GameStateRuntime gameState;
    [HideInInspector] public AIDecisionSystem aiDecisionSystem;
//    [HideInInspector] public AICorruptionSystem corruptionSystem;
    [HideInInspector] public AISecretSystem secretSystem;
    [HideInInspector] public RedemptionScheduler redemptionScheduler;

    // ============ 当前状态 ============
    [HideInInspector] public ClientProfile currentClient;
    [HideInInspector] public ItemInstance selectedItem;
    [HideInInspector] public InspectionReport lastReport;
    [HideInInspector] public float lastOffer;
    [HideInInspector] public bool isProcessingDay;

    // ============ 实例事件（UI直接回调用） ============
    public System.Action OnDayStarted;
    public System.Action<ClientProfile> OnClientArrived;
    public System.Action<ItemInstance> OnItemSelected;
    public System.Action<InspectionReport, float> OnScanCompleted;
    public System.Action<string> OnDecisionMade;
    public System.Action<PawnTicket> OnItemPawned;
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
        redemptionScheduler = new RedemptionScheduler();

        //if (GetComponent<AIDetectionTool>() == null)
        //    gameObject.AddComponent<AIDetectionTool>();

        //if (FindObjectOfType<AIBehaviorEvent>() == null)
        //    gameObject.AddComponent<AIBehaviorEvent>();

        Debug.Log("深空当铺系统初始化完成。");
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
        lastReport = null;
        lastOffer = 0;

        Debug.Log($"客户入店: {currentClient.clientCareer} ({currentClient.clientFaction})");
        OnClientArrived?.Invoke(currentClient);
        GameEvents.SafeInvoke(GameEvents.OnToastRequested, $"客户: {currentClient.clientName}", "info", 1.5f);
    }

    public void EndDay()
    {
        isProcessingDay = false;

        redemptionScheduler.TickDay(ledger, dayScheduler.currentDayConfig);
        //corruptionSystem.DailyCorruptionTick(ledger, dayScheduler);
        energyLevel = maxEnergy;

        // ✅ GameEvents 转发
        GameEvents.SafeInvoke(GameEvents.OnDayChanged, gameState.currentDay);
        GameEvents.SafeInvoke(GameEvents.OnFundsChanged, ledger.shopFunds, 0f, "日结算");

        OnDayEnded?.Invoke();

        // 自动存档
        //var saveMgr = FindObjectOfType<SaveManager>();
        //if (saveMgr != null) saveMgr.AutoSave();

        Debug.Log($"=== 第 {gameState.currentDay} 天结束 ===");
        GameEvents.SafeInvoke(GameEvents.OnToastRequested, $"第 {gameState.currentDay} 天结束，资金: {ledger.shopFunds:F0}", "info", 3f);
    }

    public void AdvanceToNextDay()
    {
        gameState.currentDay++;
        StartNewDay();
    }

    // ============ 物品操作 ============

    public void SelectItem(int itemIndex)
    {
        if (currentClient == null || itemIndex < 0 || itemIndex >= currentClient.broughtItems.Count)
        {
            Debug.LogWarning("无效物品索引");
            return;
        }

        selectedItem = currentClient.broughtItems[itemIndex];
        lastReport = null;
        lastOffer = 0;

        Debug.Log($"选中物品: {selectedItem.itemName}");
        OnItemSelected?.Invoke(selectedItem);
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

    // ============ 决策 ============

    public enum DecisionType { Pawn, Reject, Report, BlackMarket, Negotiate }

    public string ExecuteDecision(DecisionType decision)
    {
        if (selectedItem == null) return "未选中物品";

        string resultMessage = "";

        switch (decision)
        {
            case DecisionType.Pawn: resultMessage = ExecutePawn(); break;
            case DecisionType.Reject: resultMessage = ExecuteReject(); break;
            case DecisionType.Report: resultMessage = ExecuteReport(); break;
            case DecisionType.BlackMarket: resultMessage = ExecuteBlackMarket(); break;
            case DecisionType.Negotiate: resultMessage = ExecuteNegotiate(); break;
        }

        OnDecisionMade?.Invoke(resultMessage);
        selectedItem = null;
        lastReport = null;
        return resultMessage;
    }

    string ExecutePawn()
    {
        if (selectedItem.aiParasite != null && selectedItem.aiParasite.discoveredByPlayer)
            return "该物品存在AI寄生意识，请先决定如何处理AI。";

        if (lastOffer <= 0) return "无法估价，请先扫描物品。";
        if (ledger.shopFunds < lastOffer) return "当铺资金不足，无法收当。";

        float oldFunds = ledger.shopFunds;

        if (lastReport != null && lastReport.HasIssue)
        {
            if (selectedItem.aiParasite != null && selectedItem.aiParasite.isPresent)
                selectedItem.aiParasite.corruptionLevel += 0.15f;
        }

        var ticket = ledger.AcceptItem(selectedItem, lastOffer, dayScheduler.currentDayConfig);
        if (ticket != null)
        {
            // ✅ GameEvents 转发
            GameEvents.SafeInvoke(GameEvents.OnFundsChanged, ledger.shopFunds, -lastOffer, "收当放款");
            GameEvents.SafeInvoke(GameEvents.OnTicketStatusChanged, ticket, TicketStatus.None, TicketStatus.Held);
            GameEvents.SafeInvoke(GameEvents.OnPlaySFX, "pawn_success", 0.8f);

            OnItemPawned?.Invoke(ticket);
            return $"✓ 收当成功！放款 {lastOffer:F0} 信用点。当票ID: {ticket.ticketId}";
        }
        return "✗ 收当失败。";
    }

    string ExecuteReject()
    {
        GameEvents.SafeInvoke(GameEvents.OnToastRequested, "已拒收", "info", 1.5f);
        return "✗ 已拒收该物品。";
    }

    string ExecuteReport()
    {
        if (selectedItem.aiParasite != null && selectedItem.aiParasite.discoveredByPlayer)
        {
            float bounty = selectedItem.aiParasite.intelligence * 100f;
            ledger.shopFunds += bounty;
            gameState.reputation += 10;
            gameState.wantedLevel += 5;

            // ✅ GameEvents 转发
            GameEvents.SafeInvoke(GameEvents.OnFundsChanged, ledger.shopFunds, bounty, "AI赏金");
            GameEvents.SafeInvoke(GameEvents.OnReputationChanged, gameState.reputation, 10f);
            GameEvents.SafeInvoke(GameEvents.OnWantedChanged, gameState.wantedLevel, 5f);
            GameEvents.SafeInvoke(GameEvents.OnPlaySFX, "alarm_report", 0.9f);

            return $"✓ 已报警。AI '{selectedItem.aiParasite.aiName}' 被联邦收押。获得赏金 {bounty:F0}。";
        }
        else if (lastReport != null && lastReport.HasIssue)
        {
            gameState.reputation += 5;
            GameEvents.SafeInvoke(GameEvents.OnReputationChanged, gameState.reputation, 5f);
            return "✓ 异常已上报当局。";
        }
        return "没有需要上报的内容。";
    }

    string ExecuteBlackMarket()
    {
        if (selectedItem.aiParasite == null || !selectedItem.aiParasite.discoveredByPlayer)
            return "该物品无特殊价值，黑市不感兴趣。";

        float bmPrice = pricer.EstimateOffer(selectedItem, dayScheduler.currentDayConfig) * 2.5f;
        ledger.shopFunds += bmPrice;
        gameState.wantedLevel += 15;
        gameState.reputation -= 5;

        // ✅ GameEvents 转发
        GameEvents.SafeInvoke(GameEvents.OnFundsChanged, ledger.shopFunds, bmPrice, "黑市交易");
        GameEvents.SafeInvoke(GameEvents.OnWantedChanged, gameState.wantedLevel, 15f);
        GameEvents.SafeInvoke(GameEvents.OnReputationChanged, gameState.reputation, -5f);
        GameEvents.SafeInvoke(GameEvents.OnPlaySFX, "black_market", 0.8f);

        return $"✓ 黑市交易完成！获得 {bmPrice:F0} 信用点。（通缉值+15，声望-5）";
    }

    string ExecuteNegotiate()
    {
        if (selectedItem.aiParasite == null || !selectedItem.aiParasite.discoveredByPlayer)
            return "该物品中没有可对话的意识。";

        OnAIDialogueRequested?.Invoke(selectedItem.aiParasite);
        GameEvents.SafeInvoke(GameEvents.OnOpenAIDialogue, selectedItem);
        return "正在与AI对话...";
    }

    // ============ AI 决策回调 ============

    public void OnAIContainAndPawn()
    {
        if (selectedItem?.aiParasite == null) return;

        selectedItem.aiParasite.contained = true;
        selectedItem.aiParasite.corruptionLevel = 0f;

        float offer = pricer.EstimateOffer(selectedItem, dayScheduler.currentDayConfig) * 0.7f;
        var ticket = ledger.AcceptItem(selectedItem, offer, dayScheduler.currentDayConfig);

        GameEvents.SafeInvoke(GameEvents.OnFundsChanged, ledger.shopFunds, -offer, "AI隔离收当");
        GameEvents.SafeInvoke(GameEvents.OnTicketStatusChanged, ticket, TicketStatus.None, TicketStatus.Held);

        OnDecisionMade?.Invoke($"AI已隔离收当。估价折损30%，放款 {offer:F0}。");
        selectedItem = null;
    }

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
    public List<PawnTicket> GetActiveTickets() => ledger.activeTickets.Where(t => t.status == TicketStatus.Held).ToList();
    public float GetShopFunds() => ledger.shopFunds;
    public int GetCurrentDay() => gameState.currentDay;
    public float GetReputation() => gameState.reputation;
    public float GetWantedLevel() => gameState.wantedLevel;
    public float GetEnergy() => energyLevel;



    //public void ShowConfirmDialog(string title, string content, System.Action onYes, System.Action onNo)
    //{
    //    if (confirmDialogPanel == null) return;
    //    confirmDialogPanel.SetActive(true);
    //    if (confirmTitleText != null) confirmTitleText.text = title;
    //    if (confirmContentText != null) confirmContentText.text = content;

    //    // 清除旧监听
    //    confirmYesButton.onClick.RemoveAllListeners();
    //    confirmNoButton.onClick.RemoveAllListeners();

    //    confirmYesButton.onClick.AddListener(() => {
    //        confirmDialogPanel.SetActive(false);
    //        onYes?.Invoke();
    //    });

    //    confirmNoButton.onClick.AddListener(() => {
    //        confirmDialogPanel.SetActive(false);
    //        onNo?.Invoke();
    //    });
    //}

    //public void ShowToast(string message, float duration = 2f)
    //{
    //    if (toastPanel == null || toastText == null) return;
    //    toastText.text = message;
    //    toastPanel.SetActive(true);
    //    CancelInvoke(nameof(HideToast));
    //    Invoke(nameof(HideToast), duration);
    //}

    //void HideToast()
    //{
    //    if (toastPanel != null) toastPanel.SetActive(false);
    //}
}