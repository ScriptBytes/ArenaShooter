using System;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Telegraph for a blast that lands after a delay (Meteor): an outline of the area that fills
/// in as the moment approaches, then runs the blast. It's in <see cref="Groups.Transient"/>,
/// so a wave ending first cancels it, and it stops while the game is paused.
/// </summary>
public partial class DelayedBlast : Node2D
{
    public float Radius { get; set; } = 50f;
    public Color Color { get; set; } = Colors.White;
    public float Delay { get; set; } = 1f;
    public Action OnLand { get; set; }

    private float _time;

    /// <summary>Adds the telegraph to the world. Deferred so it's safe from physics callbacks.</summary>
    public static void Spawn(Node world, Vector2 position, float radius, Color color, float delay, Action onLand)
    {
        var blast = new DelayedBlast
        {
            Position = position,
            Radius = radius,
            Color = color,
            Delay = delay,
            OnLand = onLand,
        };
        world.CallDeferred(Node.MethodName.AddChild, blast);
    }

    public override void _Ready()
    {
        AddToGroup(Groups.Transient);
        ZIndex = -1; // under the enemies walking through it
    }

    public override void _PhysicsProcess(double delta)
    {
        _time += (float)delta;
        QueueRedraw();

        if (_time < Delay)
            return;

        OnLand?.Invoke();
        QueueFree();
    }

    public override void _Draw()
    {
        float t = Mathf.Clamp(_time / Delay, 0f, 1f);
        DrawCircle(Vector2.Zero, Radius * t, new Color(Color, 0.25f));
        DrawArc(Vector2.Zero, Radius, 0f, Mathf.Tau, 48, new Color(Color, 0.8f), 2f);

        // The meteor itself, dropping in from above the target.
        float drop = (1f - t) * 220f;
        DrawCircle(new Vector2(drop * 0.35f, -drop), 6f + 6f * t, Color);
    }
}
