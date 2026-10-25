using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// An owned (or offered) item: the shared <see cref="ItemData"/> definition plus the rarity it rolled.
/// Resources are shared, so the rarity-scaled numbers live here instead of being written into the item.
/// Plain C#, immutable. Consumables have no rarity and never become instances (see PotionBelt).
/// </summary>
public sealed class ItemInstance
{
    public ItemInstance(ItemData item, RarityTier tier)
    {
        Item = item;
        Tier = tier;
        Modifiers = item.Modifiers.Select(m => (m.Stat, tier.Scale(m.Value))).ToList();
    }

    public ItemData Item { get; }
    public RarityTier Tier { get; }
    public Rarity Rarity => Tier.Rarity;

    /// <summary>The item's stat modifiers scaled by its rarity.</summary>
    public IReadOnlyList<(StatType Stat, float Value)> Modifiers { get; }

    public string DisplayName => Item.DisplayName;

    /// <summary>"Rare Spear"; Common items keep their plain name.</summary>
    public string FullName => Rarity == Rarity.Common ? DisplayName : $"{Tier.Label} {DisplayName}";
    public Color Color => Tier.Color;
    public bool IsGear => Item.IsGear;
    public EquipSlot Slot => Item.Slot;
    public bool TwoHanded => Item.TwoHanded;

    /// <summary>Base price scaled by rarity (the shop adds its per-wave increase on top).</summary>
    public int Price => Mathf.RoundToInt(Item.Price * Tier.PriceMultiplier);

    /// <summary>Gold back when this gear is replaced: half its rarity-scaled base price.</summary>
    public int SellValue => Price / 2;

    /// <summary>Damage multiplier for the basic attack this item gives (1 for non-weapons).</summary>
    public float WeaponDamageMultiplier => Item.Weapon != null ? Tier.WeaponDamageMultiplier : 1f;

    /// <summary>"Rare · Main Hand"; Common items show just the slot.</summary>
    public string SlotLabel => Rarity == Rarity.Common ? Item.SlotLabel : $"{Tier.Label} · {Item.SlotLabel}";

    /// <summary>For logs: "Spear[rare]".</summary>
    public string LogName => $"{DisplayName}[{Rarity.ToString().ToLower()}]";

    public string Describe() => Item.Describe(Modifiers, WeaponDamageMultiplier);

    /// <summary>Tooltip text: name, rarity and slot, then the stats.</summary>
    public string Tooltip() => $"{DisplayName}\n{SlotLabel}\n{Describe()}";
}
