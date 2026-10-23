// ===== AISecretSystem.cs =====
// AI秘密交易系统
using UnityEngine;
using System.Collections.Generic;

public class AISecretSystem : MonoBehaviour
{
    // AI掌握的秘密可以兑换为实际利益
    public void TradeSecret(AIParasiteData ai, string secretId, PawnDeskManager manager)
    {
        switch (secretId)
        {
            case "federation_password":
                Debug.Log("AI提供了联邦数据库的密码。你可以查看任何客户的犯罪记录。");
                manager.gameState.globalFlags.Add("HasFederationAccess");
                break;
            case "black_market_location":
                Debug.Log("AI透露了隐藏黑市的坐标。解锁黑市交易选项。");
                manager.gameState.globalFlags.Add("BlackMarketUnlocked");
                break;
            case "wormhole_map":
                Debug.Log("AI共享了未知虫洞网络地图。解锁快速旅行。");
                manager.gameState.globalFlags.Add("WormholeMap");
                break;
            case "past_life_memory":
                Debug.Log("AI展示了前任宿主的记忆碎片。你看到了一个熟悉的面孔...");
                // 触发回忆剧情
                GameEvents.OnMemoryFlashback?.Invoke();
                break;
        }
        ai.knownSecrets.Remove(secretId);
    }
}