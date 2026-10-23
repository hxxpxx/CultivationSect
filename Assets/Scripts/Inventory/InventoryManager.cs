// ===== InventoryManager.cs =====
// Assets/Scripts/Inventory/InventoryManager.cs
// 物品仓库服务（纯 C# 单例，无需挂载 GameObject）：
//   持有多个 Inventory（柜台/背包/仓库…），按 inventoryID 索引；
//   自动把每个 Inventory 的变化转发为 GameEvents.OnInventoryChanged(inventoryID)。
//
// 用法：
//   var counter = InventoryManager.Instance.GetOrCreate("counter", "柜台", 24);
//   InventoryManager.Instance.Get("player_bag").Add(item);
// 注意：直接 new Inventory(...) 创建的容器不会自动广播，请用 Register 注册。
using System.Collections.Generic;

public class InventoryManager
{
    public static InventoryManager Instance { get; } = new InventoryManager();

    readonly Dictionary<string, Inventory> inventories = new Dictionary<string, Inventory>();

    public Inventory Get(string id)
    {
        inventories.TryGetValue(id, out var inv);
        return inv;
    }

    /// <summary>取容器；不存在则创建（自动注册事件转发）。</summary>
    public Inventory GetOrCreate(string id, string name, int capacity, int maxStack = 99)
    {
        if (inventories.TryGetValue(id, out var existing))
            return existing;
        var inv = new Inventory(id, name, capacity, maxStack);
        Register(inv);
        return inv;
    }

    /// <summary>注册一个已创建（或反序列化得到的）容器，并挂接事件转发。</summary>
    public void Register(Inventory inv)
    {
        if (inv == null || string.IsNullOrEmpty(inv.inventoryID)) return;
        inventories[inv.inventoryID] = inv;
        Wire(inv);
    }

    public bool Unregister(string id)
    {
        if (!inventories.TryGetValue(id, out var inv)) return false;
        inv.Changed -= OnInventoryChangedForward;
        inventories.Remove(id);
        return true;
    }

    public void ClearAll()
    {
        foreach (var inv in inventories.Values)
            inv.Changed -= OnInventoryChangedForward;
        inventories.Clear();
    }

    public IEnumerable<Inventory> GetAll() => inventories.Values;

    void Wire(Inventory inv)
    {
        inv.Changed -= OnInventoryChangedForward; // 防重复订阅
        inv.Changed += OnInventoryChangedForward;
    }

    void OnInventoryChangedForward(Inventory inv)
        => GameEvents.SafeInvoke(GameEvents.OnInventoryChanged, inv.inventoryID);
}
