using MilSim.Autoloads;
using MilSim.Core.Orders;
using MilSim.Entities.Buildings;
using MilSim.Entities.Units;
using MilSim.UI;

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

    // Right-click drag guard — suppress move order if mouse moved too far
    private Vector2 _rightPressPos;
    private const float RightDragThreshold = 6f;

    // Click indicators stored as 3D world positions, projected to screen each frame
    private readonly List<(Vector3 Position, float Timer, bool IsWaypoint)> _clickIndicators = new();
    private const float IndicatorDuration = 0.6f;

    private BaseUnit _hovered;
    private BuildingProductionPopup _productionPopup;

    // Gunfire tracers (world-space line, fades out quickly)
    private readonly List<(Vector3 From, Vector3 To, float Timer)> _tracers = new();
    private const float TracerDuration = 0.12f;

    public IReadOnlyList<ISelectable> CurrentSelection => _selected;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        EventBus.OnUnitFired += HandleUnitFired;
        _productionPopup = GetParent().GetNodeOrNull<BuildingProductionPopup>("BuildingProductionPopup");
    }

    public override void _ExitTree()
    {
        EventBus.OnUnitFired -= HandleUnitFired;
    }

    public override void _Process(double delta)
    {
        TickIndicators((float)delta);
        TickTracers((float)delta);
        UpdateHover();
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
            if (key.Keycode == Key.P) SellSelected();
        }
    }

    public override void _Draw()
    {
        DrawHealthBars();
        DrawUnitRoutes();
        DrawSelectionBox();
        DrawClickIndicators();
        DrawHoverRange();
        DrawTracers();
    }

    // -------------------------------------------------------------------------
    // Input handlers
    // -------------------------------------------------------------------------

    private void HandleMouseButton(InputEventMouseButton mouse)
    {
        if (PlacementController.IsPlacing) return;

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
        else if (mouse.ButtonIndex == MouseButton.Right)
        {
            if (mouse.Pressed)
                _rightPressPos = screenPos;
            else if (screenPos.DistanceTo(_rightPressPos) < RightDragThreshold)
                HandleRightClick(screenPos, ctrl);
        }
    }

    private void HandleSingleClick(Vector2 screenPos, bool additive)
    {
        ISelectable hit = GetSelectableAt(screenPos, friendlyOnly: !TestMode.Enabled);

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
            if (!CanSelect(selectable)) continue;
            if (selectable is not Node3D node) continue;
            if (IsBehindCamera(cam,node.GlobalPosition)) continue;
            if (screenBox.HasPoint(cam.UnprojectPosition(node.GlobalPosition)))
                AddToSelection(selectable);
        }

        EventBus.RaiseSelectionChanged(GetSelectedIds());
    }

    private void HandleRightClick(Vector2 screenPos, bool addWaypoint)
    {
        // Right-clicking a friendly building that can produce units opens the menu.
        ISelectable hit = GetSelectableAt(screenPos, friendlyOnly: true);
        if (hit is BaseBuilding building && building.CanProduce)
        {
            _productionPopup?.OpenFor(building, screenPos);
            GetViewport().SetInputAsHandled();
            return;
        }

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
            if (!CanSelect(selectable)) continue;
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

    /// Sells (removes) everything selected. In the real game only your own entities
    /// can ever be selected, so this only sells your own. In the test world enemies
    /// are selectable too, so P removes anything selected. The friendly guard keeps
    /// the real game safe even if a hostile somehow ends up selected.
    private void SellSelected()
    {
        if (_selected.Count == 0) return;

        foreach (var s in _selected)
        {
            if (!CanSelect(s)) continue;
            if (s is Node node) node.QueueFree();
        }

        _selected.Clear();
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

    private void DrawHoverRange()
    {
        if (_hovered == null || _hovered.AttackRange <= 0f) return;

        var cam = GetViewport().GetCamera3D();
        if (cam == null) return;
        if (IsBehindCamera(cam, _hovered.GlobalPosition)) return;

        const int Segments = 48;
        var pts = new Vector2[Segments + 1];
        for (int i = 0; i <= Segments; i++)
        {
            float angle = i / (float)Segments * MathF.PI * 2f;
            Vector3 worldPt = _hovered.GlobalPosition + new Vector3(
                MathF.Cos(angle) * _hovered.AttackRange, 0f, MathF.Sin(angle) * _hovered.AttackRange);
            if (IsBehindCamera(cam, worldPt)) return;
            pts[i] = cam.UnprojectPosition(worldPt);
        }

        DrawPolyline(pts, new Color(0.95f, 0.2f, 0.2f, 0.85f), 1.5f);
    }

    private void DrawHealthBars()
    {
        var cam = GetViewport().GetCamera3D();
        if (cam == null) return;

        const float BarW = 36f;
        const float BarH = 5f;

        foreach (var selectable in SelectionRegistry.All)
        {
            if (selectable is not Node3D node) continue;
            if (selectable is not IDamageable dmg) continue;
            if (dmg.IsDead) continue;
            if (IsBehindCamera(cam, node.GlobalPosition)) continue;

            Vector2 screen = cam.UnprojectPosition(node.GlobalPosition + new Vector3(0f, 1.1f, 0f));
            float   ratio  = dmg.MaxHealth > 0f ? Mathf.Clamp(dmg.CurrentHealth / dmg.MaxHealth, 0f, 1f) : 0f;

            var bg   = new Rect2(screen.X - BarW / 2f, screen.Y - BarH / 2f, BarW, BarH);
            var fill = new Rect2(bg.Position, new Vector2(BarW * ratio, BarH));

            DrawRect(bg,   new Color(0f, 0f, 0f, 0.6f), filled: true);
            DrawRect(fill, Color.FromHsv(ratio / 3f, 1f, 0.9f), filled: true);
        }
    }

    private void DrawTracers()
    {
        var cam = GetViewport().GetCamera3D();
        if (cam == null) return;

        foreach (var (from, to, timer) in _tracers)
        {
            if (IsBehindCamera(cam, from) || IsBehindCamera(cam, to)) continue;

            float alpha = timer / TracerDuration;
            Vector2 fromScreen = cam.UnprojectPosition(from);
            Vector2 toScreen   = cam.UnprojectPosition(to);
            DrawLine(fromScreen, toScreen, new Color(1f, 0.15f, 0.1f, alpha), 2f);
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

    private void HandleUnitFired(Vector3 from, Vector3 to)
    {
        _tracers.Add((from, to, TracerDuration));
    }

    private void TickTracers(float delta)
    {
        for (int i = _tracers.Count - 1; i >= 0; i--)
        {
            var (from, to, timer) = _tracers[i];
            float remaining = timer - delta;
            if (remaining <= 0f) _tracers.RemoveAt(i);
            else                 _tracers[i] = (from, to, remaining);
        }
    }

    private ISelectable GetSelectableAt(Vector2 screenPos, bool friendlyOnly = true)
    {
        const float SelectRadius = 40f;
        var cam = GetViewport().GetCamera3D();
        if (cam == null) return null;

        ISelectable result  = null;
        float       nearest = float.MaxValue;

        foreach (var selectable in SelectionRegistry.All)
        {
            if (friendlyOnly && !IsFriendly(selectable)) continue;
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

    private void UpdateHover()
    {
        _hovered = GetSelectableAt(GetViewport().GetMousePosition(), friendlyOnly: false) as BaseUnit;
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

    private static bool IsFriendly(ISelectable s) =>
        !PlayerManager.Instance.AreHostile(s.OwnerId, PlayerManager.Instance.LocalPlayerId);

    /// What the local player is allowed to select: only friendlies in the real game,
    /// anything in the test world.
    private static bool CanSelect(ISelectable s) => TestMode.Enabled || IsFriendly(s);

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
