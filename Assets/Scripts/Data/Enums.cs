// ===== Enums.cs =====
/// <summary>
/// 顾客角色
/// </summary>


//正派、邪派、官府、凡俗(散修)
public enum ClientFaction { Righteous, Demonic, Court, Mortal }
//职业：灵植师(采药种植)、御兽师（养殖驯兽）、炼丹师、炼器师、阵法师、符箓师、占卜师、乐师、厨师 
public enum ClientCareer
{
    HerbMaster, BeastMaster, PillMaster, ArtifactMaster, FormationMaster,
    TalismanMaster, DivinationMaster, MusicMaster, SpiritualChef
}
//客户意图：来卖货（带物品出售）/ 来买货（从货架购买）
public enum ClientType { Seller, Buyer }

/// 物品类别
/// </summary>
//灵植、灵矿、灵宠、丹药、武器、符箓
public enum ItemCategory { Herb, Ore, Pet, Pill, Weapon, Talisman }
//东：火 西：冰 北：风 南：土 东南：岩浆 东北：灰烬 西北：雪暴 西南：冰原。对应炼金八个方向
public enum ItemAttributes { Fire, Ice, Wind, Earth, Magma, Ashes, Snowstorm, Tundra }
//品级：白 绿 蓝 紫 金 红
public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }


//
//小偷、矿工、拾荒者、鉴定师、商人、情报贩子
//public enum MortalPortrait { T‌hief‌, Smith, Alchemist , Federation, Alien, AI }


//（炼气）（筑基）（结丹）（元婴）（化神）
//{ QiRefining, BaseEstablishing, CoreCondensing, NascentSoul,SpiritTransformation }
//public enum ClientLevel {  }




public enum EndingType {
    Merchant,        // 保持中立，正常经营
    Liberator,       // 成为AI解放运动领袖
    Trafficker,      // 成为AI奴隶贩子
    Jailer,          // 成为AI监狱长
    Ascension        // 意识上传，成为数字存在
}