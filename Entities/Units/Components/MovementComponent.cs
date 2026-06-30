using MilSim.World;

namespace MilSim.Entities.Units.Components;

public partial class MovementComponent : Node
{
    [Signal] public delegate void ArrivedAtDestinationEventHandler();

    [Export] public float MoveSpeed { get; set; } = 6f;

    public bool    IsMoving    { get; private set; }
    public Vector3 Destination { get; private set; }

    private CharacterBody3D _body;
    private float           _yVel;
    private const float ArrivalThreshold = 0.08f;
    private const float Gravity          = 20f;

    public override void _Ready() => _body = GetParent<CharacterBody3D>();

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // Apply gravity; reset when on floor so slopes don't accumulate velocity.
        _yVel = _body.IsOnFloor() ? 0f : _yVel - Gravity * dt;

        if (!IsMoving)
        {
            _body.Velocity = new Vector3(0f, _yVel, 0f);
            _body.MoveAndSlide();
            return;
        }

        Vector3 toDestination = Destination - _body.GlobalPosition;
        toDestination.Y = 0f;

        if (toDestination.Length() <= ArrivalThreshold)
        {
            IsMoving = false;
            _body.Velocity = new Vector3(0f, _yVel, 0f);
            _body.MoveAndSlide();
            EmitSignal(SignalName.ArrivedAtDestination);
            return;
        }

        var dir = toDestination.Normalized();
        _body.Velocity = new Vector3(dir.X * MoveSpeed, _yVel, dir.Z * MoveSpeed);
        _body.MoveAndSlide();
    }

    public void MoveTo(Vector3 destination)
    {
        Destination = new Vector3(destination.X, destination.Y, destination.Z);
        IsMoving    = true;
    }

    public void Stop()
    {
        IsMoving       = false;
        _body.Velocity = Vector3.Zero;
    }
}
