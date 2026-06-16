using MilSim.Autoloads;
using MilSim.Core.Orders;
using MilSim.Entities.Units;

namespace MilSim.Systems;

public partial class SelectionManager : Node2D
{
    private readonly List<ISelectable> _selected = new();

    // Box select state (world space)
    private Vector2 _boxStart;
    private Vector2 _boxEnd;
    private bool _isBoxSelecting;
    private const float BoxDragThreshold = 8f;

    // Click indicators — drawn in world space, fade out over time
    private readonly List<(Vector2 Position, float Timer, bool IsWaypoint)> _clickIndicators = new();
    private const float IndicatorDuration = 0.6f;
    private const float IndicatorRadius   = 14f;

    public IReadOnlyList<ISelectable> CurrentSelection => _selected;

    public override void _Process(double delta)
    {
        TickIndicators((float)delta);

        QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse)
            HandleMouseButton(mouse);
        else if (@event is InputEventMouseMotion && _isBoxSelecting)
        {
            _boxEnd = GetGlobalMousePosition();
            QueueRedraw();
        }
        else if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.F) SelectAllInView();
            if (key.Keycode == Key.C) DeselectAll();
        }
    }

    public override void _Draw()
    {
        DrawUnitRoutes();
        DrawSelectionBox();
        DrawClickIndicators();
    }

    // -------------------------------------------------------------------------
    // Input handlers
    // -------------------------------------------------------------------------

    private void HandleMouseButton(InputEventMouseButton mouse)
    {
        bool shift = mouse.ShiftPressed;
        bool ctrl  = mouse.CtrlPressed;

        if (mouse.ButtonIndex == MouseButton.Left)
        {
            if (mouse.Pressed)
            {
                _boxStart = GetGlobalMousePosition();
                _boxEnd   = _boxStart;
                _isBoxSelecting = true;
            }
            else
            {
                _isBoxSelecting = false;
                QueueRedraw();

                bool isDrag = _boxStart.DistanceTo(_boxEnd) >= BoxDragThreshold;
                if (isDrag)
                    HandleBoxSelect(GetWorldBox(), shift);
                else
                    HandleSingleClick(GetGlobalMousePosition(), shift);
            }
        }
        else if (mouse.ButtonIndex == MouseButton.Right && mouse.Pressed)
        {
            HandleRightClick(GetGlobalMousePosition(), ctrl);
        }
    }

    private void HandleSingleClick(Vector2 worldPos, bool additive)
    {
        ISelectable hit = GetSelectableAt(worldPos);

        if (!additive)
            ClearSelection();

        if (hit != null)
        {
            if (additive && _selected.Contains(hit))
                RemoveFromSelection(hit);
            else
                AddToSelection(hit);
        }

        EventBus.RaiseSelectionChanged(GetSelectedIds());
    }

    private void HandleBoxSelect(Rect2 worldBox, bool additive)
    {
        if (!additive)
            ClearSelection();

        foreach (var selectable in SelectionRegistry.All)
            if (selectable is Node2D node && worldBox.HasPoint(node.GlobalPosition))
                AddToSelection(selectable);

        EventBus.RaiseSelectionChanged(GetSelectedIds());
    }

    private void HandleRightClick(Vector2 worldPos, bool addWaypoint)
    {
        if (_selected.Count == 0) return;

        var order = new MoveOrder(worldPos);

        foreach (var selectable in _selected)
        {
            if (selectable is not IOrderReceiver receiver) continue;

            if (addWaypoint)
                receiver.QueueOrder(order);
            else
                receiver.IssueOrder(order);
        }

        _clickIndicators.Add((worldPos, IndicatorDuration, addWaypoint));
        QueueRedraw();
    }

    private void DeselectAll()
    {
        ClearSelection();
        EventBus.RaiseSelectionChanged(new List<int>());
    }

    private void SelectAllInView()
    {
        Rect2 viewRect = GetCameraWorldRect();
        ClearSelection();

        foreach (var selectable in SelectionRegistry.All)
            if (selectable is Node2D node && viewRect.HasPoint(node.GlobalPosition))
                AddToSelection(selectable);

        EventBus.RaiseSelectionChanged(GetSelectedIds());
    }

    // -------------------------------------------------------------------------
    // Drawing
    // -------------------------------------------------------------------------

    private void DrawSelectionBox()
    {
        if (!_isBoxSelecting) return;

        Rect2 box = GetWorldBox();
        DrawRect(box, new Color(0.2f, 0.85f, 0.2f, 0.12f), filled: true);
        DrawRect(box, new Color(0.2f, 0.85f, 0.2f, 0.9f),  filled: false, width: 1.5f);
    }

    private void DrawClickIndicators()
    {
        foreach (var (pos, timer, isWaypoint) in _clickIndicators)
        {
            float alpha = timer / IndicatorDuration;
            Color color = isWaypoint
                ? new Color(1f, 0.85f, 0f, alpha)  // yellow for waypoints
                : new Color(0.2f, 0.85f, 0.2f, alpha); // green for immediate moves

            DrawArc(pos, IndicatorRadius, 0f, MathF.PI * 2f, 20, color, 2f);

            // Small cross at center
            DrawLine(pos + new Vector2(-5, 0), pos + new Vector2(5, 0), color, 1.5f);
            DrawLine(pos + new Vector2(0, -5), pos + new Vector2(0, 5), color, 1.5f);
        }
    }

    private void DrawUnitRoutes()
    {
        foreach (var selectable in _selected)
        {
            if (selectable is not BaseUnit unit) continue;

            var route = unit.GetRoute();
            if (route.Count == 0) continue;

            Vector2 from = unit.GlobalPosition;
            for (int i = 0; i < route.Count; i++)
            {
                // Green to the current move target, yellow through waypoints
                Color color = i == 0
                    ? new Color(0.2f, 0.85f, 0.2f, 0.75f)
                    : new Color(1f, 0.85f, 0f, 0.75f);
                DrawDashedLine(from, route[i], color);
                from = route[i];
            }
        }
    }

    private void DrawDashedLine(Vector2 from, Vector2 to, Color color,
        float dashLen = 10f, float gapLen = 7f, float width = 1.5f)
    {
        float total = from.DistanceTo(to);
        if (total < 1f) return;

        Vector2 dir     = (to - from) / total;
        float   traveled = 0f;
        bool    isDash   = true;

        while (traveled < total)
        {
            float segEnd = Math.Min(traveled + (isDash ? dashLen : gapLen), total);
            if (isDash)
                DrawLine(from + dir * traveled, from + dir * segEnd, color, width);
            traveled = segEnd;
            isDash   = !isDash;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void TickIndicators(float delta)
    {
        bool dirty = false;
        for (int i = _clickIndicators.Count - 1; i >= 0; i--)
        {
            var (pos, timer, isWaypoint) = _clickIndicators[i];
            float remaining = timer - delta;
            if (remaining <= 0f)
            {
                _clickIndicators.RemoveAt(i);
                dirty = true;
            }
            else
            {
                _clickIndicators[i] = (pos, remaining, isWaypoint);
                dirty = true;
            }
        }
        if (dirty) QueueRedraw();
    }

    private ISelectable GetSelectableAt(Vector2 worldPos)
    {
        const float SelectRadius = 32f;
        float nearest = float.MaxValue;
        ISelectable result = null;

        foreach (var selectable in SelectionRegistry.All)
        {
            if (selectable is not Node2D node) continue;
            float dist = node.GlobalPosition.DistanceTo(worldPos);
            if (dist < SelectRadius && dist < nearest)
            {
                nearest = dist;
                result  = selectable;
            }
        }
        return result;
    }

    private Rect2 GetWorldBox() =>
        new Rect2(_boxStart, _boxEnd - _boxStart).Abs();

    private Rect2 GetCameraWorldRect()
    {
        var camera   = GetViewport().GetCamera2D();
        Vector2 size = GetViewport().GetVisibleRect().Size / camera.Zoom;
        return new Rect2(camera.GlobalPosition - size * 0.5f, size);
    }

    private void AddToSelection(ISelectable selectable)
    {
        if (_selected.Contains(selectable)) return;
        _selected.Add(selectable);
        selectable.Select();
    }

    private void RemoveFromSelection(ISelectable selectable)
    {
        _selected.Remove(selectable);
        selectable.Deselect();
    }

    private void ClearSelection()
    {
        foreach (var s in _selected)
            s.Deselect();
        _selected.Clear();
    }

    private List<int> GetSelectedIds()
    {
        var ids = new List<int>();
        foreach (var s in _selected)
            if (s is Node n)
                ids.Add((int)n.GetInstanceId());
        return ids;
    }
}
