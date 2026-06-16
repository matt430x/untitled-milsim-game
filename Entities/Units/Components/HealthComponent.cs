using MilSim.Autoloads;

namespace MilSim.Entities.Units.Components;

public partial class HealthComponent : Node, IDamageable
{
    [Signal] public delegate void DiedEventHandler(int attackerId);
    [Signal] public delegate void HealthChangedEventHandler(float current, float max);
    [Signal] public delegate void DamageStateChangedEventHandler(float healthPercent);

    [Export] public float MaxHealth { get; set; } = 100f;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private const float HalfHealthThreshold = 0.5f;
    private const float CriticalHealthThreshold = 0.25f;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

    public void InitHealth()
    {
        CurrentHealth = MaxHealth;
        IsDead = false;
    }

    public void TakeDamage(float amount, int attackerId)
    {
        if (IsDead) return;

        float previous = CurrentHealth;
        CurrentHealth = Math.Max(0f, CurrentHealth - amount);

        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        NotifyDamageStateChange(previous, CurrentHealth);

        if (CurrentHealth <= 0f)
            Die(attackerId);
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    private void Die(int attackerId)
    {
        IsDead = true;
        EmitSignal(SignalName.Died, attackerId);
    }

    private void NotifyDamageStateChange(float previous, float current)
    {
        float prevPercent = previous / MaxHealth;
        float currPercent = current / MaxHealth;

        bool crossedHalf = prevPercent > HalfHealthThreshold && currPercent <= HalfHealthThreshold;
        bool crossedCritical = prevPercent > CriticalHealthThreshold && currPercent <= CriticalHealthThreshold;

        if (crossedHalf || crossedCritical)
            EmitSignal(SignalName.DamageStateChanged, currPercent);
    }
}
