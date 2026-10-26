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
    public float budget;                     // 携带资金上限（仅 只卖/既买又卖 生成预算）

    // ===== 派生便捷属性 =====
    /// <summary>该客户是否会购买（可从货架购买）：只买 / 既买又卖</summary>
    public bool CanBuy => clientType == ClientType.Buyer || clientType == ClientType.Both;
    /// <summary>该客户是否会出售（携带物品来卖）：只卖 / 既买又卖</summary>
    public bool CanSell => clientType == ClientType.Seller || clientType == ClientType.Both;
    /// <summary>是否携带待售物品</summary>
    public bool BringsItems => CanSell;
    /// <summary>客户类型中文标签</summary>
    public string TypeLabel
    {
        get
        {
            switch (clientType)
            {
                case ClientType.Seller:    return "只卖";
                case ClientType.Buyer:     return "只买";
                case ClientType.Inspector: return "检查人员";
                case ClientType.Both:      return "既买又卖";
                case ClientType.Thief:     return "小偷";
                case ClientType.Robber:    return "强盗";
                default:                   return "未知";
            }
        }
    }

    public bool wanted; 
    public bool isLying;
    public bool knowsAboutAI;                       // 客户是否知道物品里有AI
    public bool isAICarrier;                        // 客户是AI的载体/同伙
    public string dialogueText;
    public List<ItemInstance> broughtItems = new List<ItemInstance>();
}