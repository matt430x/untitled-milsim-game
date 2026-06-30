using MilSim.Systems;

namespace MilSim.Entities.Crystals;

public partial class CrystalNode : Node3D
{
    [Export] public float       BuildRadius { get; set; } = 10f;
    [Export] public CrystalType Type        { get; set; } = CrystalType.Standard;

    public override void _Ready()
    {
        BuildCrystalMesh();
        BuildZoneRing();
        CrystalRegistry.Register(this);
    }

    public override void _ExitTree() => CrystalRegistry.Unregister(this);

    private void BuildCrystalMesh()
    {
        bool isSuper = Type == CrystalType.Super;

        var mesh = new MeshInstance3D();
        var box  = new BoxMesh { Size = isSuper ? new Vector3(2f, 4f, 2f) : new Vector3(1.2f, 2.2f, 1.2f) };
        box.Material = new StandardMaterial3D
        {
            AlbedoColor              = isSuper ? new Color(1.0f, 0.78f, 0.1f) : new Color(0.3f, 0.85f, 0.95f),
            EmissionEnabled          = true,
            Emission                 = isSuper ? new Color(0.9f, 0.55f, 0f)   : new Color(0.2f, 0.7f, 0.9f),
            EmissionEnergyMultiplier = isSuper ? 1.5f : 0.5f,
        };
        mesh.Mesh     = box;
        mesh.Position = new Vector3(0f, isSuper ? 2f : 1.1f, 0f);
        AddChild(mesh);
    }

    private void BuildZoneRing()
    {
        var ring  = new MeshInstance3D();
        var torus = new TorusMesh
        {
            InnerRadius  = BuildRadius - 0.2f,
            OuterRadius  = BuildRadius,
            Rings        = 64,
            RingSegments = 6,
        };
        torus.Material = new StandardMaterial3D
        {
            AlbedoColor  = Type == CrystalType.Super
                ? new Color(1.0f, 0.78f, 0.1f, 0.5f)
                : new Color(0.3f, 0.85f, 0.95f, 0.5f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode  = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        ring.Mesh     = torus;
        ring.Position = new Vector3(0f, 0.05f, 0f);
        AddChild(ring);
    }
}
