using Godot;

namespace ArenaShooter;

/// <summary>The pool of items the shop can offer. New items must be added here to appear.</summary>
[GlobalClass]
public partial class ItemCatalog : Resource
{
    [Export] public Godot.Collections.Array<ItemData> Items { get; set; } = new();
}
