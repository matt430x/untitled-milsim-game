using MilSim.World;

namespace MilSim.UI;

/// <summary>
/// Minimap in the bottom-right corner. Shows the map as a true overhead rectangle.
/// The camera viewport is projected by casting rays through each screen corner to Y=0,
/// which naturally produces a trapezoid from the perspective camera — same as Civ 6.
/// </summary>
public partial class MinimapPanel : Control
{
    // Must match Baseplate defaults
    private const int   MapTilesWide = 80;
    private const int   MapTilesTall = 80;
    private const float MapTileSize  = 1f;

    private const float Pad    = 12f;
    private const float MmW    = 220f;
    private const float MmH    = 220f;
    private const float PanelW = MmW + Pad * 2f;
    private const float PanelH = MmH + Pad * 2f;
    private const float Margin = 10f;

    private static readonly Color BgColor        = new(0.05f, 0.07f, 0.05f, 0.90f);
    private static readonly Color MapFillColor   = new(0.18f, 0.32f, 0.14f, 1.00f);
    private static readonly Color MapBorderColor = new(0.40f, 0.60f, 0.30f, 0.70f);
    private static readonly Color BorderColor    = new(0.55f, 0.55f, 0.55f, 0.85f);
    private static readonly Color ViewportColor  = Colors.White;

    private GameCamera _gameCamera;
    private bool       _isDragging;
    private Vector3    _cameraOffset;   // 3D offset from focus to click point

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        AnchorLeft   = 1f; AnchorTop    = 1f;
        AnchorRight  = 1f; AnchorBottom = 1f;
        OffsetRight  = -Margin;
        OffsetBottom = -Margin;
        OffsetLeft   = OffsetRight  - PanelW;
        OffsetTop    = OffsetBottom - PanelH;

        MouseFilter = MouseFilterEnum.Stop;

