// ===== ClientProfile.cs =====
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class ClientProfile
{
    public string clientId;
    public string clientName;
    public int clientPortrait;               //肖像
    public ClientFaction clientFaction;      //派系
    public ClientCareer clientCareer;        //职业
    public int clientLevel;                  //级别
    public string motive;                    //动机
    public ClientType clientType = ClientType.Seller;   // 客户类型：来卖 / 来买
    public float budget;                     // 携带资金上限（购买时受限）

    public bool wanted; 
    public bool isLying;
    public bool knowsAboutAI;                       // 客户是否知道物品里有AI
    public bool isAICarrier;                        // 客户是AI的载体/同伙
    public string dialogueText;
    public List<ItemInstance> broughtItems = new List<ItemInstance>();
}