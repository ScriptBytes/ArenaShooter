using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Autoplay's thinking shop strategies (<c>--shop=smart|random</c>). Each call to <see cref="Next"/>
/// returns one button to press (buy, reroll, potion) or the Continue button when done, so the shop
/// refreshes between presses like it does for a player.
/// <list type="bullet">
/// <item><b>smart</b>: values each offer with rough per-stat weights for this class (Life Steal only for
/// classes that steal life, Minion Damage only with minions, nothing for stats at their cap), minus what it
/// would replace. Buys the best value per gold, rerolls a few times when nothing is worth it, and spends
/// what's left on health potions. "A decent player", for comparing spells and classes.</item>
/// <item><b>random</b>: buys, rerolls and stops at random (its own seeded generator), so many runs cover
/// the item space evenly. For measuring items: compare runs that bought an item with runs that didn't.</item>
/// </list>
/// The weights are opinions, so results under smart carry them; random doesn't.
/// </summary>
public sealed class ShopBot
{
    private const int MaxRerollsPerVisit = 4;
    private const int RerollReserve = 15; // gold to keep after a reroll, so there's something left to buy with

    private readonly Shop _shop;
    private readonly RunState _run;
    private readonly Player _player;
    private readonly ClassData _class;
    private readonly bool _random;
    private readonly RandomNumberGenerator _rng = new();
    private int _rerolls;

    public ShopBot(Shop shop, RunState run, Player player, ClassData playerClass, ShopStrategy strategy, ulong seed)
    {
        _shop = shop;
        _run = run;
        _player = player;
        _class = playerClass;
        _random = strategy == ShopStrategy.Random;
        _rng.Seed = seed;
    }

    /// <summary>Call when the shop opens.</summary>
    public void NewVisit() => _rerolls = 0;

    public readonly record struct Choice(BaseButton Button, Shop.Offer Offer, int Target, string Action);

    public Choice Next() => _random ? NextRandom() : NextSmart();

    private Choice NextRandom()
    {
        var affordable = _shop.Offers.Where(o => !o.BuyButton.Disabled).ToList();
        float roll = _rng.Randf();
        if (roll < 0.15f)
            return Done();
        if (affordable.Count > 0 && roll < 0.85f)
        {
            var offer = affordable[_rng.RandiRange(0, affordable.Count - 1)];
            int target = _rng.RandiRange(0, offer.Buttons.Count - 1);
            return new Choice(offer.Buttons[target], offer, target, "buy");
        }
        if (!_shop.RerollButton.Disabled && _rerolls < MaxRerollsPerVisit && _rng.Randf() < 0.5f)
            return Reroll();
        return Done();
    }

    private Choice NextSmart()
    {
        Shop.Offer best = null;
        int bestTarget = 0;
        float bestRatio = 0f;
        foreach (var offer in _shop.Offers.Where(o => !o.BuyButton.Disabled))
        {
            var (value, target) = NetValue(offer.Item);
            float ratio = value / Mathf.Max(offer.Price, 1);
            if (value > 0f && ratio > bestRatio)
            {
                best = offer;
                bestTarget = target;
                bestRatio = ratio;
            }
        }
        if (best != null)
            return new Choice(best.Buttons[bestTarget], best, bestTarget, "buy");

        if (!_shop.RerollButton.Disabled && _rerolls < MaxRerollsPerVisit && _run.Gold >= _shop.RerollCost + RerollReserve)
            return Reroll();

        // Leftover gold: health potions (a player would rather carry a potion than nothing).
        var potion = _shop.PotionOffers.FirstOrDefault(p => p.Item.HealthPotion && !p.BuyButton.Disabled);
        if (potion != null)
            return new Choice(potion.BuyButton, null, 0, "potion");
        return Done();
    }

    private Choice Reroll()
    {
        _rerolls++;
        return new Choice(_shop.RerollButton, null, 0, "reroll");
    }

    private Choice Done() => new(_shop.ContinueButton, null, 0, "continue");

