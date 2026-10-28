using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

public enum ShopStrategy { Cheapest, First, Skip, Smart, Random }

public enum MoveMode { Circle, Still, Smart }

public enum TalentStrategy { Spread, None }

public enum HeirloomStrategy { Priciest, Keep }

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
/// <item><c>--waves=N</c> run length: the run is won after wave N (default: the game's, Main.TotalWaves).</item>
/// <item><c>--seed=N</c> fixes the random seed, so before/after comparisons see the same run.</item>
/// <item><c>--god</c> the player takes no damage.</item>
/// <item><c>--shop=cheapest|first|skip|smart|random</c> what to buy between waves (default cheapest, until broke).
/// smart values offers for the class and rerolls; random explores the item space (see <see cref="ShopBot"/>).</item>
/// <item><c>--move=circle|still|smart</c> circle the arena (collects coins), stand in the middle, or steer like a player:
/// dodge enemies and shots, keep the weapon's range, grab pickups (<see cref="SteeringBot"/>; default circle).</item>
/// <item><c>--bot-aggression=0..1</c> smart movement: how hard it engages vs. how much room it gives enemies (default 0.5).</item>
/// <item><c>--bot-danger=PIXELS</c> smart movement: distance beyond contact at which enemies count as danger (default 60).</item>
/// <item><c>--autocast</c> turns auto-cast on for every spell, including manual-by-default ones.</item>
/// <item><c>--item=NAME</c> grants an item at run start (display name, Id or .tres file name; Common unless --rarity). Repeatable.</item>
/// <item><c>--buy-potion=NAME</c> buys one of this potion at every shop visit, before other purchases. Repeatable.</item>
/// <item><c>--menu-delay=SECONDS</c> waits before each menu action, so Movie Maker can record the menus (default 0).</item>
/// <item><c>--mode=arcade|career</c> mode to pick on the mode select screen (default arcade).</item>
/// <item><c>--save=PATH</c> career save file (default user://career_autoplay.json, never the real save).</item>
/// <item><c>--reset-career</c> deletes that save file before the first run.</item>
/// <item><c>--runs=N</c> plays N runs in a row through the real end-of-run restart (default 1); run k uses seed+k.</item>
/// <item><c>--talents=spread|none</c> career: spend free points one at a time on the lowest-rank talent (default spread).</item>
/// <item><c>--heirloom=priciest|keep</c> career victory: keep the priciest worn gear (default) or the current heirloom.</item>
/// <item><c>--rarity=common|uncommon|rare|epic</c> every shop offer and every --item grant gets this rarity instead of a roll.</item>
/// <item><c>--stat=STAT:VALUE</c> adds a stat bonus at run start, e.g. <c>--stat=CritChance:20</c>. Repeatable.</item>
/// <item><c>--retry</c> with <c>--runs=N</c>: later runs start through the end screen's "Retry same class" button instead of R.</item>
/// <item><c>--tour</c> early in the first wave, opens and closes the gear screen (Tab), then the pause menu (Esc) and its
/// Settings, flips Damage numbers there, and resumes. Logs a <c>tour</c> event per step. Combine with --menu-delay to record it.</item>
/// <item><c>--settings=PATH</c> settings file to load and save (default: none, so Autoplay uses default settings in memory and never writes user://settings.cfg).</item>
/// </list>
/// </summary>
public sealed class AutoplayOptions
{
    public string ClassName { get; private set; }
    public List<string> Spells { get; } = new();
    public int StartWave { get; private set; } = 1;
    public int? Waves { get; private set; }
    public ulong? Seed { get; private set; }
    public bool God { get; private set; }
    public ShopStrategy Shop { get; private set; } = ShopStrategy.Cheapest;
    public MoveMode Move { get; private set; } = MoveMode.Circle;
    public float BotAggression { get; private set; } = 0.5f;
    public float BotDanger { get; private set; } = 60f;
    public bool AutoCastAll { get; private set; }
    public float MenuDelay { get; private set; }
    public List<string> Items { get; } = new();
    public List<string> BuyPotions { get; } = new();
    public List<(StatType Stat, float Value)> Stats { get; } = new();
    public GameMode Mode { get; private set; } = GameMode.Arcade;
    public string SavePath { get; private set; } = "user://career_autoplay.json";
    public bool ResetCareer { get; private set; }
    public int Runs { get; private set; } = 1;
    public TalentStrategy Talents { get; private set; } = TalentStrategy.Spread;
    public HeirloomStrategy Heirloom { get; private set; } = HeirloomStrategy.Priciest;
    public Rarity? Rarity { get; private set; }
    public bool Retry { get; private set; }
    public bool Tour { get; private set; }
    public string SettingsPath { get; private set; }

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
                case "--waves" when int.TryParse(value, out int waves) && waves >= 1:
                    options.Waves = waves;
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
                case "--bot-aggression" when float.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out float aggression) && aggression is >= 0f and <= 1f:
                    options.BotAggression = aggression;
                    break;
                case "--bot-danger" when float.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out float danger) && danger >= 0f:
                    options.BotDanger = danger;
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
                case "--buy-potion" when value != "":
                    options.BuyPotions.Add(value);
                    break;
                case "--mode" when Enum.TryParse(value, ignoreCase: true, out GameMode mode) && Enum.IsDefined(mode) && mode != GameMode.None:
                    options.Mode = mode;
                    break;
                case "--save" when value != "":
                    options.SavePath = value;
                    break;
                case "--reset-career":
                    options.ResetCareer = true;
                    break;
                case "--runs" when int.TryParse(value, out int runs) && runs >= 1:
                    options.Runs = runs;
                    break;
                case "--talents" when Enum.TryParse(value, ignoreCase: true, out TalentStrategy talents):
                    options.Talents = talents;
                    break;
                case "--heirloom" when Enum.TryParse(value, ignoreCase: true, out HeirloomStrategy heirloom):
                    options.Heirloom = heirloom;
                    break;
                case "--rarity" when Enum.TryParse(value, ignoreCase: true, out Rarity rarity) && Enum.IsDefined(rarity):
                    options.Rarity = rarity;
                    break;
                case "--retry":
                    options.Retry = true;
                    break;
                case "--tour":
                    options.Tour = true;
                    break;
                case "--settings" when value != "":
                    options.SettingsPath = value;
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
