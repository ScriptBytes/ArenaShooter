using System;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Autoload (project.godot) holding the player's settings: fullscreen, screen shake, damage
/// numbers and master volume. Like an options service: anything reads <see cref="Instance"/>,
/// screens change values through the setters, and every change is applied and written to
/// <c>user://settings.cfg</c> (a Godot <see cref="ConfigFile"/>, INI-like) straight away.
/// Also handles the fullscreen hotkey (F11 / Alt+Enter), so it works on every screen.
/// Never touches the career save.
/// </summary>
public partial class GameSettings : Node
{
    public const string DefaultPath = "user://settings.cfg";

    public static GameSettings Instance { get; private set; }

    /// <summary>Where settings are saved, or null to keep them in memory only (Autoplay without --settings).</summary>
    public string Path { get; private set; } = DefaultPath;

    public bool Fullscreen { get; private set; }

    /// <summary>Screen shake strength, 0 (off) to 1 (full). Scales <see cref="ShakeCamera"/>.</summary>
    public float ScreenShake { get; private set; } = 1f;

    /// <summary>Damage numbers over enemies. The player's own red numbers and heal/pickup texts always show.</summary>
    public bool DamageNumbers { get; private set; } = true;

    /// <summary>Master bus volume, 0 to 1. No audio yet, but the bus is set so it works once sound exists.</summary>
    public float MasterVolume { get; private set; } = 1f;

    public event Action Changed;

    public override void _EnterTree()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;

        // Autoplay runs keep the defaults (so seeded runs don't depend on the player's settings)
        // unless --settings=PATH points them at a scratch file. Read here because autoloads are
        // ready before Main parses its flags.
        var autoplay = AutoplayOptions.FromCommandLine();
        if (autoplay != null)
            Path = autoplay.SettingsPath;

        Load();
        Apply(window: autoplay == null || Path != null);
    }

    public void SetFullscreen(bool value)
    {
        Fullscreen = value;
        ApplyWindow();
        Save();
    }

    public void SetScreenShake(float value)
    {
        ScreenShake = Mathf.Clamp(value, 0f, 1f);
        Save();
    }

    public void SetDamageNumbers(bool value)
    {
        DamageNumbers = value;
        Save();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp(value, 0f, 1f);
        ApplyAudio();
        Save();
    }

    public override void _Input(InputEvent @event)
    {
        if (!@event.IsActionPressed("toggle_fullscreen"))
            return;
        SetFullscreen(!Fullscreen);
        GetViewport().SetInputAsHandled();
    }

    /// <summary>One line for logs: "fullscreen=false shake=1 damage_numbers=true volume=1".</summary>
    public string Summary() =>
        $"fullscreen={Fullscreen.ToString().ToLower()} shake={ScreenShake:0.##} damage_numbers={DamageNumbers.ToString().ToLower()} volume={MasterVolume:0.##}";

    private void Load()
    {
        if (Path == null)
        {
            GD.Print($"[settings] in memory (autoplay): {Summary()}");
            return;
        }

        var file = new ConfigFile();
        var error = file.Load(Path);
        if (error == Error.Ok)
        {
            Fullscreen = file.GetValue("display", "fullscreen", Fullscreen).AsBool();
            ScreenShake = Mathf.Clamp(file.GetValue("game", "screen_shake", ScreenShake).AsSingle(), 0f, 1f);
            DamageNumbers = file.GetValue("game", "damage_numbers", DamageNumbers).AsBool();
            MasterVolume = Mathf.Clamp(file.GetValue("audio", "master_volume", MasterVolume).AsSingle(), 0f, 1f);
        }
        // A missing file is a fresh start; an unreadable one is ignored and overwritten on the next change.
        string status = error == Error.Ok ? "loaded" : error == Error.FileNotFound ? "fresh" : $"unreadable ({error}), using defaults";
        GD.Print($"[settings] {ProjectSettings.GlobalizePath(Path)}: {status} {Summary()}");
    }

    private void Save()
    {
        Changed?.Invoke();
        if (Path == null)
            return;

        var file = new ConfigFile();
        file.SetValue("display", "fullscreen", Fullscreen);
        file.SetValue("game", "screen_shake", ScreenShake);
        file.SetValue("game", "damage_numbers", DamageNumbers);
        file.SetValue("audio", "master_volume", MasterVolume);
        var error = file.Save(Path);
        if (error != Error.Ok)
            GD.PushError($"[settings] couldn't write {Path}: {error}");
    }

    private void Apply(bool window)
    {
        if (window)
            ApplyWindow();
        ApplyAudio();
    }

    private void ApplyWindow()
    {
        // Headless (cloud, tests) has no window to change.
        if (DisplayServer.GetName() == "headless")
            return;
        var mode = Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed;
        if (DisplayServer.WindowGetMode() != mode)
            DisplayServer.WindowSetMode(mode);
    }

    private void ApplyAudio()
    {
        int master = AudioServer.GetBusIndex("Master");
        AudioServer.SetBusMute(master, MasterVolume <= 0f);
        AudioServer.SetBusVolumeDb(master, Mathf.LinearToDb(Mathf.Max(MasterVolume, 0.0001f)));
    }
}
