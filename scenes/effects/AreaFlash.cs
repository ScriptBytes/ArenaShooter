using Godot;

namespace ArenaShooter;

/// <summary>
/// Brief fading circle or wedge that shows where an area attack landed.
/// Purely visual; damage is applied separately by the ability.
/// </summary>
public partial class AreaFlash : Node2D
{
    public float Radius { get; set; } = 50f;
    public Color Color { get; set; } = Colors.White;
    public float Arc { get; set; } = Mathf.Tau;
    public float Duration { get; set; } = 0.25f;

    private float _time;

    /// <summary>
    /// Adds a flash to the world. Deferred so it's safe from physics callbacks.
    /// <paramref name="position"/> is global; the world root sits at the origin.
    /// </summary>
    public static void Spawn(Node world, Vector2 position, float radius, Color color,
        float arc = Mathf.Tau, float rotation = 0f)
    {
        var flash = new AreaFlash
        {
            Position = position,
            Rotation = rotation,
            Radius = radius,
            Color = color,
            Arc = arc,
        };
        world.CallDeferred(Node.MethodName.AddChild, flash);
    }

    public override void _Ready()
    {
        AddToGroup(Groups.Transient);
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
        float fade = 1f - Mathf.Clamp(_time / Duration, 0f, 1f);
        var fill = new Color(Color, 0.45f * fade);
        var edge = new Color(Color, 0.9f * fade);

        if (Arc >= Mathf.Tau - 0.01f)
        {
            DrawCircle(Vector2.Zero, Radius, fill);
            DrawArc(Vector2.Zero, Radius, 0f, Mathf.Tau, 48, edge, 2f);
            return;
        }

        // Wedge pointing along +X; Rotation aims it.
        const int segments = 16;
        var points = new Vector2[segments + 2];
        points[0] = Vector2.Zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = -Arc / 2f + Arc * i / segments;
            points[i + 1] = Vector2.FromAngle(angle) * Radius;
        }
        DrawColoredPolygon(points, fill);
        DrawArc(Vector2.Zero, Radius, -Arc / 2f, Arc / 2f, segments, edge, 2f);
    }
}
