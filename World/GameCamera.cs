namespace MilSim.World;

public partial class GameCamera : Camera2D
{
    [Export] public float PanSpeed { get; set; } = 500f;
    [Export] public float ZoomSpeed { get; set; } = 0.1f;
    [Export] public float MinZoom { get; set; } = 0.25f;
    [Export] public float MaxZoom { get; set; } = 2f;

    public override void _Ready()
    {
        // Center on the map at start
        var baseplate = GetTree().Root.FindChild("Baseplate", owned: false) as Baseplate;
        if (baseplate != null)
            Position = baseplate.MapCenter();
    }

    public override void _Process(double delta)
    {
        HandlePan((float)delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse)
            HandleZoom(mouse);
    }

    private void HandlePan(float delta)
    {
        Vector2 direction = Vector2.Zero;

        if (Input.IsActionPressed("ui_left")  || Input.IsKeyPressed(Key.A)) direction.X -= 1f;
        if (Input.IsActionPressed("ui_right") || Input.IsKeyPressed(Key.D)) direction.X += 1f;
        if (Input.IsActionPressed("ui_up")    || Input.IsKeyPressed(Key.W)) direction.Y -= 1f;
        if (Input.IsActionPressed("ui_down")  || Input.IsKeyPressed(Key.S)) direction.Y += 1f;

        if (direction != Vector2.Zero)
            Position += direction.Normalized() * PanSpeed * delta / Zoom.X;
    }

    private void HandleZoom(InputEventMouseButton mouse)
    {
        if (!mouse.Pressed) return;

        if (mouse.ButtonIndex == MouseButton.WheelUp)
            SetZoom(Zoom.X + ZoomSpeed);
        else if (mouse.ButtonIndex == MouseButton.WheelDown)
            SetZoom(Zoom.X - ZoomSpeed);
    }

    private void SetZoom(float value)
    {
        Zoom = Vector2.One * Math.Clamp(value, MinZoom, MaxZoom);
    }
}
