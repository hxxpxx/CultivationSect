// ===== PawnTicket.cs =====
using System;

[Serializable]
public class PawnTicket
{
    public string ticketId;
    public string itemInstanceId;
    public float principal;          // 放款金额
    public float interestRate;        // 日利率 (如 0.05 = 5%)
    public int redeemDays;           // 赎期（天）
    public float penaltyRate;        // 逾期罚金比例
    public System.DateTime acceptTime;
    public TicketStatus status;
}