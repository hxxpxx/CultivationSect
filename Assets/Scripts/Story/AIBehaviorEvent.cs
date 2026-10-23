// ===== AIBehaviorEvent.cs =====
using UnityEngine;
using System.Collections;

public class AIBehaviorEvent : MonoBehaviour
{
    public static AIBehaviorEvent Instance;

    void Awake() { Instance = this; }

    // 随机触发AI行为事件
    public void TriggerRandomAIBehavior(ItemInstance item, ClientProfile client)
    {
        if (item.aiParasite == null || !item.aiParasite.isPresent) return;
        if (item.aiParasite.discoveredByPlayer) return; // 已发现就不再偷偷来

        float chance = item.aiParasite.intelligence * 0.02f + item.aiParasite.desperation * 0.01f;
        if (Random.value > chance) return;

        StartCoroutine(PlayAIBehaviorEvent(item));
    }

    IEnumerator PlayAIBehaviorEvent(ItemInstance item)
    {
        // 扫描仪突然显示乱码
        yield return new WaitForSeconds(Random.Range(2f, 5f));
        GameEvents.OnScanGlitch?.Invoke(item, "SCAN_DATA_CORRUPTED");

        yield return new WaitForSeconds(1.5f);

        // 物品温度异常
        GameEvents.OnItemTemperatureSpike?.Invoke(item, Random.Range(40f, 80f));

        // 如果玩家正在查看，显示文字暗示
        if (GameEvents.OnAIMessage != null)
        {
            string[] hints = {
                "你听到物品发出微弱的嗡鸣声...",
                "扫描屏幕闪过一行无法识别的文字",
                "物品表面出现短暂的像素化现象",
                "你感觉有人在看着你...但店里只有你和客户"
            };
            GameEvents.OnAIMessage.Invoke(hints[Random.Range(0, hints.Length)]);
        }
    }
}