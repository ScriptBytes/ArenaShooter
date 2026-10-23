using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

public enum ShopStrategy { Cheapest, First, Skip }

public enum MoveMode { Circle, Still }

/// <summary>
/// Command-line flags for <see cref="Autoplay"/>. Pass them after "--" so Godot hands them to the game:
/// <code>
/// godot --headless --path . -- --autoplay --class=mage --wave=3 --seed=1 --item="Rabbit's Foot"
/// </code>
/// <list type="bullet">
/// <item><c>--autoplay</c> turns the mode on; without it every other flag is ignored.</item>
/// <item><c>--class=NAME|INDEX</c> class to pick on the select screen (default: the first).</item>
/// <item><c>--spells=A,B,C</c> spells to pick, in slot order (display or .tres file names; default: the list's first N).</item>
/// <item><c>--wave=N</c> wave to start on (default 1).</item>
/// <item><c>--seed=N</c> fixes the random seed, so before/after comparisons see the same run.</item>
/// <item><c>--god</c> the player takes no damage.</item>
/// <item><c>--shop=cheapest|first|skip</c> what to buy between waves (default cheapest, until broke).</item>
/// <item><c>--move=circle|still</c> circle the arena (collects coins) or stand in the middle (default circle).</item>
/// <item><c>--autocast</c> turns auto-cast on for every spell, including manual-by-default ones.</item>
/// <item><c>--item=NAME</c> grants an item at run start (display name or .tres file name). Repeatable.</item>
/// <item><c>--menu-delay=SECONDS</c> waits before each menu action, so Movie Maker can record the menus (default 0).</item>
/// <item><c>--stat=STAT:VALUE</c> adds a stat bonus at run start, e.g. <c>--stat=CritChance:20</c>. Repeatable.</item>
/// </list>
/// </summary>
public sealed class AutoplayOptions
{
    public string ClassName { get; private set; }
    public List<string> Spells { get; } = new();
    public int StartWave { get; private set; } = 1;
    public ulong? Seed { get; private set; }
    public bool God { get; private set; }
    public ShopStrategy Shop { get; private set; } = ShopStrategy.Cheapest;
    public MoveMode Move { get; private set; } = MoveMode.Circle;
    public bool AutoCastAll { get; private set; }
    public float MenuDelay { get; private set; }
    public List<string> Items { get; } = new();
    public List<(StatType Stat, float Value)> Stats { get; } = new();

    /// <summary>Problems with the flags. Autoplay logs them and quits with exit code 1.</summary>
    public List<string> Errors { get; } = new();

    /// <summary>Returns null unless the game was launched with <c>--autoplay</c>.</summary>
    public static AutoplayOptions FromCommandLine() => Parse(OS.GetCmdlineUserArgs());

    public static AutoplayOptions Parse(IReadOnlyCollection<string> args)
    {
        if (!args.Contains("--autoplay"))
            return null;

        var options = new AutoplayOptions();
        foreach (var arg in args)
        {
            int eq = arg.IndexOf('=');
            string key = eq < 0 ? arg : arg[..eq];
            string value = eq < 0 ? "" : arg[(eq + 1)..];

            switch (key)
            {
                case "--autoplay":
                    break;
                case "--class":
                    options.ClassName = value;
                    break;
                case "--spells" when value != "":
                    options.Spells.AddRange(value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                    break;
                case "--wave" when int.TryParse(value, out int wave) && wave >= 1:
                    options.StartWave = wave;
                    break;
                case "--seed" when ulong.TryParse(value, out ulong seed):
                    options.Seed = seed;
                    break;
                case "--god":
                    options.God = true;
                    break;
                case "--shop" when Enum.TryParse(value, ignoreCase: true, out ShopStrategy shop):
                    options.Shop = shop;
                    break;
                case "--move" when Enum.TryParse(value, ignoreCase: true, out MoveMode move):
                    options.Move = move;
                    break;
                case "--autocast":
                    options.AutoCastAll = true;
                    break;
                case "--menu-delay" when float.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out float delay) && delay >= 0f:
                    options.MenuDelay = delay;
                    break;
                case "--item" when value != "":
                    options.Items.Add(value);
                    break;
                case "--stat" when TryParseStat(value, out var stat):
                    options.Stats.Add(stat);
                    break;
                default:
                    options.Errors.Add($"bad or unknown flag '{arg}'");
                    break;
            }
        }

        return options;
    }

    private static bool TryParseStat(string value, out (StatType, float) stat)
    {
        stat = default;
        var parts = value.Split(':');
        if (parts.Length != 2
            || !Enum.TryParse(parts[0], ignoreCase: true, out StatType type)
            || !float.TryParse(parts[1], System.Globalization.CultureInfo.InvariantCulture, out float amount))
            return false;

        stat = (type, amount);
        return true;
    }
}
