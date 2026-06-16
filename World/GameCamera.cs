namespace MilSim.World;

/// <summary>
/// Perspective camera angled downward like Civ 6.
/// The camera maintains a focus point on the ground plane and positions itself
/// above and behind it at a fixed pitch. Zoom scales both height and distance
/// so the angle stays constant.
/// </summary>
public partial class GameCamera : Camera3D
{
    [Export] public float PanSpeed    { get; set; } = 15f;
    [Export] public float ZoomStep    { get; set; } = 0.15f;
    [Export] public float MinZoom     { get; set; } = 0.25f;
    [Export] public float MaxZoom     { get; set; } = 3.0f;
    [Export] public float BaseHeight  { get; set; } = 30f;
    [Export] public float BaseOffset  { get; set; } = 25f;  // horizontal pull-back from focus

    private float   _zoom = 1f;
    private Vector3 _focus;

    public Vector3 FocusPoint => _focus;

    public override void _Ready()
    {
        Fov = 45f;

        var baseplate = GetTree().Root.FindChild("Baseplate", owned: false) as Baseplate;
        _focus = baseplate != null ? baseplate.MapCenter() : new Vector3(40f, 0f, 40f);
        ApplyTransform();
    }

    public override void _Process(double delta)
    {
        HandlePan((float)delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mb || !mb.Pressed) return;

        if (mb.ButtonIndex == MouseButton.WheelUp)
            SetZoom(_zoom - ZoomStep);
        else if (mb.ButtonIndex == MouseButton.WheelDown)
            SetZoom(_zoom + ZoomStep);
    }

    public void SetFocusPoint(Vector3 point)
    {
        _focus = new Vector3(point.X, 0f, point.Z);
        ApplyTransform();
    }

    private void HandlePan(float delta)
    {
        Vector3 dir = Vector3.Zero;
        if (Input.IsKeyPressed(Key.W) || Input.IsActionPressed("ui_up"))    dir.Z -= 1f;
        if (Input.IsKeyPressed(Key.S) || Input.IsActionPressed("ui_down"))  dir.Z += 1f;
        if (Input.IsKeyPressed(Key.A) || Input.IsActionPressed("ui_left"))  dir.X -= 1f;
        if (Input.IsKeyPressed(Key.D) || Input.IsActionPressed("ui_right")) dir.X += 1f;

        if (dir == Vector3.Zero) return;

        float speed = PanSpeed * _zoom;
        _focus += dir.Normalized() * speed * delta;
        ApplyTransform();
    }

    private void SetZoom(float value)
    {
        _zoom = Math.Clamp(value, MinZoom, MaxZoom);
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        float h = BaseHeight * _zoom;
        float d = BaseOffset * _zoom;
        Position = _focus + new Vector3(0f, h, d);
        LookAt(_focus, Vector3.Up);
    }
}
