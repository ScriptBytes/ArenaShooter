using System.Collections.Generic;
using System.Linq;

namespace ArenaShooter;

/// <summary>
/// The player's potions for the run. Plain C#, owned by RunState; Player drinks from it.
/// <list type="bullet">
/// <item><b>Charges:</b> free health potions, refilled to Player.PotionsPerWave each wave.</item>
/// <item><b>Stock:</b> bought consumables (health potions and buff potions). They carry over until drunk.</item>
/// <item><b>Reserve:</b> a second health potion that hearts fill a little at a time, up to
/// <see cref="ReserveCapacity"/>. Drunk only when there's no full potion left. Carries over.</item>
/// </list>
/// </summary>
public class PotionBelt
{
    private readonly Dictionary<ConsumableData, int> _stock = new();

    /// <summary>Every consumable the shop sells. The buff potions among them get hotkeys 5, 6… in this order.</summary>
    public IReadOnlyList<ConsumableData> Catalog { get; private set; } = new List<ConsumableData>();
    public IReadOnlyList<ConsumableData> Hotbar { get; private set; } = new List<ConsumableData>();

    public int Charges { get; private set; }
    public float Reserve { get; private set; }

    /// <summary>Set by Player to its potion heal, so a full reserve is worth one potion.</summary>
    public float ReserveCapacity { get; set; } = 10f;

    public bool ReserveFull => Reserve >= ReserveCapacity;

    /// <summary>Full health potions available: this wave's charges plus bought ones.</summary>
    public int HealthPotions => Charges + _stock.Where(s => s.Key.HealthPotion).Sum(s => s.Value);

    public void SetCatalog(IEnumerable<ConsumableData> catalog)
    {
        Catalog = catalog.ToList();
        Hotbar = Catalog.Where(c => !c.HealthPotion).ToList();
    }

    public int Count(ConsumableData consumable) => _stock.GetValueOrDefault(consumable);

    public bool CanCarryMore(ConsumableData consumable) => Count(consumable) < consumable.MaxCarry;

    public void Add(ConsumableData consumable) => _stock[consumable] = Count(consumable) + 1;

    public bool Take(ConsumableData consumable)
    {
        if (Count(consumable) <= 0)
            return false;
        _stock[consumable]--;
        return true;
    }

    public void RefillCharges(int perWave) => Charges = perWave;

    /// <summary>Spends a full health potion: this wave's free charge first, then a bought one.</summary>
    public bool TakeHealthPotion()
    {
        if (Charges > 0)
        {
            Charges--;
            return true;
        }

        var bought = _stock.Keys.FirstOrDefault(c => c.HealthPotion && _stock[c] > 0);
        return bought != null && Take(bought);
    }

    /// <summary>Pours up to <paramref name="amount"/> into the reserve. Returns what fit; the rest is lost.</summary>
    public float FillReserve(float amount)
    {
        float added = System.MathF.Min(amount, System.MathF.Max(ReserveCapacity - Reserve, 0f));
        Reserve += added;
        return added;
    }

    /// <summary>Empties the reserve and returns what was in it.</summary>
    public float DrainReserve()
    {
        float amount = Reserve;
        Reserve = 0f;
        return amount;
    }
}
