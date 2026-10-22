using Godot;

namespace ArenaShooter;

/// <summary>
/// Burst left behind when an enemy dies: a white flash, an expanding ring and a few
/// shards flying outward in the enemy's color. Purely visual.
/// </summary>
public partial class DeathPop : Node2D
{
    private const float Duration = 0.35f;
    private const int ShardCount = 7;

    public Color Color { get; set; } = Colors.White;
    public float Size { get; set; } = 24f;

    private readonly Vector2[] _shardDirections = new Vector2[ShardCount];
    private float _time;

    /// <summary>Adds a pop to the world. Deferred so it's safe from physics callbacks.</summary>
    public static void Spawn(Node world, Vector2 position, Color color, float size)
    {
        var pop = new DeathPop { Position = position, Color = color, Size = size };
        world.CallDeferred(Node.MethodName.AddChild, pop);
    }

    public override void _Ready()
    {
        AddToGroup(Groups.Transient);
        for (int i = 0; i < ShardCount; i++)
        {
            float angle = Mathf.Tau * i / ShardCount + (float)GD.RandRange(-0.3, 0.3);
            _shardDirections[i] = Vector2.FromAngle(angle) * (float)GD.RandRange(0.7, 1.3);
        }
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();

        if (_time >= Duration)
            QueueFree();
    }

    public override void _Draw()
    {
        float t = Mathf.Clamp(_time / Duration, 0f, 1f);
        float easeOut = 1f - (1f - t) * (1f - t);
        float fade = 1f - t;
        float r = Size / 2f;

        if (t < 0.25f)
            DrawCircle(Vector2.Zero, r * (1f - t * 2f), new Color(1f, 1f, 1f, 0.9f));

        DrawArc(Vector2.Zero, r * (0.6f + 1.2f * easeOut), 0f, Mathf.Tau, 32, new Color(Color, fade), 3f * fade + 1f);

        float shard = Mathf.Max(r * 0.35f * fade, 1.5f);
        foreach (var direction in _shardDirections)
        {
            var center = direction * r * (0.5f + 2.2f * easeOut);
            DrawRect(new Rect2(center - Vector2.One * shard / 2f, Vector2.One * shard), new Color(Color, fade));
        }
    }
}
