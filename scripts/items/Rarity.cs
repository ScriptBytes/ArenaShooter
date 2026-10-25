namespace ArenaShooter;

/// <summary>
/// Item rarity, rolled per shop offer (see <see cref="RaritySettings"/>). Higher tiers scale an item's
/// stats, price and (for weapons) damage. Saved by name in the career heirloom, so renaming an entry
/// orphans saved data; adding one (e.g. Legendary) is safe.
/// </summary>
public enum Rarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
}
