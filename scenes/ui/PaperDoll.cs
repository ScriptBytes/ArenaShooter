using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// The worn gear as an RPG paper doll: one slot box per <see cref="Equipment.Layout"/> position,
/// placed on a body silhouette (amulet at the neck, armor on the chest, weapons at the sides, rings
/// at the hands). Each filled slot shows the item name in its rarity color, with its stats as a tooltip.
/// </summary>
public partial class PaperDoll : Control
{
    private static readonly Vector2 SlotSize = new(64f, 54f);

    // Top-left corner per Equipment.Layout position: main hand, off hand, armor, ring, ring, amulet.
    // The main hand is on the viewer's left, like a character facing you.
    private static readonly Vector2[] SlotPositions =
    {
        new(0f, 70f), new(140f, 70f), new(70f, 104f), new(0f, 130f), new(140f, 130f), new(70f, 44f),
    };

    private static readonly Color Body = new(1f, 1f, 1f, 0.07f);
    private static readonly Color SlotBackground = new(0.09f, 0.09f, 0.12f);

    private readonly float _scale = 1f;

    /// <param name="scale">Grows the whole doll (slots, text, silhouette) for roomier screens.</param>
    public PaperDoll(Equipment equipment, float scale = 1f)
    {
        _scale = scale;
        CustomMinimumSize = new Vector2(204f, 230f) * scale;
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        MouseFilter = MouseFilterEnum.Pass;
        for (int i = 0; i < Equipment.Layout.Length; i++)
            AddChild(BuildSlot(equipment, i));
    }

    // Godot instantiates nodes with a parameterless constructor (e.g. on hot reload).
    public PaperDoll() { }

    private Control BuildSlot(Equipment equipment, int position)
    {
        var item = equipment[position];
        var slot = Equipment.Layout[position];
        var style = new StyleBoxFlat { BgColor = SlotBackground };
        style.SetCornerRadiusAll(5);
        style.SetBorderWidthAll(item != null ? 2 : 1);
        style.BorderColor = item?.Color ?? new Color(1f, 1f, 1f, 0.18f);

        var size = SlotSize * _scale;
        var box = new Panel { Position = SlotPositions[position] * _scale, Size = size, TooltipText = item?.Tooltip() ?? "" };
        box.AddThemeStyleboxOverride("panel", style);

        if (item != null)
            box.AddChild(new ColorRect
            {
                Color = item.Item.IconColor,
                Position = new Vector2(5f, 5f) * _scale,
                Size = new Vector2(size.X - 10f * _scale, 4f * _scale),
                MouseFilter = MouseFilterEnum.Ignore,
            });

        string text = item?.DisplayName
            ?? (slot == EquipSlot.OffHand && equipment.IsTwoHanding ? "(two-handed)" : EquipSlotInfo.Label(slot));
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.Word,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
        };
        // Sized after wrapping is on: a Control never shrinks below its minimum size, and an
        // unwrapped label's minimum is the whole name on one line.
        label.Position = new Vector2(2f, 9f) * _scale;
        label.Size = new Vector2(size.X - 4f * _scale, size.Y - 10f * _scale);
        label.AddThemeFontSizeOverride("font_size", FontSizeFor(text, label.Size.X));
        label.AddThemeConstantOverride("line_spacing", -3);
        label.AddThemeColorOverride("font_color", item?.Color ?? UiStyles.Dim);
        box.AddChild(label);
        return box;
    }

    // 11 px (scaled), smaller when one word of the name ("Regeneration") wouldn't fit on a line.
    private int FontSizeFor(string text, float width)
    {
        var font = ThemeDB.FallbackFont;
        int size = Mathf.RoundToInt(11f * _scale);
        int smallest = Mathf.RoundToInt(8f * _scale);
        while (size > smallest && text.Split(' ').Any(word =>
                   font.GetStringSize(word, HorizontalAlignment.Left, -1, size).X > width))
            size--;
        return size;
    }

    // The silhouette behind the slots: head, torso, arms down to the ring hands, legs.
    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One * _scale);
        DrawCircle(new Vector2(102f, 22f), 17f, Body);
        DrawRect(new Rect2(94f, 36f, 16f, 10f), Body);
        DrawColoredPolygon([new(48f, 64f), new(156f, 64f), new(136f, 162f), new(68f, 162f)], Body);
        DrawLine(new Vector2(52f, 68f), new Vector2(32f, 156f), Body, 18f);
        DrawLine(new Vector2(152f, 68f), new Vector2(172f, 156f), Body, 18f);
        DrawRect(new Rect2(72f, 156f, 26f, 74f), Body);
        DrawRect(new Rect2(106f, 156f, 26f, 74f), Body);
    }
}
