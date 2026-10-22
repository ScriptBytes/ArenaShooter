using Godot;

namespace ArenaShooter;

/// <summary>One stat change on an item, e.g. Damage +15. Percent stats use whole numbers.</summary>
[GlobalClass]
public partial class StatModifier : Resource
{
    [Export] public StatType Stat { get; set; }
    [Export] public float Value { get; set; }

    public string Describe() => StatInfo.Format(Stat, Value);
}
