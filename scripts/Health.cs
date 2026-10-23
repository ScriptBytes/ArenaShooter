using Godot;

namespace ArenaShooter;

/// <summary>
/// Reusable hit-point component. Add it as a child node named "Health"; the owning
/// entity forwards damage to it and listens for <see cref="Died"/>.
/// </summary>
public partial class Health : Node
{
    [Signal] public delegate void ChangedEventHandler(float current, float max);
    [Signal] public delegate void DiedEventHandler();

    [Export] public float Max { get; set; } = 10f;

    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;

    public override void _Ready()
    {
        Current = Max;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        Current = Mathf.Max(Current - amount, 0f);
        EmitSignal(SignalName.Changed, Current, Max);

        if (IsDead)
            EmitSignal(SignalName.Died);
    }

    /// <summary>Restores up to <paramref name="amount"/>, capped at Max. Returns how much was restored.</summary>
    public float Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return 0f;

        float healed = Mathf.Min(amount, Max - Current);
        Current += healed;
        EmitSignal(SignalName.Changed, Current, Max);
        return healed;
    }

    public void SetMax(float max, bool refill)
    {
        Max = Mathf.Max(max, 1f);
        Current = refill ? Max : Mathf.Min(Current, Max);
        EmitSignal(SignalName.Changed, Current, Max);
    }

    public void Refill() => SetMax(Max, refill: true);
}
