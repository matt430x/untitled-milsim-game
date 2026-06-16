namespace MilSim.Entities.Units.Components;

public partial class SelectionComponent : Node3D, ISelectable
{
    [Signal] public delegate void SelectedEventHandler();
    [Signal] public delegate void DeselectedEventHandler();

    [Export] public int OwnerId { get; set; }

    public bool IsSelected { get; private set; }

    private MeshInstance3D _ring;

    public override void _Ready()
    {
        _ring = new MeshInstance3D();

        var torus = new TorusMesh
        {
            InnerRadius  = 0.42f,
            OuterRadius  = 0.55f,
            Rings        = 32,
            RingSegments = 8,
        };

        var mat = new StandardMaterial3D
        {
            AlbedoColor              = new Color(0.2f, 0.9f, 0.2f),
            EmissionEnabled          = true,
            Emission                 = new Color(0.2f, 0.9f, 0.2f),
            EmissionEnergyMultiplier = 0.6f,
        };
        torus.Material = mat;

        _ring.Mesh    = torus;
        _ring.Visible = false;
        AddChild(_ring);
    }

    public void Select()
    {
        if (IsSelected) return;
        IsSelected    = true;
        _ring.Visible = true;
        EmitSignal(SignalName.Selected);
    }

    public void Deselect()
    {
        if (!IsSelected) return;
        IsSelected    = false;
        _ring.Visible = false;
        EmitSignal(SignalName.Deselected);
    }
}