    // Value gained minus the value of what it displaces; for gear with two targets (rings), the better one.
    private (float Value, int Target) NetValue(ItemInstance item)
    {
        float gain = Value(item);
        if (!item.IsGear)
            return (gain, 0);

        var targets = _run.Equipment.TargetsFor(item.Item);
        float best = float.MinValue;
        int bestIndex = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            float lost = _run.Equipment.Displaced(item.Item, targets[i]).Sum(Value);
            if (gain - lost > best)
            {
                best = gain - lost;
                bestIndex = i;
            }
        }
        return (best, bestIndex);
    }

    private float Value(ItemInstance item)
    {
        float value = item.Modifiers.Sum(m => StatWeight(m.Stat) * m.Value);
        if (item.Item.Weapon is { } weapon)
            value += WeaponValue(weapon, item.WeaponDamageMultiplier);
        return value;
    }

    // Damage per second, relative to the starting weapon: +1% weapon DPS is worth about half of +1% Damage
    // (which also boosts spells). A weapon of the other kind (melee vs ranged) gets no special treatment.
    private float WeaponValue(AbilityData weapon, float damageMultiplier)
    {
        static float Dps(AbilityData w, float mult) =>
            w.Damage * mult * (w is ProjectileAbility p ? p.Count : 1) / Mathf.Max(w.Cooldown, 0.05f);

        var start = _class.StartingWeapon.Weapon;
        return (Dps(weapon, damageMultiplier) / Dps(start, 1f) - 1f) * 50f;
    }

    private float StatWeight(StatType stat)
    {
        if (StatInfo.Cap(stat) is { } cap && _run.Stats.Get(stat) >= cap)
            return 0f;

        bool melee = _player.Abilities.Weapon?.Ability is MeleeArcAbility;
        var (weaponShare, minionShare) = DamageShares();
        float cdr = Mathf.Min(_run.Stats.Get(StatType.CooldownReduction), 90f) / 100f;
        return stat switch
        {
            StatType.MaxHealth => 1.2f,
            StatType.Armor => 3f,
            StatType.Damage => 1f,
            // Scaled by where this run's damage comes from: +1% Attack Speed is +1% weapon damage, +1% Cooldown
            // Reduction is a bit more than +1% spell damage (more as it nears its cap); Minion Damage only boosts minions.
            StatType.AttackSpeed => Mathf.Max(weaponShare, 0.2f),
            StatType.MoveSpeed => 0.5f,
            StatType.Range => melee ? 0.03f : 0.06f,
            StatType.PickupRange => 0.02f,
            StatType.CritChance => 0.7f,
            StatType.LifeSteal => StealsLife() ? 1.5f : 0f,
            StatType.MinionDamage => HasMinions() ? Mathf.Max(minionShare, 0.3f) : 0f,
            StatType.PotionHeal => 0.5f,
            StatType.CooldownReduction => Mathf.Max(1f - weaponShare, 0.2f) / (1f - cdr),
            StatType.HpRegen => 1f,
            StatType.Dodge => 1.5f,
            _ => 0f,
        };
    }

    // Share of this run's damage (Combat.Damage, the meter) from the weapon and from minions. Before any damage
    // (never in practice: the first shop comes after wave 1) assume a 50/50 weapon/spell split.
    private (float Weapon, float Minions) DamageShares()
    {
        var totals = Combat.Damage.Totals;
        float sum = totals.Values.Sum();
        if (sum <= 0f)
            return (0.5f, 0f);

        var minionNames = new HashSet<string>();
        foreach (var ability in _player.Abilities.Spells.Select(s => s.Ability).Append(_player.Abilities.Weapon?.Ability))
        {
            if (ability is SummonAbility { Minion: { } summoned })
                minionNames.Add(summoned.DisplayName);
            if (ability?.RaiseMinion is { } raised)
                minionNames.Add(raised.DisplayName);
        }
        // Every weapon this class can wear counts, so the starter's damage still counts after an upgrade.
        var weaponNames = _class.Weapons.Append(_class.StartingWeapon)
            .Where(i => i?.Weapon != null).Select(i => i.Weapon.DisplayName).ToHashSet();
        float weapon = totals.Where(t => weaponNames.Contains(t.Key)).Sum(t => t.Value);
        float minions = totals.Where(t => minionNames.Contains(t.Key)).Sum(t => t.Value);
        return (weapon / sum, minions / sum);
    }

    private bool StealsLife() =>
        _player.Abilities.Weapon?.Ability.LifeSteal > 0f || _player.Abilities.Spells.Any(s => s.Ability.LifeSteal > 0f);

    private bool HasMinions() =>
        _player.Abilities.Spells.Any(s => s.Ability is SummonAbility || s.Ability.RaiseMinion != null)
        || _player.Abilities.Weapon?.Ability.RaiseMinion != null;
}
