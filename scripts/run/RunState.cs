using System.Collections.Generic;
using System.Linq;

namespace ArenaShooter;

/// <summary>
/// Everything that persists across waves within one run. Plain C#, owned by Main, so a
/// scene reload (restart) discards it. Career-mode persistence would live elsewhere.
/// </summary>
public class RunState
{
    public int Wave { get; set; }
    public int Gold { get; set; }

    /// <summary>
    /// Gold left on the floor at the end of a wave (Brotato's "bag"). Each pickup in later
    /// waves draws the same amount again from the bag, doubling it, until the bag is empty.
    /// </summary>
    public int Bag { get; set; }
    public int Kills { get; set; }

    /// <summary>Summed bonuses from gear, trinkets and grants. Recalculated when they change.</summary>
    public PlayerStats Stats { get; private set; } = new();

    public Equipment Equipment { get; } = new();

    /// <summary>Slotless items. Copies stack, and copies of different rarities just add up.</summary>
    public List<ItemInstance> Trinkets { get; } = new();

    /// <summary>Health potion charges, bought potions and the heart-filled reserve.</summary>
    public PotionBelt Potions { get; } = new();

    // Bonuses that don't come from an item (Autoplay's --stat).
    private readonly PlayerStats _granted = new();

    /// <summary>Takes up to <paramref name="amount"/> from the bag and returns what was taken.</summary>
    public int DrawFromBag(int amount)
    {
        int bonus = System.Math.Min(Bag, amount);
        Bag -= bonus;
        return bonus;
    }

    public bool TrySpend(int amount)
    {
        if (Gold < amount)
            return false;
        Gold -= amount;
        return true;
    }

    public void GrantStat(StatType stat, float value)
    {
        _granted.Add(stat, value);
        RecalculateStats();
    }

    /// <summary>
    /// Adds an item. Gear goes into <paramref name="position"/> (default: its first target, see
    /// <see cref="Equipment.TargetsFor"/>). Returns the gear it replaced.
    /// </summary>
    public List<ItemInstance> AddItem(ItemInstance item, int position = -1)
    {
        var removed = new List<ItemInstance>();
        if (item.IsGear)
            removed = Equipment.Equip(item, position >= 0 ? position : Equipment.TargetsFor(item.Item)[0]);
        else
            Trinkets.Add(item);

        RecalculateStats();
        return removed;
    }

    /// <summary>Consumables have no rarity: they go straight into <see cref="Potions"/>.</summary>
    public void AddPotion(ConsumableData potion) => Potions.Add(potion);

    private void RecalculateStats()
    {
        var stats = _granted.Copy();
        foreach (var item in Trinkets.Concat(Equipment.Worn))
        {
            foreach (var (stat, value) in item.Modifiers)
                stats.Add(stat, value);
        }
        Stats = stats;
    }
}
