using System.Collections.Generic;
using System.Linq;

namespace ArenaShooter;

/// <summary>
/// The gear the player wears: at most one item per position in <see cref="Layout"/>. Gear for a
/// full slot replaces what's there. A two-handed weapon fills the off hand too, so equipping one
/// removes the off-hand item, and equipping an off-hand item removes a two-hander (the class's
/// starting weapon goes back into the main hand). Holds item instances (item + rarity); the swap rules only
/// look at the item definition. Plain C#, owned by RunState.
/// </summary>
public class Equipment
{
    /// <summary>Slot positions in display order. Ring appears twice: you can wear two.</summary>
    public static readonly EquipSlot[] Layout =
    {
        EquipSlot.MainHand, EquipSlot.OffHand, EquipSlot.Armor, EquipSlot.Ring, EquipSlot.Ring, EquipSlot.Amulet,
    };

    private const int MainHandPosition = 0;
    private const int OffHandPosition = 1;

    private readonly ItemInstance[] _items = new ItemInstance[Layout.Length];

    /// <summary>Goes back into the main hand whenever it would otherwise be empty.</summary>
    public ItemInstance StartingWeapon { get; set; }

    public ItemInstance this[int position] => _items[position];
    public ItemInstance MainHand => _items[MainHandPosition];
    public bool IsTwoHanding => MainHand?.TwoHanded == true;
    public IEnumerable<ItemInstance> Worn => _items.Where(i => i != null);

    /// <summary>The basic attack the main hand gives.</summary>
    public AbilityData Weapon => WeaponItem?.Item.Weapon;

    /// <summary>The basic attack's damage multiplier from the weapon's rarity.</summary>
    public float WeaponDamageMultiplier => WeaponItem?.WeaponDamageMultiplier ?? 1f;

    private ItemInstance WeaponItem => MainHand?.Item.Weapon != null ? MainHand : StartingWeapon;

    /// <summary>Whether any rarity of <paramref name="item"/> is worn.</summary>
    public bool IsWorn(ItemData item) => _items.Any(i => i?.Item == item);

    /// <summary>
    /// Positions <paramref name="item"/> can go into: an empty matching slot if there is one,
    /// otherwise every matching slot (both rings), so the player picks which to replace.
    /// </summary>
    public List<int> TargetsFor(ItemData item)
    {
        var matching = Enumerable.Range(0, Layout.Length).Where(i => Layout[i] == item.Slot).ToList();
        int empty = matching.FindIndex(i => _items[i] == null);
        return empty >= 0 ? new List<int> { matching[empty] } : matching;
    }

    /// <summary>What equipping <paramref name="item"/> at <paramref name="position"/> would remove.</summary>
    public List<ItemInstance> Displaced(ItemData item, int position) =>
        DisplacedPositions(item, position).Select(i => _items[i]).ToList();

    /// <summary>Equips <paramref name="item"/> and returns the items it removed.</summary>
    public List<ItemInstance> Equip(ItemInstance item, int position)
    {
        var removed = Displaced(item.Item, position);
        foreach (int i in DisplacedPositions(item.Item, position))
            _items[i] = null;

        _items[position] = item;
        _items[MainHandPosition] ??= StartingWeapon;
        return removed;
    }

    private IEnumerable<int> DisplacedPositions(ItemData item, int position)
    {
        if (_items[position] != null)
            yield return position;
        if (item.TwoHanded && position != OffHandPosition && _items[OffHandPosition] != null)
            yield return OffHandPosition;
        if (item.Slot == EquipSlot.OffHand && IsTwoHanding)
            yield return MainHandPosition;
    }
}
