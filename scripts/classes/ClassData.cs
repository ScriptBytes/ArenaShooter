using Godot;

namespace ArenaShooter;

/// <summary>A playable class: base stats, starting weapon, and spell list to pick from (one .tres per class).</summary>
[GlobalClass]
public partial class ClassData : Resource
{
    /// <summary>
    /// Stable key for the career save. Unique among classes; never change it once shipped
    /// (saves reference it). The .tres file name is free to change.
    /// </summary>
    [Export] public string Id { get; set; } = "";

    [Export] public string DisplayName { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public Color Color { get; set; } = Colors.White;

    [ExportGroup("Sprite")]
    /// <summary>Placeholder art (art/), authored facing right. Unset = the plain circle.</summary>
    [Export] public Texture2D Sprite { get; set; }

    /// <summary>Drawn size as a multiple of the body's diameter: the art has a head and feet past the collider.</summary>
    [Export] public float SpriteScale { get; set; } = 1.7f;

    [ExportGroup("Base Stats")]
    [Export] public float MaxHealth { get; set; } = 20f;
    [Export] public float MoveSpeed { get; set; } = 300f;
    [Export] public float Armor { get; set; }

    [ExportGroup("Loadout")]
    /// <summary>Main-hand item at run start, and the fallback whenever the main hand empties.</summary>
    [Export] public ItemData StartingWeapon { get; set; }

    /// <summary>The class's full spell list. The player picks <see cref="SpellPicks"/> of them at run start.</summary>
    [Export] public Godot.Collections.Array<AbilityData> Spells { get; set; } = new();

    /// <summary>How many spells to pick from the list (at most 4, one per hotkey slot).</summary>
    [Export(PropertyHint.Range, "1,4,1")] public int SpellPicks { get; set; } = 3;
}
