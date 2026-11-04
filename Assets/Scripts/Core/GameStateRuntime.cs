// ===== GameStateRuntime.cs =====
// 全局运行时状态（跨场景持久单例）
// 资金 / 灵能 / 天数 / 声望 / 通缉 等需要跨场景沿用的数值统一放这里。
// 纯 C# 单例，不依赖场景物体，场景切换时不会被销毁。
// 灵能规则：只在每天开始时重置满，切换场景不重置。
using System.Collections.Generic;

public class GameStateRuntime
{
    public static GameStateRuntime Instance { get; } = new GameStateRuntime();

    public int currentDay = 1;
    public float reputation = 50f;
    public float wantedLevel = 0f;
    public List<string> globalFlags = new List<string>();

    // ===== 跨场景共享数值（由 PawnDeskManager 首次初始化，之后切场景沿用） =====
    public float shopFunds;              // 商栈资金
    public float maxSpiritEnergy = 100f; // 灵能上限
    public float spiritEnergy;           // 当前灵能（只在每天开始时满，切场景不清零）
    public bool fundsInitialized;        // 资金/灵能是否已初始化（防止重复覆盖）
    public int spiritResetDay = -1;      // 灵能已重置的天数；仅真正跨天(日更)时才重置灵能

    public class AITimelineState
    {
        public List<string> encounteredAIs = new List<string>();
        public int totalAIsFreed;
        public int totalAIsContained;
        public int totalAIsSold;
        public float averageTrustTowardPlayer;
        public bool playerIntegrated;
        public string dominantAIAlly;                     // 最信任玩家的AI
    }
}
