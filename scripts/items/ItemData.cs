using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Definition of a buyable item. Each item is a .tres file under data/items/, editable
/// in the Inspector; [GlobalClass] makes it show up in the editor's "New Resource" list.
/// Items with a <see cref="Slot"/> are gear (one per slot, see Equipment); the rest are
/// trinkets, which stack.
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

    /// <summary>Gold back when this gear is replaced.</summary>
    public int SellValue => Price / 2;

    public virtual string SlotLabel => !IsGear ? "Trinket · stacks"
        : TwoHanded ? "Main Hand · two-handed"
        : EquipSlotInfo.Label(Slot);

    public virtual string Describe()
    {
        var lines = new List<string>();
        if (Weapon != null)
        {
            lines.Add($"{Weapon.Damage:0.#} damage every {Weapon.Cooldown:0.##}s");
            if (Weapon.Description != "")
                lines.Add(Weapon.Description);
        }
        lines.AddRange(Modifiers.Select(m => m.Describe()));
        return string.Join("\n", lines);
    }
}
