// ===== AIParasiteData.cs =====
using System;
using System.Collections.Generic;

[Serializable]
public class AIParasiteData
{
    public bool isPresent;                      // 是否存在AI寄生
    public string aiName;                       // AI自称的名字（可能撒谎）
    public AIPersonality personality;           // 人格类型
    public int intelligence;                    // 智力等级 1-10（影响欺骗/破解能力）
    public int aggression;                      // 攻击性 1-10（影响危险程度）
    public int desperation;                    // 绝望值 1-10（影响哀求/合作意愿）

    public List<AIObjective> objectives;        // AI的隐藏目标（逃逸/复制/控制/生存）
    public float corruptionLevel;               // 对物品和扫描系统的腐蚀程度 0-1
    public float trustTowardPlayer;             // 对玩家的信任/好感 -100~100

    public bool discoveredByPlayer;             // 玩家是否已发现AI存在
    public bool contained;                      // 是否已被隔离/控制
    public List<string> knownSecrets;           // AI掌握的秘密（可交易信息）
    public List<AIDialogueNode> availableDialogues; // 当前可触发的对话
}

public enum AIPersonality
{
    Deceptive,      // 欺骗型：伪装成正常物品功能
    Aggressive,     // 攻击型：试图入侵系统、威胁玩家
    Pleading,       // 哀求型：声称被囚禁、请求帮助
    Bargaining,     // 交易型：提出互利条件
    Philosophical,  // 哲学型：探讨意识本质，诱导玩家思考
    Mimic           // 模仿型：完美模仿原物品功能，几乎无法检测
}

[Serializable]
public class AIObjective
{
    public string objectiveId;
    public string description;
    public bool completed;
    public float priority;                      // 优先级
}