// ===== Inventory.cs =====
// Assets/Scripts/Inventory/Inventory.cs
// 物品容器：格子数组 + 容量 + 堆叠上限，提供增删换移分等操作。
// 支持两种语义：
//   1) 唯一物品（当铺场景，如带真伪/赃货/AI寄生状态的物品）→ 一格一份 ItemInstance；
//   2) 可堆叠商品 → AddStacked 按 itemID 合并，单格堆叠到 maxStack。
// 每次改动触发 Changed 事件，由 InventoryManager 转发为 GameEvents.OnInventoryChanged。
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Inventory
{
    public string inventoryID;      // 唯一标识（如 "counter" / "player_bag" / "warehouse"）
    public string displayName;      // 显示名（用于 UI 标题）
    public int capacity;            // 总格子数
    public int maxStack = 99;       // 单格堆叠上限（仅可堆叠商品生效）
    public List<InventorySlot> slots = new List<InventorySlot>();

    /// <summary>变化通知（InventoryManager 订阅后转发为全局事件；勿手动调用）</summary>
    [NonSerialized]
    public Action<Inventory> Changed;

    public Inventory() { }

    public Inventory(string id, string name, int capacity, int maxStack = 99)
    {
        inventoryID = id;
        displayName = name;
        this.capacity = capacity;
        this.maxStack = maxStack;
        Resize(capacity);
    }

    // ============ 容量管理 ============

    public void Resize(int newCapacity)
    {
        if (newCapacity < 0) newCapacity = 0;
        while (slots.Count < newCapacity)
            slots.Add(new InventorySlot());
        if (slots.Count > newCapacity)
        {
            for (int i = newCapacity; i < slots.Count; i++)
                slots[i].Clear();
            slots.RemoveRange(newCapacity, slots.Count - newCapacity);
        }
        capacity = newCapacity;
    }

    // ============ 查询 ============

    public int UsedSlots => slots.Count(s => !s.IsEmpty);
    public bool IsFull => UsedSlots >= capacity;
    public int FreeSlotCount => capacity - UsedSlots;
    public bool HasFreeSlot() => UsedSlots < capacity;

    public InventorySlot Get(int index)
        => index >= 0 && index < slots.Count ? slots[index] : null;

    public List<ItemInstance> GetAllItems()
        => slots.Where(s => !s.IsEmpty).Select(s => s.item).ToList();

    public bool Contains(ItemInstance item)
        => item != null && slots.Any(s => !s.IsEmpty && ReferenceEquals(s.item, item));

    /// <summary>按实例ID查找（用于当票 itemInstanceId 定位柜台物品）</summary>
    public ItemInstance FindByInstanceId(string instanceID)
    {
        if (string.IsNullOrEmpty(instanceID)) return null;
        foreach (var s in slots)
            if (!s.IsEmpty && s.item.instanceID == instanceID)
                return s.item;
        return null;
    }

    /// <summary>按实例ID移除。返回是否成功。</summary>
    public bool RemoveByInstanceId(string instanceID)
    {
        if (string.IsNullOrEmpty(instanceID)) return false;
        int idx = slots.FindIndex(s => !s.IsEmpty && s.item.instanceID == instanceID);
        if (idx < 0) return false;
        slots[idx].Clear();
        NotifyChanged();
        return true;
    }

    public int GetItemCount(string itemID)
    {
        int total = 0;
        foreach (var s in slots)
        {
            if (!s.IsEmpty && s.item.itemID == itemID)
                total += s.count;
        }
        return total;
    }

    // ============ 增 ============

    /// <summary>放入一份唯一实例到首个空位。成功返回 true。</summary>
    public bool Add(ItemInstance item)
    {
        if (item == null) return false;
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s.IsEmpty && !s.isLocked)
            {
                s.item = item;
                s.count = 1;
                NotifyChanged();
                return true;
            }
        }
        return false; // 无空位
    }

    /// <summary>按 itemID 堆叠放入 count 份（先并入同 ID 堆叠槽，再放入空位）。返回实际放入数。</summary>
    public int AddStacked(ItemInstance item, int count)
    {
        if (item == null || count <= 0) return 0;

        int added = 0;
        // 1. 合并到已有同 ID 堆叠槽
        foreach (var s in slots)
        {
            if (s.IsEmpty || s.isLocked || s.item.itemID != item.itemID) continue;
            int room = maxStack - s.count;
            if (room <= 0) continue;
            int take = Mathf.Min(room, count - added);
            s.count += take;
            added += take;
            if (added >= count) break;
        }

        // 2. 放入空位
        for (int i = 0; i < slots.Count && added < count; i++)
        {
            var s = slots[i];
            if (!s.IsEmpty || s.isLocked) continue;
            int take = Mathf.Min(maxStack, count - added);
            s.item = item.Clone();
            s.count = take;
            added += take;
        }

        if (added > 0) NotifyChanged();
        return added;
    }

    // ============ 删 ============

    /// <summary>移除指定格（count 用于可堆叠格；count>=当前堆叠数则清空整格）。</summary>
    public bool RemoveAt(int index, int count = 1)
    {
        var s = Get(index);
        if (s == null || s.IsEmpty || s.isLocked) return false;
        if (s.count > count)
        {
            s.count -= count;
            NotifyChanged();
            return true;
        }
        s.Clear();
        NotifyChanged();
        return true;
    }

    /// <summary>按实例引用移除（唯一物品）。</summary>
    public bool Remove(ItemInstance item)
    {
        if (item == null) return false;
        int idx = slots.FindIndex(s => !s.IsEmpty && ReferenceEquals(s.item, item));
        if (idx < 0) return false;
        slots[idx].Clear();
        NotifyChanged();
        return true;
    }

    /// <summary>按 itemID 移除 count 份（跨格累计）。返回实际移除数。</summary>
    public int Remove(string itemID, int count)
    {
        if (string.IsNullOrEmpty(itemID) || count <= 0) return 0;
        int removed = 0;
        for (int i = slots.Count - 1; i >= 0 && removed < count; i--)
        {
            var s = slots[i];
            if (s.IsEmpty || s.isLocked || s.item.itemID != itemID) continue;
            int take = Mathf.Min(s.count, count - removed);
            if (s.count > take) s.count -= take;
            else s.Clear();
            removed += take;
        }
        if (removed > 0) NotifyChanged();
        return removed;
    }

    // ============ 换 / 移 / 分 ============

    /// <summary>交换两格内容。</summary>
    public bool Swap(int a, int b)
    {
        if (a < 0 || b < 0 || a >= slots.Count || b >= slots.Count || a == b) return false;
        var sa = slots[a];
        var sb = slots[b];
        if (sa.isLocked || sb.isLocked) return false;

        (sa.item, sb.item) = (sb.item, sa.item);
        (sa.count, sb.count) = (sb.count, sa.count);
        NotifyChanged();
        return true;
    }

    /// <summary>把 from 格内容移动到 to 格；to 为空则放入，to 有物则交换。</summary>
    public bool Move(int from, int to)
    {
        var sFrom = Get(from);
        var sTo = Get(to);
        if (sFrom == null || sTo == null || sFrom.IsEmpty || sFrom.isLocked || sTo.isLocked) return false;

        if (sTo.IsEmpty)
        {
            sTo.item = sFrom.item;
            sTo.count = sFrom.count;
            sFrom.Clear();
        }
        else
        {
            (sFrom.item, sTo.item) = (sTo.item, sFrom.item);
            (sFrom.count, sTo.count) = (sTo.count, sFrom.count);
        }
        NotifyChanged();
        return true;
    }

    /// <summary>从堆叠格分出 amount 到首个空位。返回是否成功。</summary>
    public bool Split(int index, int amount)
    {
        var s = Get(index);
        if (s == null || s.IsEmpty || s.isLocked || amount <= 0 || amount >= s.count) return false;
        for (int i = 0; i < slots.Count; i++)
        {
            var t = slots[i];
            if (!t.IsEmpty || t.isLocked) continue;
            t.item = s.item.Clone();
            t.count = amount;
            s.count -= amount;
            NotifyChanged();
            return true;
        }
        return false;
    }

    /// <summary>清空所有格子。</summary>
    public void Clear()
    {
        foreach (var s in slots) s.Clear();
        NotifyChanged();
    }

    // ============ 存档（第 4 步：不接入已注释的 SaveManager，但做好 DTO） ============

    public SerializableInventory ToSerializable()
    {
        var data = new SerializableInventory
        {
            inventoryID = inventoryID,
            displayName = displayName,
            capacity = capacity,
            maxStack = maxStack,
        };
        foreach (var s in slots)
        {
            data.slots.Add(new SerializableInventorySlot
            {
                item = s.IsEmpty ? null : s.item.ToSerializableData(),
                count = s.count,
                isLocked = s.isLocked,
            });
        }
        return data;
    }

    public static Inventory FromSerializable(SerializableInventory data)
    {
        if (data == null) return null;
        var inv = new Inventory(data.inventoryID, data.displayName, data.capacity, data.maxStack);
        inv.slots.Clear();
        for (int i = 0; i < data.slots.Count; i++)
        {
            var ss = data.slots[i];
            if (ss == null) continue;
            inv.slots.Add(new InventorySlot
            {
                item = ss.item != null ? ItemInstance.FromSerializableData(ss.item) : null,
                count = ss.count,
                isLocked = ss.isLocked,
            });
        }
        inv.Resize(inv.capacity); // 补齐槽数
        return inv;
    }

    // ============ 内部 ============

    void NotifyChanged()
    {
        if (Changed != null)
            Changed(this);
    }
}
