using System;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Autoplay's <c>--move=smart</c>: a context-steering bot. Every frame it scores 16 directions on
/// two maps, interest (coins, hearts, the engagement ring around the nearest enemy) and danger
/// (enemies about to touch, incoming enemy shots, spawn markers, walls), then moves along the most
/// interesting direction that isn't too dangerous. The engagement distance comes from the equipped
/// weapon, so melee classes close in and ranged classes kite without per-class code.
/// Plain C#, deterministic for a given seed. Keep the logic frozen while comparing results: a
/// smarter bot changes every number it produces.
/// </summary>
public sealed class SteeringBot
{
    private const int Slots = 16;

    // How far ahead to look for enemy shots, and how close counts as a hit.
    private const float ShotLookahead = 0.6f;
    private const float ShotMargin = 18f;

    // Walls: probe this far along each direction; danger starts this far from the edge.
    private const float WallProbe = 90f;
    private const float WallMargin = 70f;

    private const float SpawnMarkerRadius = 70f;
    private const float PickupInterestRange = 350f;

    // Engagement: how far from the preferred distance still counts as "there" (then it strafes).
    private const float RingTolerance = 25f;

    // Below this HP fraction the bot plays safe: kites at ranged distance even with a melee weapon.
    private const float RetreatHealth = 0.35f;

    // Player-only enemy shots have the player layer (2) in their mask; the player's own don't.
    private const uint PlayerLayer = 2;
    private const float PlayerHurtRadius = 14f;

    private static readonly Vector2[] Directions = BuildDirections();

    private readonly Player _player;
    private readonly Rect2 _bounds;
    private readonly EnemySpawner _spawner;
    private readonly float _aggression;
    private readonly float _dangerRadius;

    private readonly float[] _interest = new float[Slots];
    private readonly float[] _danger = new float[Slots];
    private Vector2 _last;
    private float _strafeSign = 1f;

    /// <param name="aggression">0–1: how hard it pushes toward enemies vs. how much danger it tolerates.</param>
    /// <param name="dangerRadius">Extra distance beyond contact at which enemies start to count as danger.</param>
    public SteeringBot(Player player, Rect2 bounds, EnemySpawner spawner, float aggression, float dangerRadius)
    {
        _player = player;
        _bounds = bounds;
        _spawner = spawner;
        _aggression = aggression;
        _dangerRadius = dangerRadius;
    }

    /// <summary>A short description of the engagement profile, for run_start.</summary>
    public string Profile()
    {
        var (melee, reach) = Weapon();
        return $"{(melee ? "melee" : "ranged")}:{reach:0}";
    }

    /// <summary>The direction to move this frame (unit length, or zero to stand still).</summary>
    public Vector2 Steer()
    {
        Array.Clear(_interest);
        Array.Clear(_danger);
        var pos = _player.GlobalPosition;
        var tree = _player.GetTree();

        bool hurt = _player.Health.Current < _player.Health.Max * RetreatHealth;
        var (melee, reach) = Weapon();
        float preferred = melee && !hurt ? reach * 0.7f : Mathf.Max(reach * 0.8f, hurt ? 260f : 0f);

        Enemy nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var node in tree.GetNodesInGroup(Groups.Enemies))
        {
            if (node is not Enemy enemy || enemy.Health.IsDead)
                continue;

            var offset = enemy.GlobalPosition - pos;
            float dist = offset.Length();
            if (dist < nearestDist)
            {
                nearest = enemy;
                nearestDist = dist;
            }

            // Danger grows from (contact + danger radius) down to contact; less aggressive bots keep a
            // wider berth. A healthy melee bot only fears what's inside its own preferred distance,
            // or it could never get close enough to swing.
            float contact = enemy.Data.Size / 2f + PlayerHurtRadius;
            float extra = _dangerRadius * (1.5f - _aggression);
            if (melee && !hurt)
                extra = Mathf.Min(extra, Mathf.Max(preferred - contact, 8f));
            float radius = contact + extra;
            if (dist < radius)
                AddDanger(offset, 1f - Mathf.Max(dist - contact, 0f) / (radius - contact));
        }

        AddShotDanger(tree, pos);
        AddSpawnDanger(pos);
        AddWallDanger(pos);

        if (nearest != null)
            AddEngagement(nearest.GlobalPosition - pos, nearestDist, preferred);
        AddPickupInterest(tree, pos, hurt);

        // A gentle pull to the middle keeps it out of corners when nothing else matters.
        var toCenter = _bounds.GetCenter() - pos;
        if (toCenter.Length() > 150f)
            AddInterest(toCenter, 0.15f);

