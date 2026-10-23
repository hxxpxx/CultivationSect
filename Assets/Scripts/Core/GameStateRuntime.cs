// ===== GameStateRuntime.cs =====
using System.Collections.Generic;

public class GameStateRuntime
{
    public int currentDay = 1;
    public float reputation = 50f;
    public float wantedLevel = 0f;
    public List<string> globalFlags = new List<string>();

    public class AITimelineState
    {
        public List<string> encounteredAIs = new List<string>();
        public int totalAIsFreed;
        public int totalAIsContained;
        public int totalAIsSold;
        public float averageTrustTowardPlayer;
        public bool playerIntegrated;
        public string dominantAIAlly;                     // 最信任玩家的AI

        //public EndingType CalculateEnding()
        //{
        //    if (playerIntegrated) return EndingType.Ascension; // 与AI融合结局
        //    if (totalAIsFreed > totalAIsContained + totalAIsSold) return EndingType.Liberator; // 解放者
        //    if (totalAIsSold > totalAIsFreed + totalAIsContained) return EndingType.Trafficker; // 贩卖者
        //    if (totalAIsContained > totalAIsFreed + totalAIsSold) return EndingType.Jailer; // 囚禁者
        //    return EndingType.Merchant; // 普通当铺老板
        //}
    }
}

