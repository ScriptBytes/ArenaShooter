using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArenaShooter;

/// <summary>
/// The career save file's contents (JSON, see <see cref="CareerStore"/>). Plain data: every field
/// has a default so a file missing it still loads, and fields this build doesn't know are kept in
/// <see cref="Extra"/> and written back, so an older build doesn't erase a newer build's data.
/// Store facts (total XP, ranks bought), not derived values (level, points), so tuning the level
/// curve later re-derives them instead of needing a migration.
/// </summary>
public class CareerSave
{
    /// <summary>Bump when a change needs a migration step in <see cref="CareerStore"/>, not for added fields.</summary>
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>Last write, ISO 8601 UTC. Informational only.</summary>
    public string Updated { get; set; } = "";

    /// <summary>One career character per class, keyed by <see cref="ClassData.Id"/> ("fighter").</summary>
    public Dictionary<string, CareerCharacter> Characters { get; set; } = new();

    [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; }

    /// <summary>Fills in nulls and clamps nonsense left by hand edits or older builds.</summary>
    public void Sanitize()
    {
        Characters ??= new();
        foreach (var key in Characters.Where(c => c.Value == null).Select(c => c.Key).ToList())
            Characters.Remove(key);
        foreach (var character in Characters.Values)
            character.Sanitize();
    }
}

public class CareerCharacter
{
    /// <summary>Total XP ever earned. Level and talent points are derived from it (CareerSettings).</summary>
    public int Xp { get; set; }

    /// <summary>Ranks bought per talent id (CareerTalent.Id). Unknown ids are ignored, so a removed talent refunds itself.</summary>
    public Dictionary<string, int> Talents { get; set; } = new();

    /// <summary><see cref="ItemData.Id"/> of the gear kept from a victory; starts equipped. Null for none.</summary>
    public string Heirloom { get; set; }

    /// <summary>
    /// The heirloom's <see cref="Rarity"/> by name ("Rare"). Missing (saves from before rarity) or unknown
    /// means Common. Added without a version bump: an older build keeps it as an unknown field.
    /// </summary>
    public string HeirloomRarity { get; set; }

    public int Runs { get; set; }
    public int Victories { get; set; }
    public int BestWave { get; set; }
    public int Kills { get; set; }

    [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; }

    public int Rank(string talentId) => Talents.GetValueOrDefault(talentId);

    /// <summary>Whether there's nothing to lose: no XP, talents, heirloom or record.</summary>
    public bool IsBlank() => Xp == 0 && Runs == 0 && Talents.Count == 0 && Heirloom == null;

    public void Sanitize()
    {
        Talents ??= new();
        Xp = System.Math.Max(Xp, 0);
        foreach (var key in Talents.Keys.ToList())
            Talents[key] = System.Math.Max(Talents[key], 0);
        if (string.IsNullOrWhiteSpace(Heirloom))
            Heirloom = null;
        if (Heirloom == null || string.IsNullOrWhiteSpace(HeirloomRarity))
            HeirloomRarity = null;
    }
}
