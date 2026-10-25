using System;
using Godot;

namespace ArenaShooter;

/// <summary>Tuning for one rarity tier: how much it scales an item, how it looks, and how often the shop rolls it.</summary>
[GlobalClass]
public partial class RarityTier : Resource
{
    [Export] public Rarity Rarity { get; set; }

    /// <summary>Name and border color in the shop, equipment panel and tooltips.</summary>
    [Export] public Color Color { get; set; } = Colors.White;

    /// <summary>Multiplies an item's positive stat modifiers (downsides stay as they are).</summary>
    [Export] public float StatMultiplier { get; set; } = 1f;

    /// <summary>Multiplies the item's base price (and so its refund).</summary>
    [Export] public float PriceMultiplier { get; set; } = 1f;

    /// <summary>Weapons only: multiplies the basic attack's damage, on top of the Damage stat.</summary>
    [Export] public float WeaponDamageMultiplier { get; set; } = 1f;

    /// <summary>
    /// Percent chance per shop offer at the first shop (after wave 1), rising by
    /// <see cref="ChancePerWave"/> each later shop, up to <see cref="MaxChance"/>.
    /// Ignored for Common, which gets whatever chance is left.
    /// </summary>
    [ExportGroup("Odds")]
    [Export] public float Chance { get; set; }
    [Export] public float ChancePerWave { get; set; }
    [Export] public float MaxChance { get; set; } = 100f;

    public string Label => Rarity.ToString();

    /// <summary>Chance (percent) at the shop after <paramref name="wave"/>, before normalizing.</summary>
    public float ChanceAt(int wave) =>
        Mathf.Clamp(Chance + ChancePerWave * Math.Max(wave - 1, 0), 0f, MaxChance);

    /// <summary>
    /// Scales one stat modifier. Stats are whole numbers, so the result is rounded, and a higher tier
    /// always adds at least 1 so it never shows the same number as Common. Negative values (trade-offs
    /// like Berserker Charm's Max HP) aren't scaled: a rarer item is strictly better.
    /// </summary>
    public float Scale(float value)
    {
        if (value <= 0f || StatMultiplier <= 1f)
            return value;
        float scaled = MathF.Round(value * StatMultiplier, MidpointRounding.AwayFromZero);
        return MathF.Max(scaled, value + 1f);
    }
}
