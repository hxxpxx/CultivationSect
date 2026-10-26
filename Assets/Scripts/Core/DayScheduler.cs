// ===== DayScheduler.cs =====
using System.Collections.Generic;
using UnityEngine;

public class DayScheduler
{
    public DayConfig currentDayConfig;
    public List<ClientProfile> todayClients = new List<ClientProfile>();
    private ClientGenerator generator = new ClientGenerator();

    public void StartDay(int day)
    {

        int clientCount = 5 + currentDayConfig.dayIndex * 2;
        todayClients.Clear();
        for (int i = 0; i < clientCount; i++)
        {
            todayClients.Add(generator.GenerateClient(day));
        }
        Debug.Log($"第 {currentDayConfig.dayIndex} 天开始，共 {clientCount} 位客户。");
    }
}