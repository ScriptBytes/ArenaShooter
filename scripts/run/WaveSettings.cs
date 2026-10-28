using Godot;

namespace ArenaShooter;

/// <summary>
/// Difficulty knobs for one wave. A formula for now; swap for per-wave Resources once
/// waves need hand-authored enemy mixes. Tuned for a 20-wave run (Main.TotalWaves): damage grows
/// by percent so weak and strong enemies keep their difference, and speed stops growing at +40%
/// so the fastest enemy (Runner, 165) stays slower than every class.
/// </summary>
public readonly record struct WaveSettings(
    int Number,
    float Duration,
    float SpawnRateMultiplier,
    float EnemyHealthMultiplier,
    float EnemyDamageMultiplier,
    float EnemySpeedMultiplier)
{
    public static WaveSettings For(int wave)
    {
        int i = wave - 1;
        return new WaveSettings(
            Number: wave,
            Duration: Mathf.Min(30f + 10f * i, 60f),
            SpawnRateMultiplier: 1f + 0.2f * i,
            EnemyHealthMultiplier: 1f + 0.35f * i,
            EnemyDamageMultiplier: 1f + 0.15f * i,
            EnemySpeedMultiplier: Mathf.Min(1f + 0.05f * i, 1.4f));
    }
}
