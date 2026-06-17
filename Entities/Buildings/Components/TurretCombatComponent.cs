using MilSim.Autoloads;
using MilSim.Data;
using MilSim.Entities.Units;

namespace MilSim.Entities.Buildings.Components;

/// <summary>
/// Stationary auto-attack for buildings (Turret). Mirrors the unit CombatComponent
/// but parents to a BaseBuilding — the unit version hard-casts its parent to BaseUnit.
/// </summary>
public partial class TurretCombatComponent : Node
{
    [Export] public float Damage         { get; set; }
    [Export] public float AttackRange    { get; set; }
    [Export] public float AttackCooldown { get; set; } = 1f;

    private float        _cooldownTimer;
    private BaseBuilding _self;
    private BaseUnit     _currentTarget;

    public bool HasTarget => _currentTarget != null && !_currentTarget.IsDead;

    public override void _Ready()
    {
        _self = GetParent<BaseBuilding>();
    }

    public void LoadFromData(BuildingData data)
    {
        Damage         = data.Damage;
        AttackRange    = data.AttackRange;
        AttackCooldown = data.AttackCooldown;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_self == null || AttackRange <= 0f) return;

        if (_cooldownTimer > 0f)
            _cooldownTimer -= (float)delta;

        if (HasTarget && OutOfRange(_currentTarget))
            _currentTarget = null;

        if (!HasTarget)
            AcquireTarget();

        if (HasTarget && _cooldownTimer <= 0f)
            ExecuteAttack();
    }

    private bool OutOfRange(BaseUnit target) =>
        _self.GlobalPosition.DistanceTo(target.GlobalPosition) > AttackRange;

    private void AcquireTarget()
    {
        foreach (var selectable in SelectionRegistry.All)
        {
            if (selectable is not BaseUnit unit || unit.IsDead) continue;
            if (!PlayerManager.Instance.AreHostile(_self.OwnerId, unit.OwnerId)) continue;
            if (OutOfRange(unit)) continue;

            _currentTarget = unit;
            return;
        }
    }

    private void ExecuteAttack()
    {
        var muzzle = _self.GlobalPosition + new Vector3(0f, 1.0f, 0f);
        var hit    = _currentTarget.GlobalPosition + new Vector3(0f, 0.4f, 0f);
        EventBus.RaiseUnitFired(muzzle, hit);
        _currentTarget.TakeDamage(Damage, _self.GetInstanceId().ToString().GetHashCode());
        _cooldownTimer = AttackCooldown;
    }
}
