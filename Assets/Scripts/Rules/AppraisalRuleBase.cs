using System.Collections.Generic;
using UnityEngine;

public abstract class AppraisalRuleBase : ScriptableObject
{
    public string ruleId; public string notice;      // 公告文案
    public int effectiveDay = 1;
    public abstract bool Evaluate(AppraisalContext ctx, out string finding);
}

