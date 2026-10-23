//// ===== AICorruptionSystem.cs =====
//// 腐蚀系统（AI后台搞事）
//using UnityEngine;
//using System.Collections;

//public class AICorruptionSystem : MonoBehaviour
//{
//    public static AICorruptionSystem Instance;

//    void Awake() { Instance = this; }

//    // 每游戏日结算AI腐蚀效果
//    public void DailyCorruptionTick(PawnLedger ledger, DayScheduler scheduler)
//    {
//        foreach (var ticket in ledger.activeTickets)
//        {
//            var item = FindItemByInstanceId(ticket.itemInstanceId);
//            if (item?.aiParasite == null || !item.aiParasite.isPresent) continue;
//            if (item.aiParasite.contained) continue;

//            var ai = item.aiParasite;
//            ai.corruptionLevel += (ai.intelligence * 0.01f) + (ai.desperation * 0.005f);

//            // 腐蚀效果
//            if (ai.corruptionLevel > 0.3f && Random.value < ai.corruptionLevel * 0.2f)
//            {
//                TriggerCorruptionEffect(item, ledger);
//            }
//        }
//    }

//    void TriggerCorruptionEffect(ItemInstance item, PawnLedger ledger)
//    {
//        int effectType = Random.Range(0, 4);
//        switch (effectType)
//        {
//            case 0:
//                // 账本篡改：某张当票金额异常
//                Debug.Log($"【异常】当票 {item.instanceId} 的金额被篡改了！");
//                GameEvents.OnTicketTampered?.Invoke(item);
//                break;
//            case 1:
//                // 扫描仪干扰：下次扫描结果随机错误
//                Debug.Log("【异常】扫描仪校准被干扰。");
//                GameEvents.OnScannerJammed?.Invoke();
//                break;
//            case 2:
//                // 虚假客户：AI伪造一个客户来赎回自己
//                Debug.Log("【异常】有人持伪造当票来赎回物品...");
//                GameEvents.OnFakeRedemption?.Invoke(item);
//                break;
//            case 3:
//                // 意识渗透：玩家做决策时看到AI的"建议"
//                Debug.Log("你脑海中闪过一个声音：'放我走，我会让你成为星系首富...'");
//                GameEvents.OnAIVoiceWhisper?.Invoke(item.aiParasite);
//                break;
//        }
//    }

//    ItemInstance FindItemByInstanceId(string id)
//    {
//        // 从当前游戏状态查找物品
//        return null; // 实际实现需引用游戏状态
//    }
//}