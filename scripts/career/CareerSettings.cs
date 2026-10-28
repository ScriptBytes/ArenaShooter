using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Career tuning (data/career/CareerSettings.tres): XP rewards, the level curve, and the talents
/// levels pay for. Each level after the first gives one talent point. Levels are derived from
/// total XP, so changing the curve re-levels existing saves.
/// </summary>
[GlobalClass]
public partial class CareerSettings : Resource
{
    [ExportGroup("XP")]
    // Tuned for 20-wave runs, where a full run is ~3000 kills: kills are worth a tenth of a point each
    // (rounded per run), so waves and victory still matter.
    [Export] public float XpPerKill { get; set; } = 0.1f;
    [Export] public int XpPerWaveCleared { get; set; } = 20;
    [Export] public int XpVictoryBonus { get; set; } = 100;

    [ExportGroup("Levels")]
    /// <summary>XP from level 1 to 2; each later level costs <see cref="XpStepPerLevel"/> more.</summary>
    [Export] public int XpFirstLevel { get; set; } = 60;
    [Export] public int XpStepPerLevel { get; set; } = 40;
    [Export] public int MaxLevel { get; set; } = 25;

    [Export] public Godot.Collections.Array<CareerTalent> Talents { get; set; } = new();

    public readonly record struct Reward(int Kills, int WavesCleared, bool Victory, int KillXp, int WaveXp, int VictoryXp)
    {
        public int Total => KillXp + WaveXp + VictoryXp;
    }

    public Reward RewardFor(int kills, int wavesCleared, bool victory) => new(
        kills, wavesCleared, victory,
        Mathf.RoundToInt(kills * XpPerKill), wavesCleared * XpPerWaveCleared, victory ? XpVictoryBonus : 0);

    /// <summary>XP needed to go from <paramref name="level"/> to the next.</summary>
    public int XpToNext(int level) => XpFirstLevel + XpStepPerLevel * (level - 1);

    /// <summary>Total XP at which <paramref name="level"/> is reached (level 1 = 0).</summary>
    public int XpForLevel(int level)
    {
        int total = 0;
        for (int l = 1; l < level; l++)
            total += XpToNext(l);
        return total;
    }

    public int LevelFor(int xp)
    {
        int level = 1;
        while (level < MaxLevel && xp >= XpForLevel(level + 1))
            level++;
        return level;
    }

    public int PointsEarned(CareerCharacter character) => LevelFor(character.Xp) - 1;

    /// <summary>Points spent on talents that still exist, each clamped to its max rank.</summary>
    public int PointsSpent(CareerCharacter character) =>
        Talents.Sum(t => System.Math.Min(character.Rank(t.Id), t.MaxRank));

    public int PointsFree(CareerCharacter character) => PointsEarned(character) - PointsSpent(character);

    public CareerTalent FindTalent(string id) => Talents.FirstOrDefault(t => t.Id == id);

    /// <summary>The stat bonuses a character's talents give, for <see cref="RunState.GrantStat"/>.</summary>
    public IEnumerable<(StatType Stat, float Value)> Bonuses(CareerCharacter character) =>
        Talents.Where(t => character.Rank(t.Id) > 0)
            .Select(t => (t.Stat, t.PerRank * System.Math.Min(character.Rank(t.Id), t.MaxRank)));
}
