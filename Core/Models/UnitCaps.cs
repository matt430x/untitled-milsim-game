namespace MilSim.Core.Models;

public class UnitCaps
{
    public int InfantryMax { get; set; } = 20;
    public int VehicleMax { get; set; } = 6;
    public int AircraftMax { get; set; } = 4;
    public int ShipMax { get; set; } = 6;
    public int BuilderMax { get; set; } = 5;

    public int InfantryCurrent { get; set; }
    public int VehicleCurrent { get; set; }
    public int AircraftCurrent { get; set; }
    public int ShipCurrent { get; set; }
    public int BuilderCurrent { get; set; }

    public bool CanTrain(UnitType type) => type switch
    {
        UnitType.Infantry => InfantryCurrent < InfantryMax,
        UnitType.Vehicle  => VehicleCurrent < VehicleMax,
        UnitType.Aircraft => AircraftCurrent < AircraftMax,
        UnitType.Ship     => ShipCurrent < ShipMax,
        UnitType.Builder  => BuilderCurrent < BuilderMax,
        _                 => false
    };

    public void Increment(UnitType type)
    {
        switch (type)
        {
            case UnitType.Infantry: InfantryCurrent++; break;
            case UnitType.Vehicle:  VehicleCurrent++;  break;
            case UnitType.Aircraft: AircraftCurrent++; break;
            case UnitType.Ship:     ShipCurrent++;     break;
            case UnitType.Builder:  BuilderCurrent++;  break;
        }
    }

    public void Decrement(UnitType type)
    {
        switch (type)
        {
            case UnitType.Infantry: InfantryCurrent = Math.Max(0, InfantryCurrent - 1); break;
            case UnitType.Vehicle:  VehicleCurrent  = Math.Max(0, VehicleCurrent - 1);  break;
            case UnitType.Aircraft: AircraftCurrent = Math.Max(0, AircraftCurrent - 1); break;
            case UnitType.Ship:     ShipCurrent     = Math.Max(0, ShipCurrent - 1);     break;
            case UnitType.Builder:  BuilderCurrent  = Math.Max(0, BuilderCurrent - 1);  break;
        }
    }
}
