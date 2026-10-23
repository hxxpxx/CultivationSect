// ===== GameEvents.cs =====
// 放在 Assets/Scripts/Core/ 或 Assets/Scripts/ 根目录下
// 无需挂载到任何物体，纯静态类

using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 全局事件总线
/// 所有跨系统的通信通过这里中转，避免脚本间直接引用
/// </summary>
public static class GameEvents
{
    // ============ 扫描与鉴定事件 ============

    /// <summary>
    /// 扫描仪数据异常/乱码
    /// 参数: (物品实例, 异常描述)
    /// 触发: AI行为事件、腐蚀系统
    /// </summary>
    public static Action<ItemInstance, string> OnScanGlitch;

    /// <summary>
    /// 物品温度异常飙升
    /// 参数: (物品实例, 温度值)
    /// 触发: AI行为事件
    /// </summary>
    public static Action<ItemInstance, float> OnItemTemperatureSpike;

    /// <summary>
    /// 通用AI消息提示（飘字/日志）
    /// 参数: (消息文本)
    /// 触发: AI行为事件、AI对话系统
    /// </summary>
    public static Action<string> OnAIMessage;

    /// <summary>
    /// 请求打开AI对话界面
    /// 参数: (物品实例)
    /// 触发: 玩家点击"与AI对话"按钮
    /// </summary>
    public static Action<ItemInstance> OnOpenAIDialogue;

    /// <summary>
    /// AI腐蚀/篡改事件通知
    /// 参数: (物品实例, 篡改描述)
    /// 触发: AICorruptionSystem 每日结算
    /// </summary>
    public static Action<ItemInstance, string> OnAICorruption;

    /// <summary>
    /// 扫描仪被干扰/卡死
    /// 参数: 无
    /// 触发: AICorruptionSystem 腐蚀效果
    /// </summary>
    public static Action OnScannerJammed;

    /// <summary>
    /// 当票被篡改
    /// 参数: (物品实例)
    /// 触发: AICorruptionSystem 腐蚀效果
    /// </summary>
    public static Action<ItemInstance> OnTicketTampered;

    /// <summary>
    /// 虚假客户来赎回（AI伪造）
    /// 参数: (物品实例)
    /// 触发: AICorruptionSystem 腐蚀效果
    /// </summary>
    public static Action<ItemInstance> OnFakeRedemption;

    /// <summary>
    /// AI在玩家脑海中低语
    /// 参数: (AI寄生数据)
    /// 触发: AICorruptionSystem 意识渗透
    /// </summary>
    public static Action<AIParasiteData> OnAIVoiceWhisper;

    /// <summary>
    /// 记忆闪回（AI分享前任宿主记忆）
    /// 参数: 无
    /// 触发: AISecretSystem 秘密交易
    /// </summary>
    public static Action OnMemoryFlashback;

    // ============ 经济与账本事件 ============

    /// <summary>
    /// 资金变动通知
    /// 参数: (变动后总额, 变动量, 变动原因)
    /// 触发: PawnLedger 任何资金变化
    /// </summary>
    public static Action<float, float, string> OnFundsChanged;

    /// <summary>
    /// 声望变动通知
    /// 参数: (变动后声望, 变动量)
    /// 触发: 决策系统
    /// </summary>
    public static Action<float, float> OnReputationChanged;

    /// <summary>
    /// 通缉值变动通知
    /// 参数: (变动后通缉值, 变动量)
    /// 触发: 黑市交易、报警、AI相关决策
    /// </summary>
    public static Action<float, float> OnWantedChanged;

    /// <summary>
    /// 当票状态变更
    /// 参数: (当票, 旧状态, 新状态)
    /// 触发: 赎回、绝当、收当
    /// </summary>
    public static Action<PawnTicket, TicketStatus, TicketStatus> OnTicketStatusChanged;

    // ============ 游戏流程事件 ============

    /// <summary>
    /// 天数变更
    /// 参数: (新天数)
    /// 触发: DayScheduler 推进天数
    /// </summary>
    public static Action<int> OnDayChanged;

    /// <summary>
    /// 规则生效通知
    /// 参数: (规则, 当天配置)
    /// 触发: DayScheduler 每日开始
    /// </summary>
    public static Action<AppraisalRuleBase, DayConfig> OnRuleActivated;

    /// <summary>
    /// 全局Flag设置
    /// 参数: (flag名称, 是否设置)
    /// 触发: 各种剧情决策
    /// </summary>
    public static Action<string, bool> OnGlobalFlagChanged;

    // ============ UI通知事件 ============

    /// <summary>
    /// 屏幕震动/闪烁效果请求
    /// 参数: (强度 0-1, 持续时间)
    /// 触发: 高危AI、警报、爆炸
    /// </summary>
    public static Action<float, float> OnScreenShake;

    /// <summary>
    /// 播放音效请求
    /// 参数: (音效ID/名称, 音量)
    /// 触发: 各系统需要音效反馈时
    /// </summary>
    public static Action<string, float> OnPlaySFX;

    /// <summary>
    /// 弹出确认对话框
    /// 参数: (标题, 内容, 确认回调, 取消回调)
    /// 触发: 危险操作前确认
    /// </summary>
    public static Action<string, string, Action, Action> OnConfirmDialogRequested;

