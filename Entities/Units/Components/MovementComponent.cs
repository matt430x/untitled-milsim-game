namespace MilSim.Entities.Units.Components;

public partial class MovementComponent : Node
{
    [Signal] public delegate void ArrivedAtDestinationEventHandler();

    [Export] public float MoveSpeed { get; set; } = 6f;

    public bool    IsMoving     { get; private set; }
    public Vector3 Destination  { get; private set; }

    private CharacterBody3D _body;
    private const float ArrivalThreshold = 0.08f;

    public override void _Ready() => _body = GetParent<CharacterBody3D>();

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMoving)
        {
            _body.Velocity = Vector3.Zero;
            _body.MoveAndSlide();
            return;
        }

        Vector3 toDestination = Destination - _body.GlobalPosition;
        toDestination.Y = 0f;

        if (toDestination.Length() <= ArrivalThreshold)
        {
            IsMoving = false;
            _body.Velocity = Vector3.Zero;
            _body.MoveAndSlide();
            EmitSignal(SignalName.ArrivedAtDestination);
            return;
        }

        var vel = toDestination.Normalized() * MoveSpeed;
        _body.Velocity = new Vector3(vel.X, 0f, vel.Z);
        _body.MoveAndSlide();
    }

    public void MoveTo(Vector3 destination)
    {
        Destination = new Vector3(destination.X, 0f, destination.Z);
        IsMoving = true;
    }

    public void Stop()
    {
        IsMoving = false;
        _body.Velocity = Vector3.Zero;
    }
}
