//// ===== RuleAIParasite.cs (扩展规则) =====
//using UnityEngine;

//[CreateAssetMenu(menuName = "CultivationSectLeader/Rules/AIParasite")]
//public class RuleAIParasite : AppraisalRuleBase
//{
//    public float baseDetectionChance = 0.3f;
//    public override bool Evaluate(AppraisalContext ctx, out string finding)
//    {
//        finding = "";
//        if (ctx.item.aiParasite == null || !ctx.item.aiParasite.isPresent) return false;

//        // 检测难度受AI智力、腐蚀程度影响
//        float detectChance = baseDetectionChance - (ctx.item.aiParasite.intelligence * 0.05f)
//                           + (ctx.item.scans.Count * 0.1f);

//        if (Random.value < detectChance)
//        {
//            finding = $"检测到异常数字活动模式。能量签名显示非自然智能存在。";
//            ctx.item.aiParasite.discoveredByPlayer = true;
//            return true;
//        }

//        // 即使未检测到，也可能有模糊异常
//        if (Random.value < 0.15f)
//        {
//            finding = "扫描结果存在轻微数据扰动，原因不明。";
//            return true; // 标记为可疑但不确认
//        }

//        return false;
//    }
//}