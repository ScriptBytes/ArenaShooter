using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Read-only character sheet opened with Tab during a wave: worn equipment and trinkets (the
/// shop's listing, with the same hover tooltips), potions, active buffs and the full stat sheet.
/// Pauses the game while open; Tab or Esc closes it. Built in code.
/// </summary>
public partial class GearScreen : CanvasLayer
{
    /// <summary>Set by Main: whether Tab may open the screen right now (only during a wave).</summary>
    public Func<bool> CanOpen { get; set; } = () => false;

    private RunState _run;
    private Player _player;

    private Label _title;
    private VBoxContainer _equipment;
    private Label _character;
    private Label _stats;
    private Label _potions;
    private bool _wasPaused;

    public override void _Ready()
    {
        // Mostly see-through, so the paused arena stays visible behind the sheet.
        AddChild(new ColorRect { Color = new Color(0.03f, 0.03f, 0.05f, 0.88f), AnchorRight = 1f, AnchorBottom = 1f });
        var center = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
        AddChild(center);
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 16);
        center.AddChild(layout);

        _title = UiStyles.Title("", 34);
        layout.AddChild(_title);

        var body = new HBoxContainer();
        body.AddThemeConstantOverride("separation", 20);
        layout.AddChild(body);

        _equipment = new VBoxContainer();
        _equipment.AddThemeConstantOverride("separation", 0);
        var equipmentPanel = Panel(body, 280);
        equipmentPanel.Theme = UiStyles.TooltipTheme();
        equipmentPanel.AddChild(_equipment);

        var middle = new VBoxContainer();
        middle.AddThemeConstantOverride("separation", 12);
        _character = Text(Panel(middle, 280));
        _potions = Text(Panel(middle, 280));
        body.AddChild(middle);

        _stats = Text(Panel(body, 300));

        var hint = UiStyles.DimLabel("Hover an item for its stats  ·  Tab or Esc to close");
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        layout.AddChild(hint);
        Hide();
    }

    /// <summary>What the screen shows. Set by Main once the run starts.</summary>
    public void Bind(RunState run, Player player)
    {
        _run = run;
        _player = player;
    }

    public void Open()
    {
        if (_run == null || Visible)
            return;
        Refresh();
        _wasPaused = GetTree().Paused;
        GetTree().Paused = true;
        Show();
    }

    public void Close()
    {
        if (!Visible)
            return;
        Hide();
        GetTree().Paused = _wasPaused;
    }

    public override void _Input(InputEvent @event)
    {
        bool tab = @event.IsActionPressed("gear_screen");
        bool esc = @event.IsActionPressed("pause");
        if (Visible && (tab || esc))
        {
            GetViewport().SetInputAsHandled();
            Close();
        }
        else if (!Visible && tab && CanOpen())
        {
            GetViewport().SetInputAsHandled();
            Open();
        }
    }

    private void Refresh()
    {
        _title.Text = $"Wave {_run.Wave}  ·  Paused";
        GearViews.FillEquipment(_equipment, _run, 17);

        var health = _player.Health;
        var weapon = _player.Abilities.Weapon;
        var lines = new List<string>
        {
            "Character\n",
            $"HP {Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Max)}",
            $"Armor {_player.Armor:0.#}",
            $"Move speed {_player.Speed:0}",
            $"Pickup range {_player.PickupRange:0}",
            $"Dodge chance {_player.DodgeChance * 100f:0.#}%",
        };
        if (weapon != null)
            lines.Add($"Weapon: one attack every {weapon.EffectiveCooldown:0.##}s");
        if (_player.Shield > 0f)
            lines.Add($"Barrier {_player.Shield:0.#}");
        _character.Text = string.Join("\n", lines);

        var potions = _run.Potions;
        var potionLines = new List<string>
        {
            "Potions\n",
            $"Health potions: {potions.HealthPotions} (Space, +{_player.PotionHeal:0.#} HP)",
            $"Reserve: {potions.Reserve:0.#} / {potions.ReserveCapacity:0}",
        };
        for (int i = 0; i < potions.Hotbar.Count; i++)
            potionLines.Add($"{potions.Hotbar[i].DisplayName}: {potions.Count(potions.Hotbar[i])} (key {AbilityCaster.SpellSlotCount + i + 1})");
        var buffs = _player.Buffs.All;
        if (buffs.Count > 0)
        {
            potionLines.Add("\nActive buffs");
            potionLines.AddRange(buffs.Select(b => $"{BuffName(b.Source)} ({b.TimeLeft:0.#}s): " +
                string.Join(", ", b.Modifiers.Select(m => m.Describe()))));
        }
        _potions.Text = string.Join("\n", potionLines);

        _stats.Text = "Bonuses (gear, trinkets, talents)\n\n" + string.Join("\n", GearViews.StatLines(_run.Stats, all: true));
    }

    private static string BuffName(Resource source) => source switch
    {
        AbilityData ability => ability.DisplayName,
        ItemData item => item.DisplayName,
        _ => "Buff",
    };

    private static PanelContainer Panel(Container parent, float width)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(width, 0) };
        panel.AddThemeStyleboxOverride("panel", UiStyles.Panel());
        parent.AddChild(panel);
        return panel;
    }

    private static Label Text(PanelContainer panel)
    {
        var label = new Label { SizeFlagsVertical = Control.SizeFlags.ShrinkBegin };
        label.AddThemeFontSizeOverride("font_size", 16);
        panel.AddChild(label);
        return label;
    }
}
