// ===== RedemptionScheduler.cs =====
using System.Collections.Generic;
using UnityEngine;

public class RedemptionScheduler
{
    public void TickDay(PawnLedger ledger, DayConfig day)
    {
        foreach (var ticket in ledger.activeTickets)
        {
            if (ticket.status == TicketStatus.Held)
            {
                // 简化：超过 redeemDays 转绝当
                if (Random.value < 0.1f) // 模拟客户放弃赎回
                {
                    ticket.status = TicketStatus.Defaulted;
                    ledger.shopFunds += ticket.principal * 0.2f; // 绝当处理收回少量
                    Debug.Log($"当票 {ticket.ticketId} 已绝当，物品归当铺所有。");
                }
            }
        }
    }
}