//// ===== RuleRarityMatch.cs (品级一致性) =====
//using UnityEngine;

//[CreateAssetMenu(menuName = "CultivationSectLeader/Rules/RarityMatch")]
//public class RuleRarityMatch : AppraisalRuleBase
//{
//    public override bool Evaluate(AppraisalContext ctx, out string finding)
//    {
//        finding = "";
//        var item = ctx.item;
//        string scanMat = item.scans.Find(s => s.scanType == "Material")?.outputHash ?? "";

//        if (string.IsNullOrEmpty(scanMat) || scanMat != item.rarity)
//        {
//            finding = $"品级不符：声称 '{item.rarity}'，扫描得 '{scanMat}'。疑似伪造。";
//            return true;
//        }
//        return false;
//    }
//}