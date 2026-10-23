// ===== InspectionFinding.cs =====
using System.Collections.Generic;

public class InspectionReport
{
    public List<string> findings = new List<string>();
    public bool HasIssue => findings.Count > 0;
}