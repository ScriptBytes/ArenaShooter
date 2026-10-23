using System.Collections.Generic;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Temporary stat bonuses (War Cry, Shield Wall, later potions). They sit on top of the
/// run's PlayerStats without changing them: Player adds them into a copy when applying stats.
/// Recasting the same source refreshes its timer instead of stacking.
/// </summary>
public class ActiveBuffs
{
    public class Buff
    {
        public Resource Source;
        public IReadOnlyList<StatModifier> Modifiers;
        public Color Color;
        public float Duration;
        public float TimeLeft;
    }

    private readonly List<Buff> _buffs = new();

    public IReadOnlyList<Buff> All => _buffs;

    public void Add(Resource source, IReadOnlyList<StatModifier> modifiers, float duration, Color color)
    {
        var buff = _buffs.Find(b => b.Source == source);
        if (buff == null)
        {
            buff = new Buff { Source = source, Modifiers = modifiers, Color = color };
            _buffs.Add(buff);
        }

        buff.Duration = duration;
        buff.TimeLeft = duration;
    }

    /// <summary>Counts timers down. Returns true when a buff expired, so stats need re-applying.</summary>
    public bool Tick(float dt)
    {
        foreach (var buff in _buffs)
            buff.TimeLeft -= dt;
        return _buffs.RemoveAll(b => b.TimeLeft <= 0f) > 0;
    }

    public void Clear() => _buffs.Clear();

    public void AddTo(PlayerStats stats)
    {
        foreach (var buff in _buffs)
        {
            foreach (var modifier in buff.Modifiers)
                stats.Add(modifier.Stat, modifier.Value);
        }
    }
}
