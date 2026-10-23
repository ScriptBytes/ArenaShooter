using Godot;

namespace ArenaShooter;

/// <summary>
/// A permanent career upgrade bought with talent points: each rank adds <see cref="PerRank"/> to
/// <see cref="Stat"/> at the start of every career run. Listed in CareerSettings.
/// </summary>
[GlobalClass]
public partial class CareerTalent : Resource
{
    /// <summary>Key in the save file. Never rename it once shipped, or players lose those ranks (they're refunded).</summary>
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public StatType Stat { get; set; }
    [Export] public float PerRank { get; set; } = 1f;
    [Export(PropertyHint.Range, "1,20,1")] public int MaxRank { get; set; } = 5;

    public string Describe(int ranks) => StatInfo.Format(Stat, PerRank * System.Math.Max(ranks, 1));
}
