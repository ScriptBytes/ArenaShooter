using Godot;

namespace ArenaShooter;

/// <summary>
/// Telegraphs an upcoming spawn with a blinking X, so enemies never pop in on top of
/// the player without warning. Emits <see cref="Elapsed"/> and frees itself when done.
/// </summary>
public partial class SpawnMarker : Node2D
{
    [Signal] public delegate void ElapsedEventHandler(Vector2 position);

    public float Duration { get; set; } = 0.8f;

    private float _time;

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();

        if (_time >= Duration)
        {
            EmitSignal(SignalName.Elapsed, Position);
            QueueFree();
        }
    }

    public override void _Draw()
    {
        const float s = 10f;
        float alpha = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(_time * 12f));
        var color = new Color(0.9f, 0.28f, 0.3f, alpha);
        DrawLine(new Vector2(-s, -s), new Vector2(s, s), color, 3f);
        DrawLine(new Vector2(-s, s), new Vector2(s, -s), color, 3f);
    }
}
