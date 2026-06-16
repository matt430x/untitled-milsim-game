namespace MilSim.Entities.Units.Visual;

/// <summary>
/// Simple box mesh placeholder until real unit models are added.
/// </summary>
public partial class UnitCubePlaceholder : Node3D
{
    public static readonly Color FriendlyColor = new Color(0.3f, 0.55f, 1.00f);
    public static readonly Color HostileColor  = new Color(0.95f, 0.25f, 0.2f);

    [Export] public Color Color { get; set; } = FriendlyColor;

    private StandardMaterial3D _material;

    public override void _Ready()
    {
        var mesh = new MeshInstance3D();
        var box  = new BoxMesh { Size = new Vector3(0.5f, 0.8f, 0.5f) };
        _material = new StandardMaterial3D { AlbedoColor = Color };
        box.Material  = _material;
        mesh.Mesh     = box;
        mesh.Position = new Vector3(0f, 0.4f, 0f); // pivot at feet
        AddChild(mesh);
    }

    public void SetHostile(bool hostile) =>
        _material.AlbedoColor = hostile ? HostileColor : FriendlyColor;

    public void SetLabel(string text)
    {
        AddSideLabel(text, new Vector3(0f, 0.4f, 0.3f), 0f);     // +Z face
        AddSideLabel(text, new Vector3(0f, 0.4f, -0.3f), 180f);  // -Z face
        AddSideLabel(text, new Vector3(0.3f, 0.4f, 0f), 90f);    // +X face
        AddSideLabel(text, new Vector3(-0.3f, 0.4f, 0f), -90f);  // -X face
    }

    private void AddSideLabel(string text, Vector3 position, float yRotationDegrees)
    {
        var label = new Label3D
        {
            Text = text,
            Position = position,
            RotationDegrees = new Vector3(0f, yRotationDegrees, 0f),
            FontSize = 16,
            PixelSize = 0.005f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            NoDepthTest = true,
        };
        AddChild(label);
    }
}