    /// <summary>
    /// Toast提示（短暂自动消失）
    /// 参数: (消息, 类型: info/warning/error/success, 持续时间)
    /// 触发: 各种操作反馈
    /// </summary>
    public static Action<string, string, float> OnToastRequested;


    // 存档相关事件
    //public static event Action<int> OnSaveComplete;
    //public static event Action<int> OnLoadComplete;
    //public static event Action<int> OnSaveDeleted;
    // ============ 工具方法 ============

    /// <summary>
    /// 安全调用无参事件（防止事件为null时报错）
    /// </summary>
    public static void SafeInvoke(Action action)
    {
        if (action != null)
            action.Invoke();
    }

    /// <summary>
    /// 安全调用单参事件
    /// </summary>
    public static void SafeInvoke<T>(Action<T> action, T arg)
    {
        if (action != null)
            action.Invoke(arg);
    }

    /// <summary>
    /// 安全调用双参事件
    /// </summary>
    public static void SafeInvoke<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
    {
        if (action != null)
            action.Invoke(arg1, arg2);
    }

    /// <summary>
    /// 安全调用三参事件
    /// </summary>
    public static void SafeInvoke<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
    {
        if (action != null)
            action.Invoke(arg1, arg2, arg3);
    }

    /// <summary>
    /// 安全调用四参事件
    /// </summary>
    public static void SafeInvoke<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (action != null)
            action.Invoke(arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// 清空所有事件订阅（场景切换/游戏重启时调用）
    /// 防止内存泄漏和跨场景事件残留
    /// </summary>
    public static void ClearAll()
    {
        OnScanGlitch = null;
        OnItemTemperatureSpike = null;
        OnAIMessage = null;
        OnOpenAIDialogue = null;
        OnAICorruption = null;
        OnScannerJammed = null;
        OnTicketTampered = null;
        OnFakeRedemption = null;
        OnAIVoiceWhisper = null;
        OnMemoryFlashback = null;

        OnFundsChanged = null;
        OnReputationChanged = null;
        OnWantedChanged = null;
        OnTicketStatusChanged = null;

        OnDayChanged = null;
        OnRuleActivated = null;
        OnGlobalFlagChanged = null;

        OnScreenShake = null;
        OnPlaySFX = null;
        OnConfirmDialogRequested = null;
        OnToastRequested = null;

        Debug.Log("[GameEvents] 所有事件已清空");
    }

    /// <summary>
    /// 获取当前所有事件的订阅数量（调试用）
    /// </summary>
    public static Dictionary<string, int> GetSubscriptionCounts()
    {
        var counts = new Dictionary<string, int>();

        counts["OnScanGlitch"] = OnScanGlitch?.GetInvocationList()?.Length ?? 0;
        counts["OnItemTemperatureSpike"] = OnItemTemperatureSpike?.GetInvocationList()?.Length ?? 0;
        counts["OnAIMessage"] = OnAIMessage?.GetInvocationList()?.Length ?? 0;
        counts["OnOpenAIDialogue"] = OnOpenAIDialogue?.GetInvocationList()?.Length ?? 0;
        counts["OnAICorruption"] = OnAICorruption?.GetInvocationList()?.Length ?? 0;
        counts["OnScannerJammed"] = OnScannerJammed?.GetInvocationList()?.Length ?? 0;
        counts["OnTicketTampered"] = OnTicketTampered?.GetInvocationList()?.Length ?? 0;
        counts["OnFakeRedemption"] = OnFakeRedemption?.GetInvocationList()?.Length ?? 0;
        counts["OnAIVoiceWhisper"] = OnAIVoiceWhisper?.GetInvocationList()?.Length ?? 0;
        counts["OnMemoryFlashback"] = OnMemoryFlashback?.GetInvocationList()?.Length ?? 0;

        counts["OnFundsChanged"] = OnFundsChanged?.GetInvocationList()?.Length ?? 0;
        counts["OnReputationChanged"] = OnReputationChanged?.GetInvocationList()?.Length ?? 0;
        counts["OnWantedChanged"] = OnWantedChanged?.GetInvocationList()?.Length ?? 0;
        counts["OnTicketStatusChanged"] = OnTicketStatusChanged?.GetInvocationList()?.Length ?? 0;

        counts["OnDayChanged"] = OnDayChanged?.GetInvocationList()?.Length ?? 0;
        counts["OnRuleActivated"] = OnRuleActivated?.GetInvocationList()?.Length ?? 0;
        counts["OnGlobalFlagChanged"] = OnGlobalFlagChanged?.GetInvocationList()?.Length ?? 0;

        counts["OnScreenShake"] = OnScreenShake?.GetInvocationList()?.Length ?? 0;
        counts["OnPlaySFX"] = OnPlaySFX?.GetInvocationList()?.Length ?? 0;
        counts["OnConfirmDialogRequested"] = OnConfirmDialogRequested?.GetInvocationList()?.Length ?? 0;
        counts["OnToastRequested"] = OnToastRequested?.GetInvocationList()?.Length ?? 0;

        return counts;
    }
}