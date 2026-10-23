using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Reads and writes <see cref="CareerSave"/> as JSON. Never throws: a missing file starts a fresh
/// career, and an unreadable or too-new file is moved aside (".corrupt-…"/".newer-…" next to it)
/// before starting fresh, so nothing is silently overwritten. Writes go to a temp file first and
/// then replace the save, so a crash mid-write can't leave a half-written file.
/// </summary>
public static class CareerStore
{
    public enum LoadStatus { Fresh, Loaded, Migrated, Corrupt, Newer }

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = null,
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
    };

    public static (CareerSave Save, LoadStatus Status) Load(string path)
    {
        string file = ProjectSettings.GlobalizePath(path);
        if (!File.Exists(file))
            return (new CareerSave(), LoadStatus.Fresh);

        CareerSave save;
        try
        {
            save = JsonSerializer.Deserialize<CareerSave>(File.ReadAllText(file), Json);
            if (save == null || save.Version < 1)
                throw new JsonException($"missing or invalid version");
        }
        catch (Exception e)
        {
            string backup = MoveAside(file, "corrupt");
            GD.PushWarning($"[career] save {file} unreadable ({e.Message}); moved to {backup}, starting fresh");
            return (new CareerSave(), LoadStatus.Corrupt);
        }

        if (save.Version > CareerSave.CurrentVersion)
        {
            string backup = MoveAside(file, "newer");
            GD.PushWarning($"[career] save {file} is version {save.Version}, this build reads up to " +
                           $"{CareerSave.CurrentVersion}; moved to {backup}, starting fresh");
            return (new CareerSave(), LoadStatus.Newer);
        }

        bool migrated = Migrate(save);
        save.Sanitize();
        return (save, migrated ? LoadStatus.Migrated : LoadStatus.Loaded);
    }

    /// <summary>Upgrades an older save in place, one version step at a time. Returns whether anything changed.</summary>
    private static bool Migrate(CareerSave save)
    {
        bool changed = false;
        // Add steps here when CurrentVersion goes up, e.g.:
        // if (save.Version == 1) { ...convert v1 fields...; save.Version = 2; changed = true; }
        if (save.Version != CareerSave.CurrentVersion)
        {
            save.Version = CareerSave.CurrentVersion;
            changed = true;
        }
        return changed;
    }

    /// <summary>Writes the save. Returns false (and logs) on failure; the old file stays intact.</summary>
    public static bool Write(string path, CareerSave save)
    {
        string file = ProjectSettings.GlobalizePath(path);
        string temp = file + ".tmp";
        try
        {
            save.Version = CareerSave.CurrentVersion;
            save.Updated = DateTime.UtcNow.ToString("o");
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(temp, JsonSerializer.Serialize(save, Json));
            File.Move(temp, file, overwrite: true);
            return true;
        }
        catch (Exception e)
        {
            GD.PushError($"[career] couldn't write save {file}: {e.Message}");
            return false;
        }
    }

    public static void Delete(string path)
    {
        string file = ProjectSettings.GlobalizePath(path);
        if (File.Exists(file))
            File.Delete(file);
    }

    private static string MoveAside(string file, string reason)
    {
        string backup = $"{file}.{reason}-{DateTime.Now:yyyyMMdd-HHmmss}";
        try
        {
            File.Move(file, backup, overwrite: true);
        }
        catch (Exception e)
        {
            GD.PushError($"[career] couldn't move {file} aside: {e.Message}");
        }
        return backup;
    }
}
