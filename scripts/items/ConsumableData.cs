using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// A potion bought in the shop's potion shelf (data/consumables/). Bought copies go into the
/// <see cref="PotionBelt"/> and last until drunk, across waves. A health potion is an extra
/// charge for the Space potion; any other consumable is a timed buff drunk with keys 5+
/// (<see cref="ItemData.Modifiers"/> are the buff, not permanent stats).
/// </summary>
[GlobalClass]
public partial class ConsumableData : ItemData
{
    /// <summary>Heals like the Space potion (Player.PotionHeal) instead of giving a buff.</summary>
    [Export] public bool HealthPotion { get; set; }

    /// <summary>Buff length in seconds. Drinking again while it's active refreshes the timer.</summary>
    [Export] public float Duration { get; set; } = 10f;

    /// <summary>Most copies the player can carry; the shop stops selling it at this count.</summary>
    [Export] public int MaxCarry { get; set; } = 3;

    public override string SlotLabel => HealthPotion ? "Potion · Space" : "Potion · lasts until drunk";

    public override string Describe()
    {
        if (HealthPotion)
            return "An extra health potion charge. Used after the free one each wave.";

        var lines = new List<string> { $"For {Duration:0.#}s:" };
        lines.AddRange(Modifiers.Select(m => m.Describe()));
        return string.Join("\n", lines);
    }
}
