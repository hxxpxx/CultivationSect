// ===== InventorySlot.cs =====
// Assets/Scripts/Inventory/InventorySlot.cs
// 背包格子：一格可放一个唯一物品实例，或一组可堆叠商品（item + count）
using System;

[Serializable]
public class InventorySlot
{
    public ItemInstance item;   // 非空 = 有物品；唯一实例一格一份
    public int count = 1;       // 堆叠数（可堆叠商品用）；唯一物品恒为 1
    public bool isLocked;       // 锁定格不可放入/移除/交换

    public bool IsEmpty => item == null;

    public void Clear()
    {
        item = null;
        count = 0;
    }
}
