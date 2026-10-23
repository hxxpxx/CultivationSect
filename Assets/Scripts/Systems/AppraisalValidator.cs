// ===== AppraisalValidator.cs =====
using System.Collections.Generic;
using UnityEngine;

public class AppraisalValidator
{
    public InspectionReport RunInspection(ItemInstance item, ClientProfile client, DayConfig day, List<AppraisalRuleBase> rules)
    {
        var ctx = new AppraisalContext { item = item, client = client, day = day };
        var report = new InspectionReport();

        foreach (var rule in rules)
        {
            if (day.dayIndex >= rule.effectiveDay)
            {
                if (rule.Evaluate(ctx, out string finding))
                {
                    report.findings.Add($"[{rule.ruleId}] {finding}");
                }
            }
        }
        return report;
    }
}