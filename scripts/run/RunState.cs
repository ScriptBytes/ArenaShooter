using System.Collections.Generic;

namespace ArenaShooter;

/// <summary>
/// Everything that persists across waves within one run. Plain C#, owned by Main, so a
/// scene reload (restart) discards it. Career-mode persistence would live elsewhere.
/// </summary>
public class RunState
{
    public int Wave { get; set; }
    public int Gold { get; set; }
    public int Kills { get; set; }
    public PlayerStats Stats { get; } = new();
    public List<ItemData> Items { get; } = new();

    public bool TrySpend(int amount)
    {
        if (Gold < amount)
            return false;
        Gold -= amount;
        return true;
    }

    public void AddItem(ItemData item)
    {
        Items.Add(item);
        foreach (var modifier in item.Modifiers)
            Stats.Add(modifier.Stat, modifier.Value);
    }
}
