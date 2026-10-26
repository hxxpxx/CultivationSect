// ===== ClientGenerator.cs (改造版) =====
using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class ClientGenerator
{
    // 纯逻辑随机生成器（非 MonoBehaviour，可 new），无需场景组件
    public int minItemsPerClient = 1;
    public int maxItemsPerClient = 3;
    public float rareItemChance = 0.15f;

    public ClientProfile GenerateClient(int day)
    {
        var clientFaction = GenerateFaction(day);
        var clientCareer = GenerateCareer(day);
        int level = GenerateLevel(day);
        var clientType = GenerateClientType(day);

        var profile = new ClientProfile
        {
            clientId = System.Guid.NewGuid().ToString().Substring(0, 8),
            clientName = GenerateName(clientFaction, day),
            clientFaction = clientFaction,
            clientCareer = clientCareer,
            clientLevel = level,
            clientPortrait = (int)clientFaction * 16 + (int)clientCareer,
            motive = GenerateMotive(clientFaction, clientCareer, day),
            dialogueText = GenerateDialogue(clientFaction, day, clientType),
            clientType = clientType,
            // 仅 只卖/既买又卖 生成预算，其余类型不携带资金
            budget = RequiresBudget(clientType) ? GenerateBudget(clientType, day, level) : 0f,
            // 携带物品：只有会“卖”的类型（只卖/既买又卖）
            broughtItems = BringsItemsFor(clientType) ? GenerateItems(day) : new System.Collections.Generic.List<ItemInstance>(),
            //aiParasite = null  // AI系统另行处理
        };

        // 客户隐藏属性：与派系/职业联动，构成可玩的对局信息
        profile.wanted = GenerateWanted(clientFaction, clientCareer);
        profile.isLying = GenerateIsLying(clientFaction);
        profile.knowsAboutAI = GenerateKnowsAboutAI(day);
        profile.isAICarrier = false; // AI 载体由 AI 系统另行处理，此处默认非载体

        return profile;
    }

    // ===== 客户类型：六类（只卖/只买/检查人员/既买又卖/小偷/强盗） =====
    ClientType GenerateClientType(int day)
    {
        // 权重：只卖与既买又卖为交易主力，小偷/强盗较少（后期可随 day 调整）
        float[] weights = new float[]
        {
            30f,  // Seller    只卖
            25f,  // Buyer     只买
            10f,  // Inspector 检查人员
            20f,  // Both      既买又卖
            8f,   // Thief     小偷
            7f    // Robber    强盗
        };
        return (ClientType)WeightedPick(weights);
    }

    // 只有 只卖(Seller) 与 既买又卖(Both) 需要生成预算
    bool RequiresBudget(ClientType type)
        => type == ClientType.Seller || type == ClientType.Both;

    // 携带待售物品：只卖 与 既买又卖
    bool BringsItemsFor(ClientType type)
        => type == ClientType.Seller || type == ClientType.Both;

    // ===== 预算生成：携带资金有限，随天数与修为提升 =====
    float GenerateBudget(ClientType type, int day, int level)
    {
        // 基准：随天数、修为增长；既买又卖（买卖两线）资金更充裕
        float baseBudget = 250f + day * 70f + level * 40f;
        if (type == ClientType.Both) baseBudget *= 1.9f;
        return Mathf.Round(Random.Range(baseBudget * 0.75f, baseBudget * 1.5f));
    }

    // ===== 派系生成：正派 / 邪派 / 官府 / 凡俗(散修) =====
    ClientFaction GenerateFaction(int day)
    {
        // 前中期以凡俗散修为主，随天数逐步解锁官府、正派与邪派客户
        float[] weights = new float[]
        {
            Mathf.Clamp(9f - day * 0.12f, 1.5f, 9f),  // Mortal   散修：前期多，后期回落
            4f   + day * 0.06f,                        // Righteous 正派：稳定增长
            2.5f + day * 0.10f,                        // Court    官府：逐渐增多
            Mathf.Max(0.5f + day * 0.09f, 0f)          // Demonic  邪派：前期极少，后期解锁
        };
        return (ClientFaction)WeightedPick(weights);
    }

    // ===== 职业生成：九大修仙职业 =====
    ClientCareer GenerateCareer(int day)
    {
        float[] weights = new float[]
        {
            3f,   // HerbMaster       灵植师（采药种植）
            3f,   // BeastMaster      御兽师（养殖驯兽）
            2.5f, // PillMaster       炼丹师
            2.5f, // ArtifactMaster   炼器师
            2f,   // FormationMaster  阵法师
            2f,   // TalismanMaster   符箓师
            1.5f, // DivinationMaster 占卜师
            1.5f, // MusicMaster      乐师
            1.5f  // SpiritualChef    厨师
        };
        return (ClientCareer)WeightedPick(weights);
    }

    // ===== 修为境界生成（1 炼气 / 2 筑基 / 3 结丹 / 4 元婴 / 5 化神） =====
    int GenerateLevel(int day)
    {
        // 修为随天数提升：前期以炼气/筑基为主，后期逐渐出现元婴/化神
        float[] weights = new float[]
        {
            Mathf.Max(6f    - day * 0.15f, 1f),  // 炼气
            4f    + day * 0.05f,                 // 筑基
            Mathf.Max(2f    + day * 0.10f, 0f),  // 结丹
            Mathf.Max(0.6f  + day * 0.12f, 0f),  // 元婴
            Mathf.Max(0.15f + day * 0.06f, 0f)   // 化神
        };
        return WeightedPick(weights) + 1;
    }

    // ===== 动机生成（为何来典当） =====
    string GenerateMotive(ClientFaction faction, ClientCareer career, int day)
    {
        switch (faction)
        {
            case ClientFaction.Righteous: return Pick(RighteousMotives);
            case ClientFaction.Demonic:   return Pick(DemonicMotives);
            case ClientFaction.Court:     return Pick(CourtMotives);
            default:                      return Pick(MortalMotives);
        }
    }

    // ===== 姓名生成（按派系区分道号/名讳风格） =====
    string GenerateName(ClientFaction faction, int day)
    {
        switch (faction)
        {
            case ClientFaction.Righteous: return RighteousName();
            case ClientFaction.Demonic:   return DemonicName();
            case ClientFaction.Court:     return CourtName();
            default:                      return MortalName();
        }
    }

    string RighteousName()
    {
        // 道号 + 别字，偶配真人/道人等称谓
        string stem = Pick(RighteousGiven) + Pick(RighteousAlias);
        if (Random.value < 0.6f) stem += Pick(RighteousHonor);
        return stem;
    }

    string DemonicName()
    {
        string stem = Pick(DemonicGiven) + Pick(DemonicAlias);
        if (Random.value < 0.6f) stem += Pick(DemonicHonor);
        return stem;
    }

    string CourtName()
    {
        // 姓 + 官职
        return Pick(CourtSurname) + Pick(CourtTitle);
    }

    string MortalName()
    {
        // 凡俗：常见姓氏 + 通俗名讳
        return Pick(MortalSurname) + Pick(MortalGiven);
    }

    // ===== 对白生成（按派系口吻，随天数加剧张力） =====
    string GenerateDialogue(ClientFaction faction, int day, ClientType type)
    {
        // 按客户类型给对白；只卖(Seller)及无专属类型回到派系口吻
        switch (type)
        {
            case ClientType.Buyer:     return Pick(BuyerDialogue);
            case ClientType.Both:      return Pick(BothDialogue);
            case ClientType.Inspector: return Pick(InspectorDialogue);
            case ClientType.Thief:     return Pick(ThiefDialogue);
            case ClientType.Robber:    return Pick(RobberDialogue);
        }

        string line;
        switch (faction)
        {
            case ClientFaction.Righteous: line = Pick(RighteousDialogue); break;
            case ClientFaction.Demonic:   line = Pick(DemonicDialogue);   break;
            case ClientFaction.Court:     line = Pick(CourtDialogue);     break;
            default:                      line = Pick(MortalDialogue);    break;
        }

        // 后期剧情张力：邪派更凶、散修更急
        if (faction == ClientFaction.Demonic && day >= 10 && Random.value < 0.5f)
            line += " （压低声音）这是最后一次机会，别打听来历。";
        if (faction == ClientFaction.Mortal && day >= 7 && Random.value < 0.4f)
            line += " 求您行行好，俺是真的走投无路了。";

        return line;
    }

    // ===== 隐藏属性 =====
    bool GenerateWanted(ClientFaction faction, ClientCareer career)
    {
        // 邪派更易被通缉；凡俗偶有流窜之徒
        if (faction == ClientFaction.Demonic) return Random.value < 0.55f;
        if (faction == ClientFaction.Mortal)  return Random.value < 0.10f;
        return Random.value < 0.03f;
    }

    bool GenerateIsLying(ClientFaction faction)
    {
        // 邪派多谎报来路；官府/正派基本不说谎
        switch (faction)
        {
            case ClientFaction.Demonic: return Random.value < 0.60f;
            case ClientFaction.Mortal:  return Random.value < 0.20f;
            default:                    return Random.value < 0.05f;
        }
    }

    bool GenerateKnowsAboutAI(int day)
    {
        // 越后期，客户卷入/听闻 AI 寄生的概率越高（剧情线索）
        return Random.value < Mathf.Min(0.03f + day * 0.004f, 0.15f);
    }

    // ===== 通用工具 =====
    int WeightedPick(float[] weights)
    {
        float total = 0f;
        for (int i = 0; i < weights.Length; i++) total += Mathf.Max(weights[i], 0f);
        float roll = Random.Range(0f, total);
        float cum = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cum += Mathf.Max(weights[i], 0f);
            if (roll <= cum) return i;
        }
        return weights.Length - 1;
    }

    string Pick(string[] arr) => arr[Random.Range(0, arr.Length)];

    // ===== 静态数据池 =====
    static readonly string[] MortalMotives =
    {
        "家中无米下锅，急需灵石度日",
        "要为亲人抓药治病，只得典当祖产",
        "欠了外债，拿旧物来抵",
        "想盘个小摊做买卖，来换些本钱"
    };
    static readonly string[] RighteousMotives =
    {
        "外出历练盘缠耗尽，急需灵石",
        "要为门派购置灵材",
        "欲修复本命法宝，缺一笔灵石",
        "救济同门，典当随身宝物"
    };
    static readonly string[] CourtMotives =
    {
        "公务在身，顺路查看坊市行情",
        "奉命收缴违禁之物，需登记在册",
        "衙门办案，来此核对当铺账目",
        "朝廷俸禄不敷，来当些私藏"
    };
    static readonly string[] DemonicMotives =
    {
        "要脱手一批不便见光的物件",
        "筹备黑市交易，先换些灵石",
        "被人追杀，急需资金远遁",
        "夺来的赃物，找个不识货的当了"
    };

    static readonly string[] RighteousGiven = { "玄","清","云","鹤","松","静","元","真","虚","明" };
    static readonly string[] RighteousAlias = { "尘","阳","一","远","观","隐","机","台","真","元" };
    static readonly string[] RighteousHonor = { "真人","道人","居士","上人","道长","散人" };

    static readonly string[] DemonicGiven = { "血","冥","煞","幽","毒","骨","魔","鬼","噬","邪" };
    static readonly string[] DemonicAlias = { "渊","煞","殇","烬","罗","魇","魔","瞳","枯","魄" };
    static readonly string[] DemonicHonor = { "老祖","魔尊","尊者","散人","邪修","真人" };

    static readonly string[] CourtSurname = { "秦","宋","白","赵","沈","林","周","韩" };
    static readonly string[] CourtTitle = { "监察使","巡天使","戒律长老","镇魔司","提刑官","司库长老" };

    static readonly string[] MortalSurname = { "陈","李","王","张","赵","孙","周","吴","郑","钱" };
    static readonly string[] MortalGiven = { "大牛","铁柱","阿福","三娘","五郎","老根","满仓","秀娘","长生","阿旺" };

    static readonly string[] MortalDialogue =
    {
        "掌柜的，俺这是祖上传下来的老物件，你给俺个好价钱吧。",
        "家里揭不开锅了，这口旧鼎是俺娘留下的，当些灵石救急。",
        "听说你这当铺价钱公道，俺才走了三十里山路来的。",
        "掌柜的，这宝贝是俺在山上挖到的，你瞧瞧值几块灵石？"
    };
    static readonly string[] BuyerDialogue =
    {
        "掌柜的，听说你这里货色齐全，我来挑几件趁手的。",
        "我灵田缺些好东西，你店里可有灵草或矿石出售？",
        "出门历练，想添置些丹药法宝，你报个价。",
        "听闻你坊市的货最实在，我特地来采购些。"
    };
    static readonly string[] BothDialogue =
    {
        "掌柜的，我有些物件要出手，也想顺带挑几件合用的，你看着办。",
        "我这儿有几样货想脱手，你货架上若有入眼的，也一并算算。",
        "既当又买，掌柜的可得给个公道价。",
        "带来几件旧物，还想换件趁手法器，开个价吧。"
    };
    static readonly string[] InspectorDialogue =
    {
        "本官例行巡查，登记在册，你照实办理。",
        "此坊市近期或有违禁之物流通，本官特来查访。",
        "规矩如何，你比我清楚。如实呈报，莫要隐瞒。",
        "本官只查案、不做买卖。你店中账目调来一观。"
    };
    static readonly string[] ThiefDialogue =
    {
        "老板，看看这件东西……便宜收了吧，别问来路。",
        "这玩意儿顺手弄来的，你别声张，给了价我这就走。",
        "掌柜的，眼光放准些，这东西价值可不低。",
        "（眼神游移）就这件，赶紧的，别拖。"
    };
    static readonly string[] RobberDialogue =
    {
        "识相的就把好东西都拿出来，不然……哼！",
        "爷最近手头紧，你这铺子该进点贡了。",
        "把最值钱的货交出来，爷饶你这一回。",
        "别磨蹭，爷没耐心。值钱的都摆上来！"
    };
    static readonly string[] RighteousDialogue =
    {
        "贫道近日手头紧，欲将此物典当，还望道友给个公道价。",
        "此乃师门所赐之宝，若非筹集灵石，绝不轻出。",
        "奉命下山历练，盘缠不足，故来此典当一二。",
        "此物沾了尘缘，贫道不愿再用，且换些灵石罢。"
    };
    static readonly string[] DemonicDialogue =
    {
        "这物件来路不明，你莫要多问，价钱合适便成。",
        "莫声张。当个好价钱，本座自会重赏你。",
        "哼，识相的收了它；不识相的——后果你清楚。",
        "这宝贝沾了些血气，掌柜的可敢收？价高者得。"
    };
    static readonly string[] CourtDialogue =
    {
        "本官奉命巡查坊市，你如实登记，莫要藏匿违禁之物。",
        "你当铺信誉尚可。本官今日有要务，速速办妥。",
        "此物涉及朝廷卷宗，收当之后需上报备案。",
        "你当铺开在何处？若敢收赃，本官定不轻饶。"
    };

    //物品生成
    List<ItemInstance> GenerateItems(int day)
    {
        int count = Random.Range(minItemsPerClient, maxItemsPerClient + 1);
        var items = new List<ItemInstance>();

        for (int i = 0; i < count; i++)
        {
            ItemInstance item = null;

            // 稀有物品概率随天数增加
            float rareChance = rareItemChance + day * 0.02f;
            if (Random.value < rareChance)
            {
                // 稀有物品：从高价区间抽
                item = ItemFactory.Instance.CreateRandom(def =>
                {
                    float weight = 1f;
                    if (def.baseValue > 2000) weight *= 3f;
                    return weight;
                });
            }
            else
            {
                // 普通物品：正常随机
                item = ItemFactory.Instance.CreateRandom();
            }

            if (item != null)
            {
                item.owner = ItemInstance.ItemOwnership.Customer;   // 客户带来的物品归属客户
                items.Add(item);
            }
        }

        return items;
    }

}
