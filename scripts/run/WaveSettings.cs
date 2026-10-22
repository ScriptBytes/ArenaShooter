using Godot;

namespace ArenaShooter;

/// <summary>
/// Difficulty knobs for one wave. A formula for now; swap for per-wave Resources once
/// waves need hand-authored enemy mixes.
/// </summary>
public readonly record struct WaveSettings(
    float Duration,
    float SpawnRateMultiplier,
    float EnemyHealthMultiplier,
    float EnemyDamageBonus,
    float EnemySpeedMultiplier)
{
    public static WaveSettings For(int wave)
    {
        int i = wave - 1;
        return new WaveSettings(
            Duration: Mathf.Min(30f + 10f * i, 60f),
            SpawnRateMultiplier: 1f + 0.2f * i,
            EnemyHealthMultiplier: 1f + 0.35f * i,
            EnemyDamageBonus: i,
            EnemySpeedMultiplier: 1f + 0.05f * i);
    }
}
