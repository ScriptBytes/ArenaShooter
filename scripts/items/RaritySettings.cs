using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// The rarity ladder (data/RaritySettings.tres): one <see cref="RarityTier"/> per <see cref="Rarity"/>,
/// in order. The shop rolls a tier for each offer with <see cref="Roll"/>; odds rise with the wave.
/// </summary>
[GlobalClass]
public partial class RaritySettings : Resource
{
    [Export] public Godot.Collections.Array<RarityTier> Tiers { get; set; } = new();

    /// <summary>The tier for <paramref name="rarity"/>, or the first (Common) if it isn't listed.</summary>
    public RarityTier Tier(Rarity rarity) => Tiers.FirstOrDefault(t => t.Rarity == rarity) ?? Tiers[0];

    public RarityTier Common => Tier(Rarity.Common);

    /// <summary>Each tier's chance (percent, summing to 100) at the shop after <paramref name="wave"/>.</summary>
    public float[] Chances(int wave)
    {
        var chances = Tiers.Select(t => t.Rarity == Rarity.Common ? 0f : t.ChanceAt(wave)).ToArray();
        float rare = chances.Sum();
        if (rare > 100f)
        {
            for (int i = 0; i < chances.Length; i++)
                chances[i] *= 100f / rare;
            rare = 100f;
        }
        int common = Tiers.ToList().FindIndex(t => t.Rarity == Rarity.Common);
        if (common >= 0)
            chances[common] = 100f - rare;
        return chances;
    }

    /// <summary>Rolls a tier for one shop offer at the shop after <paramref name="wave"/> (uses the seeded GD RNG).</summary>
    public RarityTier Roll(int wave)
    {
        var chances = Chances(wave);
        float roll = GD.Randf() * 100f;
        for (int i = 0; i < chances.Length; i++)
        {
            roll -= chances[i];
            if (roll < 0f)
                return Tiers[i];
        }
        return Common;
    }
}
