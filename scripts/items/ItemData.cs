using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Definition of a buyable item. Each item is a .tres file under data/items/, editable
/// in the Inspector; [GlobalClass] makes it show up in the editor's "New Resource" list.
/// </summary>
[GlobalClass]
public partial class ItemData : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export] public int Price { get; set; } = 10;
    [Export] public Color IconColor { get; set; } = Colors.White;
    [Export] public Godot.Collections.Array<StatModifier> Modifiers { get; set; } = new();

    public string Describe() => string.Join("\n", Modifiers.Select(m => m.Describe()));
}
