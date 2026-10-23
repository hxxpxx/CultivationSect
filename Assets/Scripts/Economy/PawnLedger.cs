// ===== PawnLedger.cs (当铺账本) =====
using System;
using System.Collections.Generic;

public class PawnLedger
{
    public List<PawnTicket> activeTickets = new List<PawnTicket>();
    public float shopFunds = 5000f;

    public PawnTicket AcceptItem(ItemInstance item, float offerAmount, DayConfig day)
    {
        if (offerAmount > shopFunds) return null;

        shopFunds -= offerAmount;
        var ticket = new PawnTicket
        {
            ticketId = Guid.NewGuid().ToString().Substring(0, 8),
            itemInstanceId = item.instanceID,
            principal = offerAmount,
            interestRate = 0.05f,
            redeemDays = 7,
            penaltyRate = 0.1f,
            acceptTime = DateTime.Now, // 游戏中可用自定义时间
            status = TicketStatus.Held
        };
        activeTickets.Add(ticket);
        return ticket;
    }

    public float CalculateRedeemCost(PawnTicket ticket)
    {
        if (ticket.status != TicketStatus.Held) return 0f;
        // 简化：按天算息
        int daysPassed = 3; // 示例，实际应从游戏时间算
        float interest = ticket.principal * ticket.interestRate * daysPassed;
        return ticket.principal + interest;
    }

    public void RedeemTicket(string ticketId)
    {
        var t = activeTickets.Find(x => x.ticketId == ticketId);
        if (t != null)
        {
            float cost = CalculateRedeemCost(t);
            shopFunds += cost; // 玩家收回本息
            t.status = TicketStatus.Redeemed;
        }
    }
}