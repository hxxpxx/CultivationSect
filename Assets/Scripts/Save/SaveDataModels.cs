//// ===== 可序列化数据模型 =====
//using System;
//using System.Collections.Generic;
//using UnityEngine;

///// <summary>
///// 存档版本号，每次修改存档结构时+1
///// 用于兼容旧存档的迁移
///// </summary>
//[Serializable]
//public class SaveMeta
//{
//    public int saveVersion = 1;
//    public string saveName;
//    public DateTime saveTime;
//    public int day;
//    public float funds;
//    public float reputation;
//    public float wantedLevel;
//    public int totalPawns;
//    public int totalRejects;
//    public string playDuration; // 可读的游戏时长

//    // 运行时不序列化，用于显示
//    [NonSerialized] public string displayTime;
//}

//[Serializable]
//public class SerializableGameState
//{
//    public int currentDay = 1;
//    public float reputation = 50f;
//    public float wantedLevel = 0f;
//    public List<string> globalFlags = new List<string>();
//    public float playTimeSeconds = 0f; // 累计游戏秒数
//}

//[Serializable]
//public class SerializablePawnTicket
//{
//    public string ticketId;
//    public string itemInstanceId;
//    public float principal;
//    public float interestRate;
//    public int redeemDays;
//    public float penaltyRate;
//    public string acceptTime; // DateTime 转 string
//    public TicketStatus status;

//    // 物品快照（绝当后物品可能丢失引用）
//    public SerializableItemSnapshot itemSnapshot;
//}

//[Serializable]
//public class SerializableItemSnapshot
//{
//    public string instanceId;
//    public string itemDefId;       // 引用 ItemDef 的 itemId
//    public string displayName;
//    public float condition;
//    public float statedAge;
//    public float trueAge;
//    public string materialSignature;
//    public string serialNumber;
//    public bool stolen;
//    public bool hazardous;
//    public bool banned;
//    public ItemCategory category;
//    public float baseMarketValue;

//    // AI寄生快照
//    public SerializableAIParasite aiParasite;
//}

//[Serializable]
//public class SerializableAIParasite
//{
//    public bool isPresent;
//    public string aiName;
//    public AIPersonality personality;
//    public int intelligence;
//    public int aggression;
//    public int desperation;
//    public List<string> objectives;    // 只存 description 列表
//    public float corruptionLevel;
//    public float trustTowardPlayer;
//    public bool discoveredByPlayer;
//    public bool contained;
//    public List<string> knownSecrets;
//}

//[Serializable]
//public class SerializableDayProgress
//{
//    public int dayIndex;
//    public List<string> bannedCategories; // ItemCategory 枚举名
//    public float pawnPayoutRate;
//    public float stolenDiscount;
//    public float hazardDiscount;
//    public float legalPressure;
//    public float baseDemandMultiplier;
//    public string announcement;
//}

//[Serializable]
//public class SerializableClientQueue
//{
//    // 只存当天剩余未处理的客户（简化：存已处理过的客户ID列表，用于跳过）
//    public List<string> processedClientIds = new List<string>();
//    public int remainingClientCount;
//}

///// <summary>
///// 完整存档数据容器
///// </summary>
//[Serializable]
//public class FullSaveData
//{
//    public SaveMeta meta = new SaveMeta();
//    public SerializableGameState gameState = new SerializableGameState();
//    public List<SerializablePawnTicket> activeTickets = new List<SerializablePawnTicket>();
//    public SerializableDayProgress dayProgress = new SerializableDayProgress();
//    public float shopFunds;
//    public float energyLevel;
//    public int totalProcessedCount; // 今日已处理客户数
//    public SerializableClientQueue clientQueue = new SerializableClientQueue();
//}