namespace MilSim.UI;

/// <summary>
/// Minimap panel anchored to the bottom-right of the screen.
/// Draws the playable diamond, all unit positions, and the camera viewport outline.
/// Left-click inside the viewport rect: grab and drag. Left-click outside: jump camera there.
///
/// World-space bounds are derived from the Baseplate defaults (80x80 tiles, 128x64 px).
/// If the map dimensions change, update WorldMin and WorldSize to match.
/// </summary>
public partial class MinimapPanel : Control
{
    // World-space bounding rectangle that contains the entire playable diamond.
    // Recompute from Baseplate.GetMapCorners() if tile count or tile size changes.
    private static readonly Vector2 WorldMin  = new(-5120f,   -32f);
    private static readonly Vector2 WorldSize = new(10240f,  5120f);

    // The four corner points of the isometric diamond in world space
    private static readonly Vector2 CornerTop    = new(     0f,   -32f);
    private static readonly Vector2 CornerRight  = new(  5120f,  2528f);
    private static readonly Vector2 CornerBottom = new(     0f,  5088f);
    private static readonly Vector2 CornerLeft   = new( -5120f,  2528f);

    private const float Pad    = 12f;
    private const float MmW    = 300f;
    private const float MmH    = 150f;
    private const float PanelW = MmW + Pad * 2f;   // 224
    private const float PanelH = MmH + Pad * 2f;   // 124
    private const float Margin = 10f;

    private static readonly Color BgColor        = new(0.05f, 0.07f, 0.05f, 0.90f);
    private static readonly Color MapFillColor   = new(0.18f, 0.32f, 0.14f, 1.00f);
    private static readonly Color MapBorderColor = new(0.40f, 0.60f, 0.30f, 0.70f);
    private static readonly Color BorderColor    = new(0.55f, 0.55f, 0.55f, 0.85f);
    private static readonly Color UnitColor      = Colors.White;
    private static readonly Color ViewportColor  = Colors.White;

    private Camera2D _camera;

    // Drag state
    private bool    _isDragging;
    private Vector2 _cameraOffset; // offset from camera center to the grab point in world space

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        AnchorLeft   = 1f;
        AnchorTop    = 1f;
        AnchorRight  = 1f;
        AnchorBottom = 1f;
        OffsetRight  = -Margin;
        OffsetBottom = -Margin;
        OffsetLeft   = OffsetRight  - PanelW;
        OffsetTop    = OffsetBottom - PanelH;

        MouseFilter = MouseFilterEnum.Stop; // consume mouse events, don't pass through to world
    }

    public override void _Process(double delta)
    {
        UpdateDrag();
        QueueRedraw();
    }

    // -------------------------------------------------------------------------
    // Input — initial click handled here; drag continuation handled in _Process
    // -------------------------------------------------------------------------

    public override void _GuiInput(InputEvent @event)
    {
        if (_camera == null || @event is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left) return;

        if (mb.Pressed)
        {
            Vector2 worldClick = ToWorld(mb.Position);

            // Grab: maintain offset so the rect doesn't snap. Jump: center on click.
            _cameraOffset = IsInsideViewportRect(mb.Position)
                ? _camera.GlobalPosition - worldClick
                : Vector2.Zero;

            _camera.GlobalPosition = worldClick + _cameraOffset;
            _isDragging = true;
            AcceptEvent(); // prevent click from reaching SelectionManager
        }
    }

    // -------------------------------------------------------------------------
    // Drag update — runs every frame while button is held, even outside the panel
    // -------------------------------------------------------------------------

    private void UpdateDrag()
    {
        if (!_isDragging || _camera == null) return;

        if (!Input.IsMouseButtonPressed(MouseButton.Left))
        {
            _isDragging = false;
            return;
        }

        // GetLocalMousePosition works outside the panel bounds too
        _camera.GlobalPosition = ToWorld(GetLocalMousePosition()) + _cameraOffset;
    }

    // -------------------------------------------------------------------------
    // Drawing
    // -------------------------------------------------------------------------

    public override void _Draw()
    {
        if (_camera == null)
            _camera = GetViewport().GetCamera2D();

        DrawRect(new Rect2(Vector2.Zero, Size), BgColor);
        DrawMapDiamond();
        DrawUnits();
        DrawCameraViewport();
        DrawRect(new Rect2(Vector2.Zero, Size), BorderColor, filled: false, width: 1.5f);
    }

    private void DrawMapDiamond()
    {
        Vector2[] pts =
        {
            ToMm(CornerTop),
            ToMm(CornerRight),
            ToMm(CornerBottom),
            ToMm(CornerLeft),
        };

        DrawPolygon(pts, new[] { MapFillColor });
        DrawPolyline(new[] { pts[0], pts[1], pts[2], pts[3], pts[0] }, MapBorderColor, 1f);
    }

    private void DrawUnits()
    {
        const float Dot  = 3f;
        Vector2     half = Vector2.One * (Dot * 0.5f);

        foreach (var selectable in SelectionRegistry.All)
        {
            if (selectable is not Node2D node) continue;
            DrawRect(new Rect2(ToMm(node.GlobalPosition) - half, Vector2.One * Dot), UnitColor);
        }
    }

    private void DrawCameraViewport()
    {
        if (_camera == null) return;

        Vector2 viewSize = GetViewport().GetVisibleRect().Size / _camera.Zoom;
        Vector2 camPos   = _camera.GlobalPosition;
        Vector2 half     = viewSize * 0.5f;

        Vector2[] corners =
        {
            ToMm(camPos + new Vector2(-half.X, -half.Y)),
            ToMm(camPos + new Vector2( half.X, -half.Y)),
            ToMm(camPos + new Vector2( half.X,  half.Y)),
            ToMm(camPos + new Vector2(-half.X,  half.Y)),
        };

        DrawPolyline(new[] { corners[0], corners[1], corners[2], corners[3], corners[0] },
                     ViewportColor, 1.5f);
    }

    // -------------------------------------------------------------------------
    // Coordinate helpers
    // -------------------------------------------------------------------------

    /// Returns whether a minimap-local position falls inside the viewport rectangle.
    private bool IsInsideViewportRect(Vector2 mmPos)
    {
        if (_camera == null) return false;

        Vector2 viewSize = GetViewport().GetVisibleRect().Size / _camera.Zoom;
        Vector2 camMm    = ToMm(_camera.GlobalPosition);
        Vector2 halfMm   = new Vector2(
            viewSize.X / WorldSize.X * MmW,
            viewSize.Y / WorldSize.Y * MmH
        ) * 0.5f;

        return mmPos.X >= camMm.X - halfMm.X && mmPos.X <= camMm.X + halfMm.X &&
               mmPos.Y >= camMm.Y - halfMm.Y && mmPos.Y <= camMm.Y + halfMm.Y;
    }

    /// Converts a minimap-local pixel position to a world-space position.
    private Vector2 ToWorld(Vector2 mmPos)
    {
        float nx = (mmPos.X - Pad) / MmW;
        float ny = (mmPos.Y - Pad) / MmH;
        return WorldMin + new Vector2(nx * WorldSize.X, ny * WorldSize.Y);
    }

    /// Converts a world-space position to minimap-local pixel coordinates.
    private Vector2 ToMm(Vector2 world)
    {
        float nx = (world.X - WorldMin.X) / WorldSize.X;
        float ny = (world.Y - WorldMin.Y) / WorldSize.Y;
        return new Vector2(Pad + nx * MmW, Pad + ny * MmH);
    }
}
