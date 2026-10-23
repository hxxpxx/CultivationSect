// ===== AppraisalContext.cs =====
using System.Collections.Generic;

public class AppraisalContext
{
    public ItemInstance item;
    public ClientProfile client;
    public DayConfig day;
    public List<string> findings = new List<string>();
}