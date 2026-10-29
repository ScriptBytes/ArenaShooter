using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Gear and stat listings shared by the shop and the mid-run gear screen (Tab), so both show
/// equipment the same way: a <see cref="PaperDoll"/> of the worn gear, then one row per trinket, in
/// rarity colors, with the item's stats as a hover tooltip. Give the container (or a parent) <see cref="UiStyles.TooltipTheme"/>.
/// </summary>
public static class GearViews
{
    /// <summary>Rebuilds <paramref name="box"/> with the paper doll of the six equipment slots, then the trinkets.</summary>
    public static void FillEquipment(VBoxContainer box, RunState run, int fontSize = 15, float dollScale = 1f)
    {
        foreach (var child in box.GetChildren())
        {
            box.RemoveChild(child);
            child.QueueFree();
        }

        AddRow(box, "Equipment", null, fontSize);
        box.AddChild(new PaperDoll(run.Equipment, dollScale));

        AddRow(box, "\nTrinkets", null, fontSize);
        if (run.Trinkets.Count == 0)
            AddRow(box, "-", null, fontSize);
        // One row per item and rarity: a Common and a Rare Whetstone are listed apart (their stats just add up).
        foreach (var group in run.Trinkets.GroupBy(t => (t.Item, t.Rarity)))
        {
            var first = group.First();
            AddRow(box, group.Count() > 1 ? $"{first.DisplayName} x{group.Count()}" : first.DisplayName, first, fontSize);
        }
    }

    private static void AddRow(VBoxContainer box, string text, ItemInstance item, int fontSize)
    {
        var row = new Label { Text = text };
        row.AddThemeFontSizeOverride("font_size", fontSize);
        if (item != null)
        {
            row.AddThemeColorOverride("font_color", item.Color);
            row.TooltipText = item.Tooltip();
            // Labels ignore the mouse by default, which would also stop the tooltip.
            row.MouseFilter = Control.MouseFilterEnum.Pass;
        }
        box.AddChild(row);
    }

    /// <summary>
    /// The run's stat bonuses, one line each. <paramref name="all"/> lists every stat (zeros too) and
    /// names the cap of capped stats; otherwise only non-zero stats (the shop's compact panel).
    /// </summary>
    public static List<string> StatLines(PlayerStats stats, bool all)
    {
        var lines = new List<string>();
        foreach (var stat in System.Enum.GetValues<StatType>())
        {
            float value = stats.Get(stat);
            if (!all && value == 0f)
                continue;
            string line = StatInfo.FormatTotal(stat, value);
            if (all && StatInfo.Cap(stat) is { } cap && value <= cap)
                line += $" (cap {cap:0}%)";
            lines.Add(line);
        }
        return lines;
    }
}