        return Choose();
    }

    private (bool Melee, float Reach) Weapon()
    {
        var weapon = _player.Abilities.Weapon?.Ability;
        float bonus = _player.Abilities.Context.RangeBonus;
        if (weapon == null)
            return (false, 300f);
        // Melee reach gets half the Range bonus (MeleeArcAbility.RangeBonusScale).
        return weapon is MeleeArcAbility ? (true, weapon.Range + bonus * 0.5f) : (false, weapon.Range + bonus);
    }

    // Close the gap, back off, or circle at the preferred distance.
    private void AddEngagement(Vector2 toEnemy, float dist, float preferred)
    {
        float weight = 0.4f + 0.6f * _aggression;
        if (dist > preferred + RingTolerance)
            AddInterest(toEnemy, weight);
        else if (dist < preferred - RingTolerance)
            AddInterest(-toEnemy, weight);
        else
        {
            var strafe = toEnemy.Orthogonal() * _strafeSign;
            // Flip sides when the strafe direction is the dangerous one.
            if (_danger[Slot(strafe)] > 0.5f)
                _strafeSign = -_strafeSign;
            AddInterest(toEnemy.Orthogonal() * _strafeSign, weight * 0.6f);
        }
    }

    private void AddPickupInterest(SceneTree tree, Vector2 pos, bool hurt)
    {
        foreach (var node in tree.GetNodesInGroup(Groups.Transient))
        {
            float weight = node switch
            {
                Heart when _player.WantsHeart => hurt ? 1.2f : 0.6f,
                Coin => 0.35f,
                _ => 0f,
            };
            if (weight == 0f)
                continue;

            var offset = ((Node2D)node).GlobalPosition - pos;
            float dist = offset.Length();
            if (dist < PickupInterestRange && dist > 1f)
                AddInterest(offset, weight * (1f - dist / PickupInterestRange));
        }
    }

    // Enemy shots: where each passes closest to the player within the lookahead. Danger toward that
    // point, scaled by how soon it gets there; a head-on shot pushes sideways.
    private void AddShotDanger(SceneTree tree, Vector2 pos)
    {
        foreach (var node in tree.GetNodesInGroup(Groups.Transient))
        {
            if (node is not Projectile shot || (shot.CollisionMask & PlayerLayer) == 0)
                continue;

            var p = shot.GlobalPosition - pos;
            var v = shot.Direction * shot.Speed;
            float t = Mathf.Clamp(-p.Dot(v) / Mathf.Max(v.LengthSquared(), 1f), 0f, ShotLookahead);
            if (t <= 0f && p.Length() > ShotMargin * 2f)
                continue; // already moving away

            var closest = p + v * t;
            float miss = closest.Length();
            float hitDist = shot.Radius + PlayerHurtRadius + ShotMargin;
            if (miss >= hitDist)
                continue;

            float urgency = (1f - t / ShotLookahead) * (1f - miss / hitDist) + 0.3f;
            var away = miss > 2f ? -closest : v.Orthogonal() * _strafeSign;
            AddDanger(-away, Mathf.Min(urgency, 1f));
            AddInterest(away, urgency * 0.5f);
        }
    }

    private void AddSpawnDanger(Vector2 pos)
    {
        if (_spawner == null)
            return;
        foreach (var child in _spawner.GetChildren())
        {
            if (child is not SpawnMarker marker)
                continue;
            var offset = marker.GlobalPosition - pos;
            float dist = offset.Length();
            if (dist < SpawnMarkerRadius)
                AddDanger(offset, 0.8f * (1f - dist / SpawnMarkerRadius));
        }
    }

    // Probe ahead along each direction; the closer the probe gets to an edge, the more danger.
    private void AddWallDanger(Vector2 pos)
    {
        for (int i = 0; i < Slots; i++)
        {
            var probe = pos + Directions[i] * WallProbe;
            float edge = Mathf.Min(
                Mathf.Min(probe.X - _bounds.Position.X, _bounds.End.X - probe.X),
                Mathf.Min(probe.Y - _bounds.Position.Y, _bounds.End.Y - probe.Y));
            if (edge < WallMargin)
                _danger[i] = Mathf.Max(_danger[i], Mathf.Clamp(1f - edge / WallMargin, 0f, 1f));
        }
    }

    // Spread a threat over neighboring slots by how much each direction heads toward it.
    private void AddDanger(Vector2 toward, float weight)
    {
        var dir = toward.Normalized();
        for (int i = 0; i < Slots; i++)
        {
            float d = Directions[i].Dot(dir);
            if (d > 0f)
                _danger[i] = Mathf.Max(_danger[i], weight * (0.35f + 0.65f * d));
        }
    }

    private void AddInterest(Vector2 toward, float weight)
    {
        var dir = toward.Normalized();
        for (int i = 0; i < Slots; i++)
        {
            float d = Directions[i].Dot(dir);
            if (d > 0f)
                _interest[i] += weight * d;
        }
    }

    // Mask out directions noticeably more dangerous than the safest one, then take the most
    // interesting of the rest, with a small bonus for keeping the current heading (less jitter).
    private Vector2 Choose()
    {
        float minDanger = float.MaxValue;
        float maxInterest = 0f;
        for (int i = 0; i < Slots; i++)
        {
            minDanger = Mathf.Min(minDanger, _danger[i]);
            maxInterest = Mathf.Max(maxInterest, _interest[i]);
        }

        int best = -1;
        float bestScore = float.MinValue;
        for (int i = 0; i < Slots; i++)
        {
            if (_danger[i] > minDanger + 0.15f)
                continue;
            float score = _interest[i] - _danger[i] + 0.1f * Directions[i].Dot(_last);
            if (score > bestScore)
            {
                bestScore = score;
                best = i;
            }
        }

        // Nothing wanted and nothing threatening: stand still.
        _last = best < 0 || (maxInterest < 0.01f && minDanger < 0.01f) ? Vector2.Zero : Directions[best];
        return _last;
    }

    private static int Slot(Vector2 direction)
    {
        float angle = Mathf.PosMod(direction.Angle(), Mathf.Tau);
        return (int)Mathf.Round(angle / Mathf.Tau * Slots) % Slots;
    }

    private static Vector2[] BuildDirections()
    {
        var dirs = new Vector2[Slots];
        for (int i = 0; i < Slots; i++)
            dirs[i] = Vector2.FromAngle(Mathf.Tau * i / Slots);
        return dirs;
    }
}
