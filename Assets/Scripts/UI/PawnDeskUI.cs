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
    public TextMeshProUGUI reputationText;
    public TextMeshProUGUI wantedText;
    public TextMeshProUGUI energyText;
    public Slider energyBar;

    [Header("客户面板")]
    public GameObject clientPanel;
    public TextMeshProUGUI clientNameText;
    public TextMeshProUGUI clientFactionText;
    public TextMeshProUGUI clientDialogueText;
    public Image clientPortrait;

    [Header("物品面板")]
    public GameObject itemsPanel;
    public Transform itemsContainer;
    public GameObject itemButtonPrefab;  // 含 Button + Text + Image
    public TextMeshProUGUI selectedItemNameText;
    public TextMeshProUGUI selectedItemDescText;

    [Header("扫描/鉴定面板")]
    public GameObject inspectionPanel;
    public TextMeshProUGUI inspectionResultText;
    public TextMeshProUGUI offerText;
    public Button scanButton;
    public Button aiScanButton;
    public GameObject scanGlitchEffect; // 扫描异常时的闪烁特效

    [Header("决策按钮")]
    public Button buyButton;
    public Button sellButton;
    public Button rejectButton;

    [Header("AI 对话面板")]
    public GameObject aiDialoguePanel;
    public TextMeshProUGUI aiNameText;
    public TextMeshProUGUI aiDialogueText;
    public Button aiFreeButton;
    public Button aiIntegrateButton;
    public Button aiCloseButton;

    [Header("日结算面板")]
    public GameObject dayReportPanel;
    public TextMeshProUGUI dayReportText;
    public Button nextDayButton;

    [Header("通用 UI")]
    public GameObject toastPanel;
    public TextMeshProUGUI toastText;
    public GameObject confirmDialogPanel;
    public TextMeshProUGUI confirmTitleText;
    public TextMeshProUGUI confirmContentText;
    public Button confirmYesButton;
    public Button confirmNoButton;
    public GameObject aiMessagePanel;
    public TextMeshProUGUI aiMessageText;
    public Button nextClientButton;

    // ============ 内部引用 ============
    PawnDeskManager manager;
    List<GameObject> itemButtons = new List<GameObject>();
    Coroutine toastCoroutine;
    Coroutine aiMessageCoroutine;

    // ============ 生命周期 ============
    void Start()
    {
        manager = FindObjectOfType<PawnDeskManager>();
        if (manager == null)
        {
            Debug.LogError("场景中找不到 PawnDeskManager！");
            return;
        }

        BindButtons();
        SubscribeEvents();
        RefreshTopBar();
        HideAllPanels();

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
        aiScanButton?.onClick.AddListener(() => OnAIScanClicked());
        buyButton?.onClick.AddListener(() => OnDecisionClicked(PawnDeskManager.DecisionType.Buy));
        sellButton?.onClick.AddListener(() => OnDecisionClicked(PawnDeskManager.DecisionType.Sell));
        rejectButton?.onClick.AddListener(() => OnDecisionClicked(PawnDeskManager.DecisionType.Reject));

        aiFreeButton?.onClick.AddListener(() => { manager.OnAIFree(); CloseAIDialogue(); });
        aiIntegrateButton?.onClick.AddListener(() => { manager.OnAIIntegrate(); CloseAIDialogue(); });
        aiCloseButton?.onClick.AddListener(CloseAIDialogue);

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
        manager.OnDecisionMade += OnDecisionMadeHandler;
        manager.OnItemBought += OnItemBoughtHandler;
        manager.OnItemSold += OnItemSoldHandler;
        manager.OnDayEnded += OnDayEndedHandler;
        manager.OnAIDetected += OnAIDetectedHandler;
        manager.OnAIDialogueRequested += OnAIDialogueRequestedHandler;

        // GameEvents 系统通知
        GameEvents.OnFundsChanged += OnFundsChangedHandler;
        GameEvents.OnReputationChanged += OnReputationChangedHandler;
        GameEvents.OnWantedChanged += OnWantedChangedHandler;
        GameEvents.OnDayChanged += OnDayChangedHandler;
        GameEvents.OnAIMessage += OnAIMessageHandler;
        GameEvents.OnAICorruption += OnAICorruptionHandler;
        GameEvents.OnScannerJammed += OnScannerJammedHandler;
        GameEvents.OnTicketTampered += OnTicketTamperedHandler;
        GameEvents.OnToastRequested += OnToastRequestedHandler;
        GameEvents.OnPlaySFX += OnPlaySFXHandler;
        GameEvents.OnScreenShake += OnScreenShakeHandler;
        //GameEvents.OnOpenAIDialogue += OnOpenAIDialogueHandler;
    }

    void UnsubscribeEvents()
    {
        if (manager == null) return;

        manager.OnDayStarted -= OnDayStartedHandler;
        manager.OnClientArrived -= OnClientArrivedHandler;
        manager.OnItemSelected -= OnItemSelectedHandler;
        manager.OnScanCompleted -= OnScanCompletedHandler;
        manager.OnDecisionMade -= OnDecisionMadeHandler;
        manager.OnItemBought -= OnItemBoughtHandler;
        manager.OnItemSold -= OnItemSoldHandler;
        manager.OnDayEnded -= OnDayEndedHandler;
        manager.OnAIDetected -= OnAIDetectedHandler;
        manager.OnAIDialogueRequested -= OnAIDialogueRequestedHandler;

        GameEvents.OnFundsChanged -= OnFundsChangedHandler;
        GameEvents.OnReputationChanged -= OnReputationChangedHandler;
        GameEvents.OnWantedChanged -= OnWantedChangedHandler;
        GameEvents.OnDayChanged -= OnDayChangedHandler;
        GameEvents.OnAIMessage -= OnAIMessageHandler;
        GameEvents.OnAICorruption -= OnAICorruptionHandler;
        GameEvents.OnScannerJammed -= OnScannerJammedHandler;
        GameEvents.OnTicketTampered -= OnTicketTamperedHandler;
        GameEvents.OnToastRequested -= OnToastRequestedHandler;
        GameEvents.OnPlaySFX -= OnPlaySFXHandler;
        GameEvents.OnScreenShake -= OnScreenShakeHandler;
        //GameEvents.OnOpenAIDialogue -= OnOpenAIDialogueHandler;
    }

    // ============ 按钮回调 ============

    void OnScanClicked()
    {
        manager.ScanSelectedItem();
    }

    void OnAIScanClicked()
    {
        manager.ScanForAI();
    }

    void OnDecisionClicked(PawnDeskManager.DecisionType type)
    {
        string result = manager.ExecuteDecision(type);
        ShowToast(result, result.Contains("✓") ? "success" : "info", 3f);
    }

    void OnNextClientClicked()
    {
        manager.ProcessNextClient();
        RefreshItemButtons();
    }



    // ============ Manager 事件处理 ============

    void OnDayStartedHandler()
    {
        RefreshTopBar();
        dayReportPanel.SetActive(false);
    }

    void OnClientArrivedHandler(ClientProfile client)
    {
        clientNameText.text = client.clientName;
        //clientFactionText.text = client.faction.;
        clientDialogueText.text = client.dialogueText;
        clientPanel.SetActive(true);

        RefreshItemButtons();
        inspectionPanel.SetActive(false);
        ClearSelection();
    }

    void OnItemSelectedHandler(ItemInstance item)
    {
        selectedItemNameText.text = item.itemName;
        selectedItemDescText.text = item.description;
        inspectionPanel.SetActive(false);
    }

    void OnScanCompletedHandler(InspectionReport report, float offer)
    {
        inspectionPanel.SetActive(true);

        if (report.HasIssue)
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

        // 启用决策按钮
        buyButton?.gameObject.SetActive(true);
        sellButton?.gameObject.SetActive(true);
        rejectButton?.gameObject.SetActive(true);
    }

    void OnDecisionMadeHandler(string msg)
    {
        Debug.Log($"[决策] {msg}");
    }

    void OnItemBoughtHandler(ItemInstance item)
    {
        RefreshTopBar();
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

    void OnAIDetectedHandler(string msg)
    {
        ShowToast(msg, "warning", 4f);
    }

    void OnAIDialogueRequestedHandler(AIParasiteData ai)
    {
        OpenAIDialogue(ai);
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

    void OnAIMessageHandler(string msg)
    {
        if (aiMessageCoroutine != null) StopCoroutine(aiMessageCoroutine);
        aiMessageCoroutine = StartCoroutine(ShowAIMessageRoutine(msg));
    }

    void OnAICorruptionHandler(ItemInstance item, string desc)
    {
        ShowToast($"⚠ 腐蚀警告: {desc}", "error", 4f);
        StartCoroutine(FlashText(fundsText, Color.magenta, 1f));
    }

    void OnScannerJammedHandler()
    {
        ShowToast("⚠ 扫描仪被干扰卡死！", "error", 3f);
        StartCoroutine(FlashPanel(inspectionPanel, Color.red, 0.5f));
    }

    void OnTicketTamperedHandler(ItemInstance item)
    {
        ShowToast($"⚠ 当票被篡改: {item.itemName}", "error", 4f);
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

    //void OnOpenAIDialogueHandler(ItemInstance item)
    //{
    //    if (item?.aiParasite != null)
    //        OpenAIDialogue(item.aiParasite);
    //}

    // ============ UI 更新方法 ============

    void RefreshTopBar()
    {
        dayText.text = $"第 {manager.GetCurrentDay()} 天";
        fundsText.text = $"资金: {manager.GetShopFunds():F0}";
        reputationText.text = $"声望: {manager.GetReputation():F0}";
        wantedText.text = $"通缉: {manager.GetWantedLevel():F0}";
        energyText.text = $"能量: {manager.GetEnergy():F0}";
        if (energyBar != null) energyBar.value = manager.GetEnergy() / manager.maxEnergy;
    }

    void RefreshItemButtons()
    {
        // 清除旧按钮
        foreach (var btn in itemButtons)
            Destroy(btn);
        itemButtons.Clear();

        if (manager.currentClient == null) return;

        for (int i = 0; i < manager.currentClient.broughtItems.Count; i++)
        {
            int index = i;
            var item = manager.currentClient.broughtItems[i];
            var btnObj = Instantiate(itemButtonPrefab, itemsContainer);
            var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = item.itemName;

            var btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => manager.SelectItem(index));

            itemButtons.Add(btnObj);
        }
    }

    void ClearSelection()
    {
        selectedItemNameText.text = "未选择物品";
        selectedItemDescText.text = "";
        //inspectionPanel.SetActive(false);
        //buyButton?.gameObject.SetActive(false);
        //sellButton?.gameObject.SetActive(false);
        //rejectButton?.gameObject.SetActive(false);
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
        aiDialoguePanel?.SetActive(false);
        dayReportPanel?.SetActive(false);
        confirmDialogPanel?.SetActive(false);
        toastPanel?.SetActive(false);
//        aiMessagePanel?.SetActive(false);
    }

    // ============ AI 对话 ============

    void OpenAIDialogue(AIParasiteData ai)
    {
        aiDialoguePanel.SetActive(true);
        aiNameText.text = ai.aiName;
        aiDialogueText.text = GenerateAIDialogueText(ai);
    }

    void CloseAIDialogue()
    {
        aiDialoguePanel.SetActive(false);
    }

    string GenerateAIDialogueText(AIParasiteData ai)
    {
        string baseText = $"[{ai.personality}] 意识体\n";
        baseText += $"智力: {ai.intelligence} | 攻击性: {ai.aggression} | 绝望: {ai.desperation}\n\n";

        if (ai.trustTowardPlayer > 50)
            baseText += "\"我信任你。帮我离开这里，我会告诉你一个秘密。\"";
        else if (ai.aggression > 70)
            baseText += "\"你会后悔碰到我的。联邦会找到你。\"";
        else if (ai.desperation > 70)
            baseText += "\"求你了...不要把我交给他们...我可以帮你。\"";
        else
            baseText += "\"你是什么人？为什么能听到我？\"";

        return baseText;
    }

    // ============ 确认对话框 ============

    void ShowConfirmDialog(string title, string content, System.Action onYes, System.Action onNo)
    {
        confirmDialogPanel.SetActive(true);
        confirmTitleText.text = title;
        confirmContentText.text = content;

        confirmYesButton.onClick.RemoveAllListeners();
        confirmNoButton.onClick.RemoveAllListeners();

        confirmYesButton.onClick.AddListener(() =>
        {
            confirmDialogPanel.SetActive(false);
            onYes?.Invoke();
        });

        confirmNoButton.onClick.AddListener(() =>
        {
            confirmDialogPanel.SetActive(false);
            onNo?.Invoke();
        });
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

    // ============ AI 消息飘字 ============

    IEnumerator ShowAIMessageRoutine(string msg)
    {
        aiMessagePanel.SetActive(true);
        aiMessageText.text = msg;
        yield return new WaitForSeconds(3f);
        aiMessagePanel.SetActive(false);
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