//// ===== RuleEnergyHazard.cs (能量危险) =====
//using UnityEngine;

//[CreateAssetMenu(menuName = "CultivationSectLeader/Rules/EnergyHazard")]
//public class RuleEnergyHazard : AppraisalRuleBase
//{
//    public float hazardThreshold = 0.7f;
//    public override bool Evaluate(AppraisalContext ctx, out string finding)
//    {
//        finding = "";
//        float maxHazard = 0f;
//        foreach (var s in ctx.item.scans) if (s.hazardLevel > maxHazard) maxHazard = s.hazardLevel;

//        if (maxHazard > hazardThreshold && !ctx.item.certs.Exists(c => c.certType == "Safety" && c.isValid))
//        {
//            finding = $"能量危险值 {maxHazard} 超标且未持安全证书。";
//            return true;
//        }
//        return false;
//    }
//}