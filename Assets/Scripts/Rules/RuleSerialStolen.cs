//// ===== RuleSerialStolen.cs (追缴库) =====
//using UnityEngine;

//[CreateAssetMenu(menuName = "CultivationSectLeader/Rules/SerialStolen")]
//public class RuleSerialStolen : AppraisalRuleBase
//{
//    public override bool Evaluate(AppraisalContext ctx, out string finding)
//    {
//        finding = "";
//        if (ctx.item.isStolen && !string.IsNullOrEmpty(ctx.item.serialNumber))
//        {
//            finding = $"序列号 {ctx.item.serialNumber} 已列入星系追缴名单！";
//            return true;
//        }
//        return false;
//    }
//}