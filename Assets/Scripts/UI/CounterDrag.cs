// ===== CounterDrag.cs =====
// 柜台 / 窗口 拖拽系统
//   - DraggableItemButton：客户物品按钮可被拖拽（跟随指针）。
//   - ItemDropZone：放置区域（窗口 / 柜台），物品被拖到此处时请求移动到对应区域。
//   - CounterDrag：静态会话，记录当前拖拽的物品 + 由 UI 注册的移动回调。
//
// 使用方式（PawnDeskUI 已接入）：
//   物品按钮 AddComponent<DraggableItemButton>() 并赋值 item；
//   放置区域 AddComponent<ItemDropZone>() 并赋值 targetPlacement；
//   CounterDrag.RequestMove = (item, target) => { 调用 manager 移动逻辑; };
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>当前拖拽会话的静态信息。</summary>
public static class CounterDrag
{
    /// <summary>当前被拖拽的物品（非拖拽期为 null）。</summary>
    public static ItemInstance Dragging;

    /// <summary>拖拽期间被临时停用的滚动列表（避免 ScrollRect 抢走拖拽）。</summary>
    public static ScrollRect BlockedScrollRect;

    /// <summary>由 UI 注册：请求把 item 移动到 target 区域。返回是否接受。</summary>
    public static System.Func<ItemInstance, ItemInstance.ItemPlacement, bool> RequestMove;

    /// <summary>由 UI 注册：请求把 item 放入柜台指定格位 cellIndex。返回是否接受。</summary>
    public static System.Func<ItemInstance, int, bool> RequestMoveToCell;

    /// <summary>恢复被停用的滚动列表（拖拽结束/放下后调用，防丢失）。</summary>
    public static void ReleaseBlockedScrollRect()
    {
        if (BlockedScrollRect != null) BlockedScrollRect.enabled = true;
        BlockedScrollRect = null;
    }
}

/// <summary>可拖拽的物品按钮（点击选中由 Button 的 onClick 负责，此处只处理拖拽）。</summary>
[RequireComponent(typeof(Button))]
public class DraggableItemButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemInstance item;

    Transform originalParent;
    int originalSibling;
    bool dragging;
    Canvas rootCanvas;
    Graphic[] graphics;
    ScrollRect scrollRect;   // 所在滚动列表（若物品在 ScrollRect 内）

    // 记录松手前的原始布局，归位时复位，避免保留拖放坐标导致物品卡在落点
    Vector2 origAnchorMin, origAnchorMax, origOffsetMin, origOffsetMax;

    void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        graphics = GetComponentsInChildren<Graphic>(true);
        scrollRect = GetComponentInParent<ScrollRect>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;

        CounterDrag.Dragging = item;
        dragging = true;
        originalParent = transform.parent;
        originalSibling = transform.GetSiblingIndex();
        var rt0 = transform as RectTransform;
        if (rt0 != null)
        {
            origAnchorMin = rt0.anchorMin;
            origAnchorMax = rt0.anchorMax;
            origOffsetMin = rt0.offsetMin;
            origOffsetMax = rt0.offsetMax;
        }

        // 若物品在滚动列表内：拖拽期间临时停用，避免 ScrollRect 抢走拖拽变成滚动
        if (scrollRect != null)
        {
            scrollRect.enabled = false;
            CounterDrag.BlockedScrollRect = scrollRect;
        }

        // 拖拽期间提升到根 Canvas 下，便于自由跟随指针；并关闭自身射线检测，让下方区域接收 Drop
        if (rootCanvas != null) transform.SetParent(rootCanvas.transform, true);
        SetGraphicsRaycast(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        // 兼容 Overlay / 世界空间 Canvas
        if (rootCanvas != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                eventData.position, rootCanvas.worldCamera, out Vector2 local);
            (transform as RectTransform).anchoredPosition = local;
        }
        else
        {
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;

        SetGraphicsRaycast(true);
        CounterDrag.ReleaseBlockedScrollRect();   // 恢复滚动列表

        // 视觉归位（真正的移动结果由 UI 在 RequestMove 后刷新重建）
        if (originalParent != null)
        {
            // 用 false 保持本地变换，再复位为拖拽前的原始布局：
            // 若用 true(保留世界坐标)且物品是格子 cell 的子物体，重加回格子不会触发
            // GridLayoutGroup 重排，物品会带着落点坐标卡在原地（例如卡在货架上）。
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSibling);
            var rt = transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = origAnchorMin;
                rt.anchorMax = origAnchorMax;
                rt.offsetMin = origOffsetMin;
                rt.offsetMax = origOffsetMax;
            }
        }

        CounterDrag.Dragging = null;
    }

    void SetGraphicsRaycast(bool on)
    {
        if (graphics == null) return;
        foreach (var g in graphics)
            if (g != null) g.raycastTarget = on;
    }
}

/// <summary>放置区域：物品被拖到此处时请求移动到 targetPlacement 对应的区域。</summary>
public class ItemDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public ItemInstance.ItemPlacement targetPlacement;
    public int cellIndex = -1;   // 指定落格位（>=0 时按格放置）；-1=面板级（整块区域）

    Image bg;
    Color normalColor;

    void Awake()
    {
        bg = GetComponent<Image>();
        normalColor = bg != null ? bg.color : Color.white;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var item = CounterDrag.Dragging;
        bool handled = false;

        if (item != null)
        {
            // 按格落点：优先走格位回调（如放入柜台指定格）
            if (cellIndex >= 0 && CounterDrag.RequestMoveToCell != null)
                handled = CounterDrag.RequestMoveToCell(item, cellIndex);
            else if (CounterDrag.RequestMove != null)
                handled = CounterDrag.RequestMove(item, targetPlacement);
            else
                Debug.LogWarning($"[拖拽] RequestMove 未注册或物品为空，无法移动！RequestMove为空: {CounterDrag.RequestMove == null}");
        }
        else
        {
            Debug.LogWarning("[拖拽] Dragging 为空，无法移动。");
        }

        // 放下后立刻恢复滚动列表（物品若已移动，其按钮会被 UI 刷新销毁，OnEndDrag 不再触发）
        CounterDrag.ReleaseBlockedScrollRect();

        SetHighlight(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CounterDrag.Dragging != null) SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData) => SetHighlight(false);

    void SetHighlight(bool on)
    {
        if (bg == null) return;
        bg.color = on ? new Color(0.3f, 1f, 0.3f, 0.45f) : normalColor;
    }
}
