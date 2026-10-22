using Godot;

namespace ArenaShooter;

/// <summary>
/// One hotbar slot: icon color, hotkey, cooldown sweep, and auto-cast state.
/// Right-click toggles auto-cast. Drawn by hand since it's all placeholder art for now.
/// </summary>
public partial class AbilitySlotView : Control
{
    private const float SlotSize = 72f;

    public int Index { get; set; }
    public AbilityCaster Caster { get; set; }

    private AbilitySlot Slot =>
        Caster != null && Index < Caster.Spells.Count ? Caster.Spells[Index] : null;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(SlotSize, SlotSize);
        if (Slot != null)
        {
            TooltipText = $"{Slot.Ability.DisplayName}\n{Slot.Ability.Description}\n" +
                          $"Base crit: {Slot.Ability.CritChance:0}% for x{Slot.Ability.CritMultiplier:0.#} damage\n\n" +
                          $"Press {Index + 1} to cast (aims at mouse)\nShift+{Index + 1} or right-click: toggle auto-cast";
        }
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
        {
            Caster?.ToggleAutoCast(Index);
            AcceptEvent();
        }
    }

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        var rect = new Rect2(Vector2.Zero, Size);
        var slot = Slot;

        DrawRect(rect, new Color(0.08f, 0.08f, 0.1f, 0.85f));

        if (slot != null)
        {
            var icon = rect.Grow(-8f);
            DrawRect(icon, slot.Ability.IconColor);

            // Cooldown shade shrinks from the top as the spell comes back.
            float shade = icon.Size.Y * slot.CooldownFraction;
            DrawRect(new Rect2(icon.Position, new Vector2(icon.Size.X, shade)), new Color(0, 0, 0, 0.7f));

            if (!slot.IsReady)
            {
                DrawOutlinedString(font, new Vector2(0, Size.Y / 2f + 8f), $"{slot.CooldownLeft:0.0}",
                    HorizontalAlignment.Center, Size.X, 20, Colors.White);
            }

            if (slot.AutoCast)
            {
                DrawOutlinedString(font, new Vector2(0, Size.Y - 4f), "AUTO",
                    HorizontalAlignment.Right, Size.X - 4f, 11, UiStyles.Gold);
            }
        }

        var border = slot?.AutoCast == true ? UiStyles.Gold : new Color(0.45f, 0.45f, 0.5f);
        DrawRect(rect, border, filled: false, width: 2f);
        DrawOutlinedString(font, new Vector2(5f, 16f), $"{Index + 1}", HorizontalAlignment.Left, -1, 14, Colors.White);
    }

    // Text sits on top of arbitrary icon colors, so give it a dark outline to stay legible.
    private void DrawOutlinedString(Font font, Vector2 pos, string text, HorizontalAlignment align,
        float width, int size, Color color)
    {
        DrawStringOutline(font, pos, text, align, width, size, 4, Colors.Black);
        DrawString(font, pos, text, align, width, size, color);
    }
}
