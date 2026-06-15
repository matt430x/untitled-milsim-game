using MilSim.Data;

namespace MilSim.Entities.Units.Components;

public partial class CombatComponent : Node
{
    [Signal] public delegate void AttackedTargetEventHandler(int targetId);

    [Export] public float Damage { get; set; }
    [Export] public float AttackRange { get; set; }
    [Export] public float AttackCooldown { get; set; } = 1f;

    // Soft counter multipliers loaded from UnitData
    public float DamageVsInfantry { get; set; } = 1f;
    public float DamageVsVehicle { get; set; } = 1f;
    public float DamageVsAircraft { get; set; } = 1f;
    public float DamageVsShip { get; set; } = 1f;
    public float DamageVsBuilding { get; set; } = 1f;

    private float _cooldownTimer;
    private IDamageable _currentTarget;
    private UnitType _currentTargetType;

    public bool HasTarget => _currentTarget != null && !_currentTarget.IsDead;

    public override void _PhysicsProcess(double delta)
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= (float)delta;

        if (HasTarget && _cooldownTimer <= 0f)
            ExecuteAttack();
    }

    public void SetTarget(IDamageable target, UnitType targetType)
    {
        _currentTarget = target;
        _currentTargetType = targetType;
    }

    public void ClearTarget()
    {
        _currentTarget = null;
    }

    public void LoadFromData(UnitData data)
    {
        Damage = data.Damage;
        AttackRange = data.AttackRange;
        AttackCooldown = data.AttackCooldown;
        DamageVsInfantry = data.DamageVsInfantry;
        DamageVsVehicle = data.DamageVsVehicle;
        DamageVsAircraft = data.DamageVsAircraft;
        DamageVsShip = data.DamageVsShip;
        DamageVsBuilding = data.DamageVsBuilding;
    }

    private void ExecuteAttack()
    {
        float multiplier = _currentTargetType switch
        {
            UnitType.Infantry => DamageVsInfantry,
            UnitType.Vehicle  => DamageVsVehicle,
            UnitType.Aircraft => DamageVsAircraft,
            UnitType.Ship     => DamageVsShip,
            _                 => 1f
        };

        _currentTarget.TakeDamage(Damage * multiplier, GetParent().GetInstanceId().ToString().GetHashCode());
        _cooldownTimer = AttackCooldown;
    }
}