        // Defer camera lookup until scene is fully ready
        CallDeferred(MethodName.FindGameCamera);
    }

    private void FindGameCamera()
    {
        _gameCamera = GetNode<GameCamera>("/root/Game/Camera");
    }

    public override void _Process(double delta)
    {
        UpdateDrag();
        QueueRedraw();
    }

    // -------------------------------------------------------------------------
    // Input
    // -------------------------------------------------------------------------

    public override void _GuiInput(InputEvent @event)
    {
        if (_gameCamera == null || @event is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left) return;

        if (mb.Pressed)
        {
            Vector3 worldClick = ToWorld(mb.Position);
            _cameraOffset = IsInsideViewport(mb.Position)
                ? new Vector3(
                    _gameCamera.FocusPoint.X - worldClick.X,
                    0f,
                    _gameCamera.FocusPoint.Z - worldClick.Z)
                : Vector3.Zero;

            _gameCamera.SetFocusPoint(worldClick + _cameraOffset);
            _isDragging = true;
            AcceptEvent();
        }
    }

    private void UpdateDrag()
    {
        if (!_isDragging || _gameCamera == null) return;
        if (!Input.IsMouseButtonPressed(MouseButton.Left))
        {
            _isDragging = false;
            return;
        }
        _gameCamera.SetFocusPoint(ToWorld(GetLocalMousePosition()) + _cameraOffset);
    }

    // -------------------------------------------------------------------------
    // Drawing
    // -------------------------------------------------------------------------

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), BgColor);
        DrawMapRect();
        DrawUnits();
        DrawCameraViewport();
        DrawRect(new Rect2(Vector2.Zero, Size), BorderColor, filled: false, width: 1.5f);
    }

    private void DrawMapRect()
    {
        var r = new Rect2(new Vector2(Pad, Pad), new Vector2(MmW, MmH));
        DrawRect(r, MapFillColor);
        DrawRect(r, MapBorderColor, filled: false, width: 1f);
    }

    private void DrawUnits()
    {
        const float Dot  = 3f;
        Vector2     half = Vector2.One * (Dot * 0.5f);
        foreach (var selectable in SelectionRegistry.All)
        {
            if (selectable is not Node3D node) continue;
            DrawRect(new Rect2(ToMm(node.GlobalPosition) - half, Vector2.One * Dot), Colors.White);
        }
    }

    private void DrawCameraViewport()
    {
        var cam = GetViewport().GetCamera3D();
        if (cam == null) return;

        Vector2 screenSize = GetViewport().GetVisibleRect().Size;

        // Cast a ray through each screen corner to the ground plane (Y=0).
        // The perspective camera makes the near (bottom) edge wider than the far (top) edge,
        // producing a natural trapezoid — exactly like Civ 6.
        Vector2[] screenCorners =
        {
            new(0,            0),
            new(screenSize.X, 0),
            new(screenSize.X, screenSize.Y),
            new(0,            screenSize.Y),
        };

        var mmPts = new List<Vector2>(4);
        foreach (var sc in screenCorners)
        {
            Vector3 origin = cam.ProjectRayOrigin(sc);
            Vector3 dir    = cam.ProjectRayNormal(sc);
            if (Mathf.Abs(dir.Y) < 0.001f) continue;
            float t = -origin.Y / dir.Y;
            if (t < 0) continue;
            mmPts.Add(ToMm(origin + dir * t));
        }

        if (mmPts.Count < 3) return;

        var clipped = ClipPolygonToRect(mmPts, new Rect2(Pad, Pad, MmW, MmH));
        if (clipped.Count < 2) return;

        clipped.Add(clipped[0]); // close the loop
        DrawPolyline(clipped.ToArray(), ViewportColor, 1.5f);
    }

    // -------------------------------------------------------------------------
    // Sutherland-Hodgman polygon clipping
    // -------------------------------------------------------------------------

    private static List<Vector2> ClipPolygonToRect(List<Vector2> poly, Rect2 rect)
    {
        float l = rect.Position.X, t = rect.Position.Y;
        float r = l + rect.Size.X,  b = t + rect.Size.Y;

        List<Vector2> pts = new(poly);
        pts = ClipEdge(pts, p => p.X >= l, (a, v) => new Vector2(l, a.Y + (v.Y - a.Y) * (l - a.X) / (v.X - a.X)));
        pts = ClipEdge(pts, p => p.X <= r, (a, v) => new Vector2(r, a.Y + (v.Y - a.Y) * (r - a.X) / (v.X - a.X)));
        pts = ClipEdge(pts, p => p.Y >= t, (a, v) => new Vector2(a.X + (v.X - a.X) * (t - a.Y) / (v.Y - a.Y), t));
        pts = ClipEdge(pts, p => p.Y <= b, (a, v) => new Vector2(a.X + (v.X - a.X) * (b - a.Y) / (v.Y - a.Y), b));
        return pts;
    }

    private static List<Vector2> ClipEdge(List<Vector2> input,
        Func<Vector2, bool> inside, Func<Vector2, Vector2, Vector2> intersect)
    {
        var output = new List<Vector2>();
        if (input.Count == 0) return output;

        for (int i = 0; i < input.Count; i++)
        {
            Vector2 cur  = input[i];
            Vector2 prev = input[(i + input.Count - 1) % input.Count];

            if (inside(cur))
            {
                if (!inside(prev)) output.Add(intersect(prev, cur));
                output.Add(cur);
            }
            else if (inside(prev))
            {
                output.Add(intersect(prev, cur));
            }
        }
        return output;
    }

    // -------------------------------------------------------------------------
    // Coordinate helpers
    // -------------------------------------------------------------------------

    /// World 3D → minimap local pixels. X/Z map directly to tile column/row.
    private Vector2 ToMm(Vector3 world) => new(
        Pad + (world.X / (MapTilesWide * MapTileSize)) * MmW,
        Pad + (world.Z / (MapTilesTall * MapTileSize)) * MmH
    );

    /// Minimap local pixels → world 3D (on ground plane Y=0).
    private Vector3 ToWorld(Vector2 mm) => new(
        (mm.X - Pad) / MmW * MapTilesWide * MapTileSize,
        0f,
        (mm.Y - Pad) / MmH * MapTilesTall * MapTileSize
    );

    /// Returns true if the minimap position maps to a world point the camera can currently see.
    private bool IsInsideViewport(Vector2 mmPos)
    {
        var cam = GetViewport().GetCamera3D();
        if (cam == null) return false;

        Vector3 world = ToWorld(mmPos);
        if ((cam.GlobalTransform.AffineInverse() * world).Z > 0f) return false;

        Vector2 screen = cam.UnprojectPosition(world);
        return GetViewport().GetVisibleRect().HasPoint(screen);
    }
}
