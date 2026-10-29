using System.Collections.Generic;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Amounts per source name for one run (damage dealt, healing done), plus a snapshot from the
/// start of the current wave so a wave's share is the difference. Feeds the in-game meters
/// (<see cref="CombatMeter"/>) and Autoplay's run-end logs. Plain C#; see <see cref="Combat.Damage"/>.
/// </summary>
public class MeterLog
{
    private readonly Dictionary<string, float> _totals = new();
    private readonly Dictionary<string, float> _atWaveStart = new();
    private readonly Dictionary<string, Color> _colors = new();

    /// <summary>Run totals per source.</summary>
    public IReadOnlyDictionary<string, float> Totals => _totals;

    public void Add(string source, float amount)
    {
        if (amount > 0f)
            _totals[source] = _totals.GetValueOrDefault(source) + amount;
    }

    /// <summary>Adds and gives the source its bar color in the same call (healing, where each call site knows its color).</summary>
    public void Add(string source, float amount, Color color)
    {
        Add(source, amount);
        _colors.TryAdd(source, color);
    }

    /// <summary>The source's amount since the last <see cref="MarkWaveStart"/>.</summary>
    public float ThisWave(string source) =>
        _totals.GetValueOrDefault(source) - _atWaveStart.GetValueOrDefault(source);

    public void SetColor(string source, Color color) => _colors[source] = color;

    public Color ColorOf(string source, Color fallback) => _colors.GetValueOrDefault(source, fallback);

    public void MarkWaveStart()
    {
        _atWaveStart.Clear();
        foreach (var (source, amount) in _totals)
            _atWaveStart[source] = amount;
    }

    public void Reset()
    {
        _totals.Clear();
        _atWaveStart.Clear();
        _colors.Clear();
    }
}
