using MilSim.Autoloads;
using MilSim.Data;
using MilSim.Entities.Buildings;
using MilSim.Entities.Crystals;
using MilSim.Entities.Buildings.Visual;
using MilSim.Entities.Units;
using MilSim.Entities.Units.Components;

namespace MilSim.Systems;

/// <summary>
/// Drives building/unit placement from the spawn menu. A ghost preview follows the
/// mouse on the ground; green = valid, red = obstructed. Left-click places, Esc or
/// right-click cancels. While placing, SelectionManager ignores world clicks.
/// </summary>
public partial class PlacementController : Node3D
{
    public static bool IsPlacing { get; private set; }

    private const float MapSize = 80f;

    private static readonly Color ValidColor   = new Color(0.20f, 0.90f, 0.25f, 0.6f);
    private static readonly Color InvalidColor = new Color(0.95f, 0.20f, 0.20f, 0.6f);

    private static readonly Vector3 DefaultBuildingSize = new Vector3(2f, 1.5f, 2f);
    private static readonly Vector3 CrystalSize         = new Vector3(1.2f, 2.2f, 1.2f);

    private PackedScene   _scene;
    private PlaceableKind _kind;
    private int           _ownerId;
    private UnitData      _unitData;
    private BuildingData  _buildingData;
    private bool          _requiresCrystal;
    private Vector2       _footprint; // full XZ extents
    private float         _height;

    private Node3D             _ghost;
    private StandardMaterial3D _ghostMat;
    private Vector3            _ghostPos;
    private bool              _isValid;

    public override void _Ready() => EventBus.OnPlacementRequested += OnPlacementRequested;

    public override void _ExitTree()
    {
        EventBus.OnPlacementRequested -= OnPlacementRequested;
        Cancel();
    }

