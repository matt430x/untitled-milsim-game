namespace MilSim.Entities.Units.Components;

public partial class MovementComponent : Node
{
    [Export] public float MoveSpeed { get; set; } = 100f;

    public bool IsMoving { get; private set; }
    public Vector2 Destination { get; private set; }

    private CharacterBody2D _body;
    private const float ArrivalThreshold = 4f;

    public override void _Ready()
    {
        _body = GetParent<CharacterBody2D>();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMoving) return;

        Vector2 direction = (_body.GlobalPosition - Destination);
        if (direction.Length() <= ArrivalThreshold)
        {
            IsMoving = false;
            _body.Velocity = Vector2.Zero;
            _body.MoveAndSlide();
            return;
        }

        _body.Velocity = direction.Normalized() * -MoveSpeed;
        _body.MoveAndSlide();
    }

    public void MoveTo(Vector2 worldPosition)
    {
        Destination = worldPosition;
        IsMoving = true;
    }

    public void Stop()
    {
        IsMoving = false;
        _body.Velocity = Vector2.Zero;
    }
}
