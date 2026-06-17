namespace MilSim.Entities.Buildings.Visual;

/// <summary>
/// Simple box mesh placeholder until real building models are added.
/// Size is set per building scene; the box rests on the ground (pivot at base).
/// </summary>
public partial class BuildingBoxPlaceholder : Node3D
{
    public static readonly Color FriendlyColor = new Color(0.3f, 0.55f, 1.00f);
    public static readonly Color HostileColor  = new Color(0.95f, 0.25f, 0.2f);

    [Export] public Vector3 Size  { get; set; } = new Vector3(2f, 1.5f, 2f);
    [Export] public Color   Color { get; set; } = FriendlyColor;

    private StandardMaterial3D _material;

    public override void _Ready()
    {
        var mesh = new MeshInstance3D();
        var box  = new BoxMesh { Size = Size };
        _material = new StandardMaterial3D { AlbedoColor = Color };
        box.Material  = _material;
        mesh.Mesh     = box;
        mesh.Position = new Vector3(0f, Size.Y * 0.5f, 0f);
        AddChild(mesh);
    }

    public void SetHostile(bool hostile) =>
        _material.AlbedoColor = hostile ? HostileColor : FriendlyColor;

    public void SetLabel(string text)
    {
        var label = new Label3D
        {
            Text                = text,
            Position            = new Vector3(0f, Size.Y + 0.4f, 0f),
            FontSize            = 24,
            PixelSize           = 0.01f,
            Billboard           = BaseMaterial3D.BillboardModeEnum.Enabled,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            NoDepthTest         = true,
        };
        AddChild(label);
    }
}
