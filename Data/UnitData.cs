namespace MilSim.Data;

[GlobalClass]
public partial class UnitData : Resource
{
    [Export] public string UnitName { get; set; } = string.Empty;
    [Export] public UnitType UnitType { get; set; }
    [Export] public float MaxHealth { get; set; }
    [Export] public float MoveSpeed { get; set; }
    [Export] public float Damage { get; set; }
    [Export] public float AttackRange { get; set; }
    [Export] public float AttackCooldown { get; set; }
    [Export] public int Cost { get; set; }
    [Export] public float TrainingTime { get; set; }
    [Export] public PackedScene Scene { get; set; }

    // Soft counter multipliers — 1.0 = neutral, >1.0 = effective, <1.0 = weak
    [Export] public float DamageVsInfantry { get; set; } = 1f;
    [Export] public float DamageVsVehicle { get; set; } = 1f;
    [Export] public float DamageVsAircraft { get; set; } = 1f;
    [Export] public float DamageVsShip { get; set; } = 1f;
    [Export] public float DamageVsBuilding { get; set; } = 1f;

    public float GetDamageMultiplier(UnitType targetType) => targetType switch
    {
        UnitType.Infantry => DamageVsInfantry,
        UnitType.Vehicle  => DamageVsVehicle,
        UnitType.Aircraft => DamageVsAircraft,
        UnitType.Ship     => DamageVsShip,
        _                 => 1f
    };
}
