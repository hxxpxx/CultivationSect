//// ===== RuleLegalBan.cs (禁运品) =====
//using UnityEngine;

//[CreateAssetMenu(menuName = "CultivationSectLeader/Rules/LegalBan")]
//public class RuleLegalBan : AppraisalRuleBase
//{
//    public override bool Evaluate(AppraisalContext ctx, out string finding)
//    {
//        finding = "";
//        if (ctx.item.banned || ctx.day.bannedCategories.Contains(ctx.item.def.category))
//        {
//            finding = $"品类 '{ctx.item.def.category}' 属于当日禁运品。";
//            return true;
//        }
//        return false;
//    }
//}