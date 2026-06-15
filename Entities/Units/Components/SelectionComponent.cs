namespace MilSim.Entities.Units.Components;

public partial class SelectionComponent : Node2D, ISelectable
{
    [Signal] public delegate void SelectedEventHandler();
    [Signal] public delegate void DeselectedEventHandler();

    [Export] public int OwnerId { get; set; }

    public bool IsSelected { get; private set; }

    // Isometric ground ellipse — matches 2:1 tile ratio
    private static readonly Color RingColor = new(0.2f, 0.9f, 0.2f);
    private const float RingRx = 28f;
    private const float RingRy = 12f;
    private const float RingOffsetY = 6f;
    private const int   RingSegments = 32;

    public override void _Ready() => Visible = false;

    public override void _Draw()
    {
        var pts = new Vector2[RingSegments + 1];
        for (int i = 0; i <= RingSegments; i++)
        {
            float a = i / (float)RingSegments * MathF.PI * 2f;
            pts[i] = new Vector2(MathF.Cos(a) * RingRx, MathF.Sin(a) * RingRy + RingOffsetY);
        }
        DrawPolyline(pts, RingColor, 2f);
    }

    public void Select()
    {
        if (IsSelected) return;
        IsSelected = true;
        Visible = true;
        EmitSignal(SignalName.Selected);
    }

    public void Deselect()
    {
        if (!IsSelected) return;
        IsSelected = false;
        Visible = false;
        EmitSignal(SignalName.Deselected);
    }
}
