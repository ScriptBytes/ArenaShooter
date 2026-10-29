using Godot;

namespace ArenaShooter;

/// <summary>
/// The damage and healing meters together. On the HUD (<see cref="Tabbed"/> off) both are stacked and
/// show the current wave, each hidden until it has something. In the shop (<see cref="Tabbed"/> on) one
/// meter shows at a time, picked by Damage / Healing and Last wave / Whole run tabs, so it stays short.
/// The tab choice is kept between shop visits.
/// </summary>
public partial class CombatMeters : VBoxContainer
{
    public bool Tabbed { get; init; }

    private CombatMeter _damage;
    private CombatMeter _healing;
    private bool _showHealing;
    private bool _wholeRun;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", Tabbed ? 6 : 14);
        if (Tabbed)
        {
            AddTabs(("Damage", false), ("Healing", true), _showHealing, on => _showHealing = on);
            AddTabs(("Last wave", false), ("Whole run", true), _wholeRun, on => _wholeRun = on);
        }

        _damage = new CombatMeter { Title = "Damage", Log = Combat.Damage, AfterWave = Tabbed, HideWhenEmpty = !Tabbed };
        _healing = new CombatMeter
        {
            Title = "Healing", Log = Combat.Healing, AfterWave = Tabbed, HideWhenEmpty = !Tabbed, MaxRows = 6,
        };
        AddChild(_damage);
        AddChild(_healing);
        Apply();
    }

    // A row of two toggle buttons, the one matching current pressed; pressing one calls set(its value).
    private void AddTabs((string Label, bool Value) first, (string Label, bool Value) second, bool current,
        System.Action<bool> set)
    {
        var tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 6);
        var group = new ButtonGroup();
        foreach (var (label, value) in new[] { first, second })
        {
            var tab = new Button
            {
                Text = label,
                ToggleMode = true,
                ButtonGroup = group,
                ButtonPressed = value == current,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                FocusMode = FocusModeEnum.None,
            };
            tab.AddThemeFontSizeOverride("font_size", 13);
            tab.Toggled += on =>
            {
                if (!on)
                    return;
                set(value);
                Apply();
            };
            tabs.AddChild(tab);
        }
        AddChild(tabs);
    }

    private void Apply()
    {
        if (Tabbed)
        {
            _damage.Visible = !_showHealing;
            _healing.Visible = _showHealing;
        }
        _damage.WholeRun = _healing.WholeRun = _wholeRun;
        _damage.Refresh();
        _healing.Refresh();
    }
}
