using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

public enum GameMode { None, Arcade, Career }

/// <summary>
/// Autoload (project.godot) holding what outlives a scene reload: the mode picked this session,
/// a pending "retry same class" request, and the career save. Like a singleton service: Main and the career screens reach it through
/// <see cref="Instance"/>. The save loads lazily on first use, so arcade play never reads or
/// writes it, and every change is written straight away (<see cref="Persist"/>).
/// </summary>
public partial class Career : Node
{
    public const string DefaultSavePath = "user://career.json";

    public static Career Instance { get; private set; }

    /// <summary>The mode picked on the mode select screen. Survives restarts; None until picked.</summary>
    public GameMode Mode { get; set; } = GameMode.None;

    /// <summary>"Retry same class": the class and spell picks (by save Id) to start again with.</summary>
    public sealed record RetryRequest(string ClassId, IReadOnlyList<string> SpellIds);

    /// <summary>
    /// Set just before a restart's scene reload; Main takes it on the next load and starts that run
    /// straight away, skipping the select screens. Null otherwise.
    /// </summary>
    public RetryRequest PendingRetry { get; set; }

    public string SavePath { get; private set; } = DefaultSavePath;
    public CareerStore.LoadStatus LoadStatus { get; private set; }

    private CareerSave _save;
    private bool _configured;

    public CareerSave Save
    {
        get
        {
            if (_save == null)
            {
                (_save, LoadStatus) = CareerStore.Load(SavePath);
                GD.Print($"[career] save {ProjectSettings.GlobalizePath(SavePath)}: {LoadStatus.ToString().ToLower()}");
            }
            return _save;
        }
    }

    public override void _EnterTree() => Instance = this;

    /// <summary>
    /// Points the career at another save file (Autoplay's --save), optionally deleting it first.
    /// Only the first call per app launch counts, so a scene reload doesn't reset the career again.
    /// </summary>
    public void Configure(string savePath, bool reset)
    {
        if (_configured)
            return;
        _configured = true;
        SavePath = savePath;
        _save = null;
        if (reset)
            CareerStore.Delete(savePath);
    }

    /// <summary>The career character for a class, created on first use.</summary>
    public CareerCharacter For(ClassData playerClass)
    {
        string id = ClassId(playerClass);
        if (!Save.Characters.TryGetValue(id, out var character))
            Save.Characters[id] = character = new CareerCharacter();
        return character;
    }

    /// <summary>The class's character without adding it to the save (a blank one if it has none yet).</summary>
    public CareerCharacter Peek(ClassData playerClass) =>
        Save.Characters.GetValueOrDefault(ClassId(playerClass)) ?? new CareerCharacter();

    public void Persist() => CareerStore.Write(SavePath, Save);

    /// <summary>Erases the class's character from the save (the other classes keep theirs). <see cref="For"/> then starts a new one.</summary>
    public void ResetCharacter(ClassData playerClass)
    {
        Save.Characters.Remove(ClassId(playerClass));
        Persist();
        GD.Print($"[career] reset character {ClassId(playerClass)}");
    }

    /// <summary>The class's save key (<see cref="ClassData.Id"/>; the file name if it's missing, which <see cref="ValidateIds"/> reports).</summary>
    public static string ClassId(ClassData playerClass) => IdOrFileName(playerClass.Id, playerClass);

    /// <summary>The item's save key (<see cref="ItemData.Id"/>; the file name if it's missing).</summary>
    public static string ItemId(ItemData item) => IdOrFileName(item.Id, item);

    /// <summary>The spell's Id (the file name if it's missing).</summary>
    public static string SpellId(AbilityData spell) => IdOrFileName(spell.Id, spell);

    private static string IdOrFileName(string id, Resource resource) =>
        string.IsNullOrWhiteSpace(id) ? resource.ResourcePath.GetFile().GetBaseName() : id;

    /// <summary>
    /// Logs an error for every empty or duplicate save id, so new content can't silently collide
    /// with (or orphan) saved data. Called once at startup with everything a save can reference.
    /// Returns the number of problems found.
    /// </summary>
    public static int ValidateIds(IEnumerable<ClassData> classes, IEnumerable<ItemData> items, IEnumerable<CareerTalent> talents,
        IEnumerable<AbilityData> spells)
    {
        int problems = 0;
        void Check<T>(string kind, IEnumerable<T> things, System.Func<T, string> id, System.Func<T, string> path) where T : Resource
        {
            var seen = new Dictionary<string, string>();
            foreach (var thing in things.Where(t => t != null).Distinct())
            {
                string key = id(thing);
                if (string.IsNullOrWhiteSpace(key))
                {
                    GD.PushError($"[career] {kind} {path(thing)} has no Id");
                    problems++;
                }
                else if (seen.TryGetValue(key, out string other))
                {
                    GD.PushError($"[career] {kind} Id '{key}' is used by both {other} and {path(thing)}");
                    problems++;
                }
                else
                    seen[key] = path(thing);
            }
        }
        Check("class", classes, c => c.Id, c => c.ResourcePath);
        Check("item", items, i => i.Id, i => i.ResourcePath);
        Check("talent", talents, t => t.Id, t => $"'{t.DisplayName}'");
        Check("spell", spells, s => s.Id, s => s.ResourcePath);
        return problems;
    }
}
