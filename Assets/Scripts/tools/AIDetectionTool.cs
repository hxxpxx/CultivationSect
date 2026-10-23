// ===== AIDetectionTool.cs =====
using UnityEngine;

public class AIDetectionTool : MonoBehaviour
{
    public float detectionPower = 1.0f;         // 设备性能
    public float energyCost = 10f;              // 每次使用消耗能量

    public AIDetectionResult ScanForAI(ItemInstance item, PawnDeskManager manager)
    {
        if (manager.energyLevel < energyCost)
        {
            return new AIDetectionResult { success = false, message = "检测设备能量不足" };
        }

        manager.energyLevel -= energyCost;

        if (item.aiParasite == null || !item.aiParasite.isPresent)
        {
            return new AIDetectionResult { success = true, found = false, message = "未检测到异常意识活动" };
        }

        float detectChance = detectionPower - (item.aiParasite.intelligence * 0.08f)
                           + (item.aiParasite.corruptionLevel * 0.2f);

        var result = new AIDetectionResult { success = true, found = Random.value < detectChance };
        if (result.found)
        {
            item.aiParasite.discoveredByPlayer = true;
            result.message = $"检测到AI寄生意识：自称'{item.aiParasite.aiName}'，危险等级{item.aiParasite.aggression}/10";
            result.parasiteData = item.aiParasite;
        }
        else
        {
            result.message = "检测到模糊的数字回声，但无法确认具体性质。";
        }

        return result;
    }
}

public class AIDetectionResult
{
    public bool success;
    public bool found;
    public string message;
    public AIParasiteData parasiteData;
}