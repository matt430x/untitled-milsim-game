namespace MilSim.Entities.Units.Components;

public partial class SelectionComponent : Node2D, ISelectable
{
    [Signal] public delegate void SelectedEventHandler();
    [Signal] public delegate void DeselectedEventHandler();

    [Export] public int OwnerId { get; set; }
    [Export] private Node2D _selectionIndicator;

    public bool IsSelected { get; private set; }

    public override void _Ready()
    {
        if (_selectionIndicator != null)
            _selectionIndicator.Visible = false;
    }

    public void Select()
    {
        if (IsSelected) return;
        IsSelected = true;
        if (_selectionIndicator != null)
            _selectionIndicator.Visible = true;
        EmitSignal(SignalName.Selected);
    }

    public void Deselect()
    {
        if (!IsSelected) return;
        IsSelected = false;
        if (_selectionIndicator != null)
            _selectionIndicator.Visible = false;
        EmitSignal(SignalName.Deselected);
    }
}
