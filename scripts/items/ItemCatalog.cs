using Godot;

namespace ArenaShooter;

/// <summary>
/// What the shop can sell. New items must be added here to appear: <see cref="Items"/> in the
/// random offers, <see cref="Consumables"/> on the always-stocked potion shelf.
/// </summary>
[GlobalClass]
public partial class ItemCatalog : Resource
{
    [Export] public Godot.Collections.Array<ItemData> Items { get; set; } = new();

    /// <summary>Potions. Buff potions get hotkeys 5, 6… in this order.</summary>
    [Export] public Godot.Collections.Array<ConsumableData> Consumables { get; set; } = new();
}
