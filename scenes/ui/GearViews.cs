using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Gear and stat listings shared by the shop and the mid-run gear screen (Tab), so both show
/// equipment the same way: one row per slot and per trinket, in rarity colors, with the item's
/// stats as a hover tooltip. Give the container (or a parent) <see cref="UiStyles.TooltipTheme"/>.
/// </summary>
public static class GearViews
{
    /// <summary>Rebuilds <paramref name="box"/> with the six equipment slots, then the trinkets.</summary>
    public static void FillEquipment(VBoxContainer box, RunState run, int fontSize = 15)
    {
        foreach (var child in box.GetChildren())
        {
            box.RemoveChild(child);
            child.QueueFree();
        }

        var equipment = run.Equipment;
        AddRow(box, "Equipment\n", null, fontSize);
        for (int i = 0; i < Equipment.Layout.Length; i++)
        {
            var slot = Equipment.Layout[i];
            string name = equipment[i]?.DisplayName
                ?? (slot == EquipSlot.OffHand && equipment.IsTwoHanding ? "(two-handed)" : "-");
            AddRow(box, $"{EquipSlotInfo.Label(slot)}: {name}", equipment[i], fontSize);
        }

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
