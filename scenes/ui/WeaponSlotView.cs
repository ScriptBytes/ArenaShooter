using Godot;

namespace ArenaShooter;

/// <summary>
/// HUD slot for the equipped weapon (the always-on basic attack), left of the spell bar: the
/// weapon's color, its name in its rarity color, and a thin bar along the bottom that refills
/// between shots. Hover shows the item's stats. Drawn by hand like <see cref="AbilitySlotView"/>.
/// </summary>
public partial class WeaponSlotView : Control
{
    private const float SlotHeight = 72f;
    private const float SlotWidth = 150f;
    private static readonly Color CommonBorder = new(0.45f, 0.45f, 0.5f);

    public AbilityCaster Caster { get; set; }

    private ItemInstance _item;

    /// <summary>The main-hand item to show (its rarity colors the name and border).</summary>
    public ItemInstance Item
    {
        get => _item;
        set
        {
            _item = value;
            TooltipText = value != null ? $"{value.Tooltip()}\n\nWeapon: fires on its own at the nearest enemy" : "";
        }
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(SlotWidth, SlotHeight);
        // Hover for the tooltip; clicks fall through.
        MouseFilter = MouseFilterEnum.Pass;
    }

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        var rect = new Rect2(Vector2.Zero, Size);
        DrawRect(rect, new Color(0.08f, 0.08f, 0.1f, 0.85f));

        var slot = Caster?.Weapon;
        if (_item != null && slot != null)
        {
            var icon = new Rect2(8f, 8f, 28f, Size.Y - 22f);
            DrawRect(icon, slot.Ability.IconColor);

            float textX = icon.End.X + 8f;
            float width = Size.X - textX - 6f;
            DrawString(font, new Vector2(textX, 20f), "WEAPON", HorizontalAlignment.Left, width, 10, UiStyles.Dim);
            DrawMultilineString(font, new Vector2(textX, 36f), _item.FullName, HorizontalAlignment.Left, width, 15,
                maxLines: 2, modulate: _item.Color);

            // Fills up as the next shot comes back (weapons fire several times a second, so no countdown text).
            var bar = new Rect2(8f, Size.Y - 10f, Size.X - 16f, 4f);
            DrawRect(bar, new Color(0f, 0f, 0f, 0.6f));
            DrawRect(new Rect2(bar.Position, new Vector2(bar.Size.X * (1f - slot.CooldownFraction), bar.Size.Y)),
                new Color(slot.Ability.IconColor, 0.9f));
        }

        var border = _item == null || _item.Rarity == Rarity.Common ? CommonBorder : _item.Color;
        DrawRect(rect, border, filled: false, width: 2f);
    }
}
