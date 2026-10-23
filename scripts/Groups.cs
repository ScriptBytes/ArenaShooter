namespace ArenaShooter;

/// <summary>
/// Scene-tree group names. Groups are Godot's built-in tagging system: any node can join
/// any number of groups, and GetTree().GetNodesInGroup() finds them without hard references.
/// </summary>
public static class Groups
{
    public const string Player = "player";
    public const string Enemies = "enemies";

    /// <summary>The player's allied units (Necromancer). Also Transient, so they're cleared at wave end.</summary>
    public const string Minions = "minions";

    /// <summary>Short-lived world objects (projectiles, coins) cleared when a wave ends.</summary>
    public const string Transient = "transient";
}
