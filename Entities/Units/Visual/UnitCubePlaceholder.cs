namespace MilSim.Entities.Units.Visual;

/// <summary>
/// Simple box mesh placeholder until real unit models are added.
/// </summary>
public partial class UnitCubePlaceholder : Node3D
{
    [Export] public Color Color { get; set; } = new Color(0.55f, 0.72f, 1.00f);

    public override void _Ready()
    {
        var mesh = new MeshInstance3D();
        var box  = new BoxMesh { Size = new Vector3(0.5f, 0.8f, 0.5f) };
        box.Material = new StandardMaterial3D { AlbedoColor = Color };
        mesh.Mesh     = box;
        mesh.Position = new Vector3(0f, 0.4f, 0f); // pivot at feet
        AddChild(mesh);
    }
}
