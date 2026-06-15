using MilSim.Autoloads;

namespace MilSim.Systems;

/// <summary>
/// Handles all unit/building selection: single click, shift-click, and box select.
/// Issues orders to the current selection on right-click.
/// Lives in the game scene — one instance per match.
/// </summary>
public partial class SelectionManager : Node2D
{
    private readonly List<ISelectable> _selected = new();
    private Vector2 _boxStart;
    private bool _isBoxSelecting;
    private Camera2D _camera;

    public IReadOnlyList<ISelectable> CurrentSelection => _selected;

    public override void _Ready()
    {
        _camera = GetViewport().GetCamera2D();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse)
            HandleMouseButton(mouse);
        else if (@event is InputEventMouseMotion motion && _isBoxSelecting)
            QueueRedraw();
    }

    public override void _Draw()
    {
        if (!_isBoxSelecting) return;

        Vector2 mousePos = GetViewport().GetMousePosition();
        Rect2 box = new Rect2(_boxStart, mousePos - _boxStart).Abs();
        DrawRect(box, new Color(0f, 1f, 0f, 0.2f), filled: true);
        DrawRect(box, new Color(0f, 1f, 0f, 0.8f), filled: false);
    }

    private void HandleMouseButton(InputEventMouseButton mouse)
    {
        if (mouse.ButtonIndex == MouseButton.Left)
        {
            if (mouse.Pressed)
            {
                _boxStart = mouse.Position;
                _isBoxSelecting = true;
            }
            else
            {
                _isBoxSelecting = false;
                QueueRedraw();

                Vector2 end = mouse.Position;
                Rect2 box = new Rect2(_boxStart, end - _boxStart).Abs();
                bool isClick = box.Size.Length() < 5f;

                if (isClick)
                    HandleSingleClick(mouse.Position, mouse.ShiftPressed);
                else
                    HandleBoxSelect(box, mouse.ShiftPressed);
            }
        }
        else if (mouse.ButtonIndex == MouseButton.Right && mouse.Pressed)
        {
            HandleRightClick(mouse.Position);
        }
    }

    private void HandleSingleClick(Vector2 screenPos, bool additive)
    {
        ISelectable hit = GetSelectableAt(screenPos);

        if (!additive)
            ClearSelection();

        if (hit != null)
            AddToSelection(hit);

        EventBus.RaiseSelectionChanged(GetSelectedIds());
    }

    private void HandleBoxSelect(Rect2 screenBox, bool additive)
    {
        if (!additive)
            ClearSelection();

        foreach (var selectable in GetSelectionInBox(screenBox))
            AddToSelection(selectable);

        EventBus.RaiseSelectionChanged(GetSelectedIds());
    }

    private void HandleRightClick(Vector2 screenPos)
    {
        if (_selected.Count == 0) return;
        // Order issuing will be wired in once we have concrete order types
    }

    private void AddToSelection(ISelectable selectable)
    {
        if (_selected.Contains(selectable)) return;
        _selected.Add(selectable);
        selectable.Select();
    }

    private void ClearSelection()
    {
        foreach (var s in _selected)
            s.Deselect();
        _selected.Clear();
    }

    private ISelectable GetSelectableAt(Vector2 screenPos)
    {
        // Implemented once entities have Area2D hit areas
        return null;
    }

    private IEnumerable<ISelectable> GetSelectionInBox(Rect2 screenBox)
    {
        // Implemented once entities are in the scene tree
        return Array.Empty<ISelectable>();
    }

    private List<int> GetSelectedIds()
    {
        var ids = new List<int>();
        foreach (var s in _selected)
            if (s is Node n)
                ids.Add(n.GetInstanceId().ToString().GetHashCode());
        return ids;
    }
}
