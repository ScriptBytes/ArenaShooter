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
}
