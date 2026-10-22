using Godot;

namespace ArenaShooter;

/// <summary>
/// Gold dropped by enemies. Sits still until the player comes within their pickup range,
/// then accelerates toward them. Coins left when the wave ends go into the bag (see RunState.Bag).
/// </summary>
public partial class Coin : Node2D
{
    private const float CollectDistance = 18f;
    private const float Acceleration = 2400f;
    private const float MaxSpeed = 900f;

    public static readonly Color GoldColor = new("f5c542");

    public int Value { get; set; } = 1;

    private Player _player;
    private bool _attracted;
    private float _speed;

    public override void _Ready()
    {
        AddToGroup(Groups.Transient);
        ZIndex = -1;
        _player = GetTree().GetFirstNodeInGroup(Groups.Player) as Player;
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(_player))
            return;

        var target = _player.GlobalPosition;
        if (!_attracted && GlobalPosition.DistanceTo(target) <= _player.PickupRange)
            _attracted = true;

        if (!_attracted)
            return;

        _speed = Mathf.Min(_speed + Acceleration * (float)delta, MaxSpeed);
        GlobalPosition = GlobalPosition.MoveToward(target, _speed * (float)delta);

        if (GlobalPosition.DistanceTo(target) <= CollectDistance)
        {
            _player.CollectGold(Value);
            QueueFree();
        }
    }

    public override void _Draw()
    {
        const float s = 6f;
        DrawColoredPolygon(
            [new Vector2(0, -s), new Vector2(s, 0), new Vector2(0, s), new Vector2(-s, 0)],
            GoldColor);
    }
}
