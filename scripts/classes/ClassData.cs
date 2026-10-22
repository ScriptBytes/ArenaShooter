using Godot;

namespace ArenaShooter;

/// <summary>A playable class: base stats, starting weapon, and spell list (one .tres per class).</summary>
[GlobalClass]
public partial class ClassData : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public Color Color { get; set; } = Colors.White;

    [ExportGroup("Base Stats")]
    [Export] public float MaxHealth { get; set; } = 20f;
    [Export] public float MoveSpeed { get; set; } = 300f;
    [Export] public float Armor { get; set; }

    [ExportGroup("Loadout")]
    [Export] public AbilityData Weapon { get; set; }

    /// <summary>Spells in hotkey order (slots 1–4).</summary>
    [Export] public Godot.Collections.Array<AbilityData> Spells { get; set; } = new();
}
