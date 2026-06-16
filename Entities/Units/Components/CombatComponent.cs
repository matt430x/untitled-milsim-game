using MilSim.Autoloads;
using MilSim.Data;
using MilSim.Entities.Units;

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
    private BaseUnit _self;
    private BaseUnit _currentTarget;

    public bool HasTarget => _currentTarget != null && !_currentTarget.IsDead;

    public override void _Ready()
    {
        _self = GetParent<BaseUnit>();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= (float)delta;

        if (HasTarget && OutOfRange(_currentTarget))
            ClearTarget();

        if (!HasTarget)
            AcquireTarget();

        if (HasTarget && _cooldownTimer <= 0f)
            ExecuteAttack();
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

    private bool OutOfRange(BaseUnit target) =>
        _self.GlobalPosition.DistanceTo(target.GlobalPosition) > AttackRange;

    /// Picks the first hostile unit within range, scanning registration order.
    private void AcquireTarget()
    {
        foreach (var selectable in SelectionRegistry.All)
        {
            if (selectable is not BaseUnit unit || unit == _self || unit.IsDead) continue;
            if (!PlayerManager.Instance.AreHostile(_self.OwnerId, unit.OwnerId)) continue;
            if (OutOfRange(unit)) continue;

            _currentTarget = unit;
            return;
        }
    }

    private void ExecuteAttack()
    {
        float multiplier = _currentTarget.UnitType switch
        {
            UnitType.Infantry => DamageVsInfantry,
            UnitType.Vehicle  => DamageVsVehicle,
            UnitType.Aircraft => DamageVsAircraft,
            UnitType.Ship     => DamageVsShip,
            _                 => 1f
        };

        var muzzle = _self.GlobalPosition + new Vector3(0f, 0.4f, 0f);
        var hit    = _currentTarget.GlobalPosition + new Vector3(0f, 0.4f, 0f);
        EventBus.RaiseUnitFired(muzzle, hit);
        _currentTarget.TakeDamage(Damage * multiplier, _self.GetInstanceId().ToString().GetHashCode());
        _cooldownTimer = AttackCooldown;
    }
}
