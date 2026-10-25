using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Definition of a buyable item. Each item is a .tres file under data/items/, editable
/// in the Inspector; [GlobalClass] makes it show up in the editor's "New Resource" list.
/// Items with a <see cref="Slot"/> are gear (one per slot, see Equipment); the rest are
/// trinkets, which stack. Shared and never mutated: what the player owns is an <see cref="ItemInstance"/>
/// (this item plus a rolled <see cref="Rarity"/>).
/// </summary>
[GlobalClass]
public partial class ItemData : Resource
{
    /// <summary>
    /// Stable key for saves (career heirloom). Unique among all items, consumables and class
    /// weapons; never change it once shipped. The .tres file name is free to change.
    /// </summary>
    [Export] public string Id { get; set; } = "";

    [Export] public string DisplayName { get; set; } = "";
    [Export] public int Price { get; set; } = 10;
    [Export] public Color IconColor { get; set; } = Colors.White;
    [Export] public EquipSlot Slot { get; set; }
    [Export] public Godot.Collections.Array<StatModifier> Modifiers { get; set; } = new();

    /// <summary>Main-hand items only: the basic attack this weapon gives.</summary>
    [ExportGroup("Weapon")]
    [Export] public AbilityData Weapon { get; set; }

    /// <summary>Takes the off-hand slot too.</summary>
    [Export] public bool TwoHanded { get; set; }

    public bool IsGear => Slot != EquipSlot.None;

    public virtual string SlotLabel => !IsGear ? "Trinket · stacks"
        : TwoHanded ? "Main Hand · two-handed"
        : EquipSlotInfo.Label(Slot);

    /// <summary>The item's Common stats. Owned and offered items describe themselves through ItemInstance.</summary>
    public virtual string Describe() => Describe(Modifiers.Select(m => (m.Stat, m.Value)), 1f);

    /// <summary>Describes the item with the given (rarity-scaled) modifiers and weapon damage multiplier.</summary>
    public string Describe(IEnumerable<(StatType Stat, float Value)> modifiers, float weaponDamageMultiplier)
    {
        var lines = new List<string>();
        if (Weapon != null)
        {
            lines.Add($"{Weapon.Damage * weaponDamageMultiplier:0.#} damage every {Weapon.Cooldown:0.##}s");
            if (Weapon.Description != "")
                lines.Add(Weapon.Description);
        }
        lines.AddRange(modifiers.Select(m => StatInfo.Format(m.Stat, m.Value)));
        return string.Join("\n", lines);
    }
}