    public override void _Process(double delta)
    {
        if (!IsPlacing) return;

        Vector3? ground = MouseToGround();
        if (ground == null) return;

        _ghostPos             = ground.Value;
        _isValid              = IsValidPlacement(_ghostPos);
        _ghost.Position       = _ghostPos;
        _ghostMat.AlbedoColor = _isValid ? ValidColor : InvalidColor;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsPlacing) return;

        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (_isValid) { Place(); Cancel(); }
                GetViewport().SetInputAsHandled();
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                Cancel();
                GetViewport().SetInputAsHandled();
            }
        }
        else if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
        {
            Cancel();
            GetViewport().SetInputAsHandled();
        }
    }

    // -------------------------------------------------------------------------

    private void OnPlacementRequested(PlacementRequest request)
    {
        Cancel();
        if (request.Scene == null) return;

        _scene           = request.Scene;
        _kind            = request.Kind;
        _ownerId         = request.OwnerId;
        _unitData        = request.Unit;
        _buildingData    = request.Building;
        _requiresCrystal = request.Kind == PlaceableKind.Building && request.Building != null
                        && request.Building.RequiresCrystal;

        ResolveFootprint();
        BuildGhost();
        IsPlacing = true;
    }

    private void ResolveFootprint()
    {
        Vector3 size;
        switch (_kind)
        {
            case PlaceableKind.Building:
                var probe = _scene.Instantiate();
                var box   = probe.GetNodeOrNull<BuildingBoxPlaceholder>("BuildingBoxPlaceholder");
                size = box?.Size ?? DefaultBuildingSize;
                probe.Free();
                break;
            case PlaceableKind.Crystal:
                size = CrystalSize;
                break;
            default:
                size = new Vector3(0.5f, 0.8f, 0.5f);
                break;
        }
        _footprint = new Vector2(size.X, size.Z);
        _height    = size.Y;
    }

    private void BuildGhost()
    {
        _ghost = new Node3D();

        var mesh = new MeshInstance3D();
        var box  = new BoxMesh { Size = new Vector3(_footprint.X, _height, _footprint.Y) };
        _ghostMat = new StandardMaterial3D
        {
            AlbedoColor  = ValidColor,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode  = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        box.Material  = _ghostMat;
        mesh.Mesh     = box;
        mesh.Position = new Vector3(0f, _height * 0.5f, 0f);

        _ghost.AddChild(mesh);
        AddChild(_ghost);
    }

    private void Place()
    {
        int ownerId = _ownerId;

        switch (_kind)
        {
            case PlaceableKind.Building:
                var building = _scene.Instantiate<BaseBuilding>();
                building.Data           = _buildingData;
                building.OwnerId        = ownerId;
                building.GlobalPosition = _ghostPos;
                GetTree().Root.GetNode<Node3D>("Game/World/Buildings").AddChild(building);
                break;
            case PlaceableKind.Crystal:
                var crystal = _scene.Instantiate<Node3D>();
                crystal.GlobalPosition = _ghostPos;
                GetTree().Root.GetNode<Node3D>("Game/World/Crystals").AddChild(crystal);
                break;
            default:
                var unit = _scene.Instantiate<BaseUnit>();
                unit.Data = _unitData;
                unit.GetNode<SelectionComponent>("SelectionComponent").OwnerId = ownerId;
                unit.GlobalPosition = _ghostPos;
                GetTree().Root.GetNode<Node3D>("Game/World/Units").AddChild(unit);
                break;
        }
    }

    private void Cancel()
    {
        IsPlacing = false;
        _ghost?.QueueFree();
        _ghost    = null;
        _ghostMat = null;
        _scene    = null;
    }

    // -------------------------------------------------------------------------
    // Validation
    // -------------------------------------------------------------------------

    private bool IsValidPlacement(Vector3 pos)
    {
        if (!IsFullyOnMap(pos)) return false;
        if (OverlapsAnyBuilding(pos)) return false;
        if (_requiresCrystal && !InsideUsableCrystalZone(pos)) return false;
        return true;
    }

    private bool IsFullyOnMap(Vector3 pos)
    {
        float hx = _footprint.X * 0.5f;
        float hz = _footprint.Y * 0.5f;
        return pos.X - hx >= 0f && pos.X + hx <= MapSize
            && pos.Z - hz >= 0f && pos.Z + hz <= MapSize;
    }

    private bool OverlapsAnyBuilding(Vector3 pos)
    {
        Rect2 mine = FootprintRect(pos, _footprint);

        foreach (var selectable in SelectionRegistry.All)
        {
            if (selectable is not BaseBuilding b) continue;
            var box = b.GetNodeOrNull<BuildingBoxPlaceholder>("BuildingBoxPlaceholder");
            Vector3 size = box?.Size ?? DefaultBuildingSize;
            Rect2 other  = FootprintRect(b.GlobalPosition, new Vector2(size.X, size.Z));
            if (mine.Intersects(other)) return true;
        }
        return false;
    }

    // Placement is allowed only inside a crystal zone that isn't claimed by an enemy.
    // A zone is "enemy-claimed" when a hostile building sits within the crystal's
    // radius — but powerplants don't count as a claim.
    private bool InsideUsableCrystalZone(Vector3 pos)
    {
        var p = new Vector2(pos.X, pos.Z);
        foreach (var crystal in CrystalRegistry.All)
        {
            var c = new Vector2(crystal.GlobalPosition.X, crystal.GlobalPosition.Z);
            if (p.DistanceTo(c) > crystal.BuildRadius) continue;
            if (!IsEnemyClaimed(crystal)) return true;
        }
        return false;
    }

    private bool IsEnemyClaimed(CrystalNode crystal)
    {
        var c = new Vector2(crystal.GlobalPosition.X, crystal.GlobalPosition.Z);
        foreach (var selectable in SelectionRegistry.All)
        {
            if (selectable is not BaseBuilding b) continue;
            if (!PlayerManager.Instance.AreHostile(_ownerId, b.OwnerId)) continue;
            if (b.Data != null && !b.Data.ClaimsCrystalZone) continue; // e.g. powerplants don't claim

            var bp = new Vector2(b.GlobalPosition.X, b.GlobalPosition.Z);
            if (bp.DistanceTo(c) <= crystal.BuildRadius) return true;
        }
        return false;
    }

    private static Rect2 FootprintRect(Vector3 center, Vector2 size) =>
        new Rect2(center.X - size.X * 0.5f, center.Z - size.Y * 0.5f, size.X, size.Y);

    private Vector3? MouseToGround()
    {
        var cam = GetViewport().GetCamera3D();
        if (cam == null) return null;

        Vector2 screenPos = GetViewport().GetMousePosition();
        Vector3 origin    = cam.ProjectRayOrigin(screenPos);
        Vector3 dir       = cam.ProjectRayNormal(screenPos);

        if (Mathf.Abs(dir.Y) < 0.001f) return null;
        float t = -origin.Y / dir.Y;
        if (t < 0f) return null;
        return origin + dir * t;
    }
}
