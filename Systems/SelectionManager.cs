using MilSim.Autoloads;
using MilSim.Core.Orders;
using MilSim.Entities.Units;

namespace MilSim.Systems;

/// <summary>
/// Handles all unit selection and order input. Runs as a full-screen Control overlay
/// in the UI CanvasLayer so it can draw in screen space while the game runs in 3D.
/// MouseFilter = Ignore so it never blocks clicks meant for the world.
/// </summary>
public partial class SelectionManager : Control
{
    private readonly List<ISelectable> _selected = new();

    // Box select (screen space)
    private Vector2 _boxStart;
    private Vector2 _boxEnd;
    private bool    _isBoxSelecting;
    private const float BoxDragThreshold = 8f;

    // Click indicators stored as 3D world positions, projected to screen each frame
    private readonly List<(Vector3 Position, float Timer, bool IsWaypoint)> _clickIndicators = new();
    private const float IndicatorDuration = 0.6f;

    public IReadOnlyList<ISelectable> CurrentSelection => _selected;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
    }

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
            _boxEnd = GetViewport().GetMousePosition();
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
        bool    shift     = mouse.ShiftPressed;
        bool    ctrl      = mouse.CtrlPressed;
        Vector2 screenPos = mouse.Position;

        if (mouse.ButtonIndex == MouseButton.Left)
        {
            if (mouse.Pressed)
            {
                _boxStart       = screenPos;
                _boxEnd         = screenPos;
                _isBoxSelecting = true;
            }
            else
            {
                _isBoxSelecting = false;
                QueueRedraw();

                bool isDrag = _boxStart.DistanceTo(_boxEnd) >= BoxDragThreshold;
                if (isDrag)
                    HandleBoxSelect(GetScreenBox(), shift);
                else
                    HandleSingleClick(screenPos, shift);
            }
        }
        else if (mouse.ButtonIndex == MouseButton.Right && mouse.Pressed)
        {
            HandleRightClick(screenPos, ctrl);
        }
    }

    private void HandleSingleClick(Vector2 screenPos, bool additive)
    {
        ISelectable hit = GetSelectableAt(screenPos);

        if (!additive) ClearSelection();

        if (hit != null)
        {
            if (additive && _selected.Contains(hit))
                RemoveFromSelection(hit);
            else
                AddToSelection(hit);
        }

        EventBus.RaiseSelectionChanged(GetSelectedIds());
    }

    private void HandleBoxSelect(Rect2 screenBox, bool additive)
    {
        if (!additive) ClearSelection();

        var cam = GetViewport().GetCamera3D();
        if (cam == null) return;

        foreach (var selectable in SelectionRegistry.All)
        {
            if (selectable is not Node3D node) continue;
            if (IsBehindCamera(cam,node.GlobalPosition)) continue;
            if (screenBox.HasPoint(cam.UnprojectPosition(node.GlobalPosition)))
                AddToSelection(selectable);
        }

        EventBus.RaiseSelectionChanged(GetSelectedIds());
    }

    private void HandleRightClick(Vector2 screenPos, bool addWaypoint)
    {
        if (_selected.Count == 0) return;

        Vector3? groundPos = ScreenToGround(screenPos);
        if (groundPos == null) return;

        var order = new MoveOrder(groundPos.Value);

        foreach (var selectable in _selected)
        {
            if (selectable is not IOrderReceiver receiver) continue;
            if (addWaypoint) receiver.QueueOrder(order);
            else             receiver.IssueOrder(order);
        }

        _clickIndicators.Add((groundPos.Value, IndicatorDuration, addWaypoint));
        QueueRedraw();
    }

    private void SelectAllInView()
    {
        var cam    = GetViewport().GetCamera3D();
        var screen = GetViewport().GetVisibleRect();
        if (cam == null) return;

        ClearSelection();
        foreach (var selectable in SelectionRegistry.All)
        {
            if (selectable is not Node3D node) continue;
            if (IsBehindCamera(cam,node.GlobalPosition)) continue;
            if (screen.HasPoint(cam.UnprojectPosition(node.GlobalPosition)))
                AddToSelection(selectable);
        }
        EventBus.RaiseSelectionChanged(GetSelectedIds());
    }

    private void DeselectAll()
    {
        ClearSelection();
        EventBus.RaiseSelectionChanged(new List<int>());
    }

    // -------------------------------------------------------------------------
    // Drawing (screen space)
    // -------------------------------------------------------------------------

    private void DrawSelectionBox()
    {
        if (!_isBoxSelecting) return;
        Rect2 box = GetScreenBox();
        DrawRect(box, new Color(0.2f, 0.85f, 0.2f, 0.12f), filled: true);
        DrawRect(box, new Color(0.2f, 0.85f, 0.2f, 0.90f), filled: false, width: 1.5f);
    }

    private void DrawUnitRoutes()
    {
        var cam = GetViewport().GetCamera3D();
        if (cam == null) return;

        foreach (var selectable in _selected)
        {
            if (selectable is not BaseUnit unit) continue;
            var route = unit.GetRoute();
            if (route.Count == 0) continue;
            if (IsBehindCamera(cam,unit.GlobalPosition)) continue;

            Vector2 from = cam.UnprojectPosition(unit.GlobalPosition);
            for (int i = 0; i < route.Count; i++)
            {
                if (IsBehindCamera(cam,route[i])) break;
                Vector2 to    = cam.UnprojectPosition(route[i]);
                Color   color = i == 0
                    ? new Color(0.2f, 0.85f, 0.2f, 0.75f)
                    : new Color(1f,   0.85f, 0f,   0.75f);
                DrawDashedLine(from, to, color);
                from = to;
            }
        }
    }

    private void DrawClickIndicators()
    {
        var cam = GetViewport().GetCamera3D();
        if (cam == null) return;

        foreach (var (pos, timer, isWaypoint) in _clickIndicators)
        {
            if (IsBehindCamera(cam,pos)) continue;
            Vector2 sp    = cam.UnprojectPosition(pos);
            float   alpha = timer / IndicatorDuration;
            Color   color = isWaypoint
                ? new Color(1f, 0.85f, 0f, alpha)
                : new Color(0.2f, 0.85f, 0.2f, alpha);

            DrawArc(sp, 14f, 0f, MathF.PI * 2f, 20, color, 2f);
            DrawLine(sp + new Vector2(-5, 0), sp + new Vector2(5,  0), color, 1.5f);
            DrawLine(sp + new Vector2(0, -5), sp + new Vector2(0,  5), color, 1.5f);
        }
    }

    private void DrawDashedLine(Vector2 from, Vector2 to, Color color,
        float dashLen = 10f, float gapLen = 7f, float width = 1.5f)
    {
        float   total    = from.DistanceTo(to);
        if (total < 1f) return;
        Vector2 dir      = (to - from) / total;
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
        for (int i = _clickIndicators.Count - 1; i >= 0; i--)
        {
            var (pos, timer, isWaypoint) = _clickIndicators[i];
            float remaining = timer - delta;
            if (remaining <= 0f) _clickIndicators.RemoveAt(i);
            else                 _clickIndicators[i] = (pos, remaining, isWaypoint);
        }
    }

    private ISelectable GetSelectableAt(Vector2 screenPos)
    {
        const float SelectRadius = 40f;
        var cam = GetViewport().GetCamera3D();
        if (cam == null) return null;

        ISelectable result  = null;
        float       nearest = float.MaxValue;

        foreach (var selectable in SelectionRegistry.All)
        {
            if (selectable is not Node3D node) continue;
            if (IsBehindCamera(cam,node.GlobalPosition)) continue;

            float dist = cam.UnprojectPosition(node.GlobalPosition).DistanceTo(screenPos);
            if (dist < SelectRadius && dist < nearest)
            {
                nearest = dist;
                result  = selectable;
            }
        }
        return result;
    }

    /// Casts a ray from the camera through screenPos and returns where it hits Y=0.
    private Vector3? ScreenToGround(Vector2 screenPos)
    {
        var cam = GetViewport().GetCamera3D();
        if (cam == null) return null;

        Vector3 origin = cam.ProjectRayOrigin(screenPos);
        Vector3 dir    = cam.ProjectRayNormal(screenPos);

        if (Mathf.Abs(dir.Y) < 0.001f) return null;
        float t = -origin.Y / dir.Y;
        if (t < 0) return null;
        return origin + dir * t;
    }

    // In camera local space the lens faces -Z; points with local.Z > 0 are behind the camera.
    private static bool IsBehindCamera(Camera3D cam, Vector3 worldPos) =>
        (cam.GlobalTransform.AffineInverse() * worldPos).Z > 0f;

    private Rect2 GetScreenBox() => new Rect2(_boxStart, _boxEnd - _boxStart).Abs();

    private void AddToSelection(ISelectable s)
    {
        if (_selected.Contains(s)) return;
        _selected.Add(s);
        s.Select();
    }

    private void RemoveFromSelection(ISelectable s)
    {
        _selected.Remove(s);
        s.Deselect();
    }

    private void ClearSelection()
    {
        foreach (var s in _selected) s.Deselect();
        _selected.Clear();
    }

    private List<int> GetSelectedIds()
    {
        var ids = new List<int>();
        foreach (var s in _selected)
            if (s is Node n) ids.Add((int)n.GetInstanceId());
        return ids;
    }
}
