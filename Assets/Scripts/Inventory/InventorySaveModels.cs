// ===== InventorySaveModels.cs =====
// Assets/Scripts/Inventory/InventorySaveModels.cs
// 背包存档 DTO：纯值类型，配合 ItemInstance.ToSerializableData/FromSerializableData。
// 当前 SaveManager 尚未启用（源码被注释），此处先提供可序列化模型，供未来存档接入。
using System;
using System.Collections.Generic;

[Serializable]
public class SerializableInventorySlot
{
    public SerializableItemInstance item;   // 空槽 = null
    public int count = 1;
    public bool isLocked;
}

[Serializable]
public class SerializableInventory
{
    public string inventoryID;
    public string displayName;
    public int capacity;
    public int maxStack = 99;
    public List<SerializableInventorySlot> slots = new List<SerializableInventorySlot>();
}
