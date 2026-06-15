namespace MilSim.Core.Interfaces;

public interface IDamageable
{
    float MaxHealth { get; }
    float CurrentHealth { get; }
    bool IsDead { get; }
    void TakeDamage(float amount, int attackerId);
}
