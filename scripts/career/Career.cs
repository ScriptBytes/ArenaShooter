using System.Collections.Generic;
using Godot;

namespace ArenaShooter;

public enum GameMode { None, Arcade, Career }

/// <summary>
/// Autoload (project.godot) holding what outlives a scene reload: the mode picked this session
/// and the career save. Like a singleton service: Main and the career screens reach it through
/// <see cref="Instance"/>. The save loads lazily on first use, so arcade play never reads or
/// writes it, and every change is written straight away (<see cref="Persist"/>).
/// </summary>
public partial class Career : Node
{
    public const string DefaultSavePath = "user://career.json";

    public static Career Instance { get; private set; }

    /// <summary>The mode picked on the mode select screen. Survives restarts; None until picked.</summary>
    public GameMode Mode { get; set; } = GameMode.None;

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

    public static string ClassId(ClassData playerClass) => playerClass.ResourcePath.GetFile().GetBaseName();

    public static string ItemId(ItemData item) => item.ResourcePath.GetFile().GetBaseName();
}
