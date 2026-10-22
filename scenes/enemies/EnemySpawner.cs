using Godot;

namespace ArenaShooter;

/// <summary>
/// Spawns enemies at random points in the arena, speeding up as the round progresses.
/// Spawned enemies (and pending spawn markers) are children of this node.
/// </summary>
public partial class EnemySpawner : Node2D
{
    [Signal] public delegate void EnemyKilledEventHandler();

    [Export] public PackedScene EnemyScene { get; set; }
    [Export] public Arena Arena { get; set; }

    [Export] public float StartInterval { get; set; } = 1.5f;
    [Export] public float EndInterval { get; set; } = 0.4f;
    [Export] public float TelegraphTime { get; set; } = 0.8f;
    [Export] public float MinPlayerDistance { get; set; } = 220f;
    [Export] public float EdgeMargin { get; set; } = 40f;

    public bool Active { get; set; }

    /// <summary>Round progress from 0 (start) to 1 (end); drives spawn rate.</summary>
    public float Progress { get; set; }

    private float _cooldown;

    public override void _Process(double delta)
    {
        if (!Active)
            return;

        _cooldown -= (float)delta;
        if (_cooldown > 0f)
            return;

        _cooldown = Mathf.Lerp(StartInterval, EndInterval, Progress);

        var marker = new SpawnMarker { Position = PickSpawnPoint(), Duration = TelegraphTime };
        marker.Elapsed += SpawnEnemy;
        AddChild(marker);
    }

    /// <summary>Stops spawning and removes all enemies and pending spawns.</summary>
    public void Clear()
    {
        Active = false;
        foreach (var child in GetChildren())
            child.QueueFree();
    }

    private Vector2 PickSpawnPoint()
    {
        var player = GetTree().GetFirstNodeInGroup(Groups.Player) as Node2D;
        var point = Arena.RandomPoint(EdgeMargin);

        for (int attempt = 0; attempt < 10 && player != null; attempt++)
        {
            if (point.DistanceTo(player.GlobalPosition) >= MinPlayerDistance)
                break;
            point = Arena.RandomPoint(EdgeMargin);
        }

        return point;
    }

    private void SpawnEnemy(Vector2 position)
    {
        if (!Active)
            return;

        var enemy = EnemyScene.Instantiate<Enemy>();
        enemy.Position = position;
        enemy.Died += _ => EmitSignal(SignalName.EnemyKilled);
        AddChild(enemy);
    }
}
