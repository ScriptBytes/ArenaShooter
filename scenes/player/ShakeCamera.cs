using Godot;

namespace ArenaShooter;

/// <summary>
/// Player camera with trauma-based screen shake: hits add trauma (0–1), which decays over
/// time, and the shake offset scales with trauma squared so small bumps stay subtle. The player's
/// Screen shake setting (<see cref="GameSettings"/>) scales it.
/// </summary>
public partial class ShakeCamera : Camera2D
{
    [Export] public float MaxOffset { get; set; } = 18f;
    [Export] public float TraumaDecay { get; set; } = 2.5f;

    private float _trauma;

    /// <summary>Shakes the player's camera, if there is one.</summary>
    public static void Shake(SceneTree tree, float trauma)
    {
        if (tree.GetFirstNodeInGroup(Groups.Player) is Player player)
            player.Camera.AddTrauma(trauma);
    }

    public override void _Ready()
    {
        // Keep decaying while the tree is paused, so the view doesn't freeze off-center in the shop.
        ProcessMode = ProcessModeEnum.Always;
    }

    public void AddTrauma(float amount)
    {
        _trauma = Mathf.Min(_trauma + amount, 1f);
    }

    public override void _Process(double delta)
    {
        if (_trauma <= 0f)
        {
            Offset = Vector2.Zero;
            return;
        }

        _trauma = Mathf.Max(_trauma - TraumaDecay * (float)delta, 0f);
        // The Screen shake setting (0–1) scales the offset only, so the random draws stay the same.
        float strength = _trauma * _trauma * MaxOffset * (GameSettings.Instance?.ScreenShake ?? 1f);
        Offset = new Vector2((float)GD.RandRange(-1.0, 1.0), (float)GD.RandRange(-1.0, 1.0)) * strength;
    }
}
