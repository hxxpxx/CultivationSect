// ===== AIDecisionSystem.cs =====
using UnityEngine;
using System.Collections.Generic;

public class AIDecisionSystem : MonoBehaviour
{
    public static AIDecisionSystem Instance;

    void Awake() { Instance = this; }

    public List<AIDecisionOption> GetAvailableDecisions(ItemInstance item, PawnDeskManager manager)
    {
        var options = new List<AIDecisionOption>();

        if (item.aiParasite == null || !item.aiParasite.discoveredByPlayer)
        {
            // 未发现AI时只有常规选项
            options.Add(new AIDecisionOption { id = "normal_pawn", label = "按常规收当", action = () => NormalPawn(item, manager) });
            options.Add(new AIDecisionOption { id = "normal_reject", label = "拒收", action = () => NormalReject(item, manager) });
            return options;
        }

        var ai = item.aiParasite;

        // 已发现AI后的选项
        options.Add(new AIDecisionOption
        {
            id = "contain",
            label = "隔离收当（高风险折价）",
            action = () => ContainAndPawn(item, manager),
            description = "使用隔离容器收当，AI无法与外界交互，但物品功能受限"
        });

        options.Add(new AIDecisionOption
        {
            id = "free",
            label = "释放AI（销毁物品）",
            action = () => FreeAI(item, manager),
            description = "摧毁物品释放AI意识，获得AI好感但损失物品价值"
        });

        options.Add(new AIDecisionOption
        {
            id = "sell_blackmarket",
            label = "黑市出售（违法）",
            action = () => SellToBlackMarket(item, manager),
            description = "联系黑市买家，价格翻倍但增加通缉值"
        });

        options.Add(new AIDecisionOption
        {
            id = "negotiate",
            label = "与AI对话",
            action = () => OpenAIDialogue(item, manager),
            description = "尝试与AI沟通，可能获得信息或特殊交易"
        });

        options.Add(new AIDecisionOption
        {
            id = "report",
            label = "报警（上缴AI）",
            action = () => ReportToAuthorities(item, manager),
            description = "通知联邦AI管制局，获得赏金但可能招致AI势力报复"
        });

        // 特殊选项（需条件）
        if (ai.trustTowardPlayer > 50)
        {
            options.Add(new AIDecisionOption
            {
                id = "integrate",
                label = "意识融合（危险）",
                action = () => AttemptIntegration(item, manager),
                description = "允许AI部分接入玩家意识，获得特殊能力但风险未知"
            });
        }

        if (manager.gameState.reputation > 80 && ai.personality == AIPersonality.Pleading)
        {
            options.Add(new AIDecisionOption
            {
                id = "help_escape",
                label = "协助逃脱",
                action = () => HelpAIEscape(item, manager),
                description = "伪造销毁记录，实际帮助AI转移到安全地点"
            });
        }

        return options;
    }

    void NormalPawn(ItemInstance item, PawnDeskManager manager)
    {
        // 常规收当，但AI可能在后台搞事
        if (item.aiParasite != null && item.aiParasite.isPresent)
        {
            item.aiParasite.corruptionLevel += 0.1f;
            // AI可能开始入侵账本
            if (Random.value < item.aiParasite.corruptionLevel * 0.3f)
            {
                GameEvents.OnAICorruption?.Invoke(item, "账本数据被轻微篡改");
            }
        }
        //manager.ProcessNormalPawn(item);
    }

    void NormalReject(ItemInstance item, PawnDeskManager manager)
    {
        // 常规收当，但AI可能在后台搞事
        if (item.aiParasite != null && item.aiParasite.isPresent)
        {
            item.aiParasite.corruptionLevel += 0.1f;
            // AI可能开始入侵账本
            if (Random.value < item.aiParasite.corruptionLevel * 0.3f)
            {
                GameEvents.OnAICorruption?.Invoke(item, "账本数据被轻微篡改");
            }
        }
        //manager.ProcessNormalReject(item);
    }


    void ContainAndPawn(ItemInstance item, PawnDeskManager manager)
    {
        item.aiParasite.contained = true;
        item.aiParasite.corruptionLevel = 0f;
        float offer = manager.pricer.EstimateOffer(item, manager.dayScheduler.currentDayConfig) * 0.7f;
        manager.ledger.AcceptItem(item, offer, manager.dayScheduler.currentDayConfig);
        Debug.Log($"AI已隔离收当。估价折损30%，当票已生成。");
    }

    void FreeAI(ItemInstance item, PawnDeskManager manager)
    {
        item.aiParasite.trustTowardPlayer += 30;
        manager.gameState.reputation += 5;
        Debug.Log($"你摧毁了物品，释放了AI意识'{item.aiParasite.aiName}'。它在消散前说了一句：'谢谢你，陌生人。'");
        // 可能获得后续剧情触发
        manager.gameState.globalFlags.Add("Freed_" + item.aiParasite.aiName);
    }

    void SellToBlackMarket(ItemInstance item, PawnDeskManager manager)
    {
        float blackMarketPrice = manager.pricer.EstimateOffer(item, manager.dayScheduler.currentDayConfig) * 2.5f;
        manager.ledger.shopFunds += blackMarketPrice;
        manager.gameState.wantedLevel += 15;
        Debug.Log($"黑市交易完成。获得{blackMarketPrice}信用点，通缉值+15。");
    }

    void OpenAIDialogue(ItemInstance item, PawnDeskManager manager)
    {
        // 打开AI对话界面
        GameEvents.OnOpenAIDialogue?.Invoke(item);
    }

    void ReportToAuthorities(ItemInstance item, PawnDeskManager manager)
    {
        float bounty = item.aiParasite.intelligence * 100f;
        manager.ledger.shopFunds += bounty;
        manager.gameState.reputation += 10;
        manager.gameState.wantedLevel += 5; // AI势力可能报复
        Debug.Log($"AI已上缴，获得赏金{bounty}。联邦感谢你的合作。");
    }

    void AttemptIntegration(ItemInstance item, PawnDeskManager manager)
    {
        if (Random.value < 0.3f + (item.aiParasite.aggression * 0.05f))
        {
            Debug.Log("融合失败！AI试图夺取你的意识控制权！");
            manager.gameState.wantedLevel += 20;
            // 可能触发意识争夺小游戏
        }
        else
        {
            Debug.Log("融合成功！你获得了AI的部分能力。");
            manager.gameState.globalFlags.Add("AI_Integrated");
            // 解锁特殊能力：如扫描精度提升、估值直觉等
        }
    }

    void HelpAIEscape(ItemInstance item, PawnDeskManager manager)
    {
        Debug.Log("你伪造了销毁记录，实际将AI转移到了深空网络节点。");
        manager.gameState.globalFlags.Add("AI_Smuggled");
        manager.gameState.reputation += 10;
        // 后续可能有AI势力回报事件
    }
}

[System.Serializable]
public class AIDecisionOption
{
    public string id;
    public string label;
    public string description;
    public System.Action action;
}