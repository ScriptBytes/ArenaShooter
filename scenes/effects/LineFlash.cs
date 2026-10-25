using Godot;

namespace ArenaShooter;

/// <summary>
/// Brief fading line through a list of points: a charge's path, or a lightning bolt when
/// given <c>jitter</c> (each segment gets a jagged kink). Purely visual.
/// </summary>
public partial class LineFlash : Node2D
{
    public Vector2[] Points { get; set; } = [];
    public Color Color { get; set; } = Colors.White;
    public float Width { get; set; } = 3f;
    public float Duration { get; set; } = 0.25f;

    /// <summary>Scales the whole line's alpha, e.g. for a faint wide band under a crack.</summary>
    public float Opacity { get; set; } = 1f;

    private float _time;

    /// <summary>Adds a line to the world. Points are global; deferred so it's safe from physics callbacks.</summary>
    public static void Spawn(Node world, Vector2[] points, Color color, float width, float jitter = 0f,
        float opacity = 1f, float duration = 0.25f)
    {
        var flash = new LineFlash
        {
            Points = jitter > 0f ? Jag(points, jitter) : points, Color = color, Width = width,
            Opacity = opacity, Duration = duration,
        };
        world.CallDeferred(Node.MethodName.AddChild, flash);
    }

    // Splits each segment into a few pieces and nudges the joints sideways at random.
    private static Vector2[] Jag(Vector2[] points, float jitter)
    {
        const int pieces = 4;
        var result = new System.Collections.Generic.List<Vector2>();
        for (int i = 0; i < points.Length - 1; i++)
        {
            var a = points[i];
            var b = points[i + 1];
            var side = (b - a).Orthogonal().Normalized();
            result.Add(a);
            for (int p = 1; p < pieces; p++)
                result.Add(a.Lerp(b, (float)p / pieces) + side * (float)GD.RandRange(-jitter, jitter));
        }
        result.Add(points[^1]);
        return result.ToArray();
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
        if (Points.Length < 2)
            return;

        float fade = (1f - Mathf.Clamp(_time / Duration, 0f, 1f)) * Opacity;
        // A soft wide glow under a bright core.
        DrawPolyline(Points, new Color(Color, 0.35f * fade), Width + 6f);
        DrawPolyline(Points, new Color(Color, 0.9f * fade), Width);
    }
}
