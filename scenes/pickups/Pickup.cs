using Godot;

namespace ArenaShooter;

/// <summary>
/// Base for things enemies drop (gold, hearts). Sits still until the player comes within their
/// pickup range, then accelerates toward them and calls <see cref="Collect"/> on arrival.
/// </summary>
public abstract partial class Pickup : Node2D
{
    private const float CollectDistance = 18f;
    private const float Acceleration = 2400f;
    private const float MaxSpeed = 900f;

    protected Player Player { get; private set; }
    private bool _attracted;
    private float _speed;

    public override void _Ready()
    {
        AddToGroup(Groups.Transient);
        ZIndex = -1;
        Player = GetTree().GetFirstNodeInGroup(Groups.Player) as Player;
    }

    /// <summary>When false the pickup stays put, e.g. a heart while the player is at full health.</summary>
    protected virtual bool Wanted => true;

    protected abstract void Collect();

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(Player))
            return;

        var target = Player.GlobalPosition;
        if (!_attracted && Wanted && GlobalPosition.DistanceTo(target) <= Player.PickupRange)
            _attracted = true;

        if (!_attracted)
            return;

        _speed = Mathf.Min(_speed + Acceleration * (float)delta, MaxSpeed);
        GlobalPosition = GlobalPosition.MoveToward(target, _speed * (float)delta);

        if (GlobalPosition.DistanceTo(target) <= CollectDistance)
        {
            Collect();
            QueueFree();
        }
    }
}
