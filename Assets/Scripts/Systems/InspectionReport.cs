// ===== InspectionFinding.cs =====
using System.Collections.Generic;

public class InspectionReport
{
    public List<string> findings = new List<string>();
    public bool HasIssue => findings.Count > 0;
    public bool scanned;   // 是否已完成扫描鉴定（false = 仅标准估价，尚未扫描）
}