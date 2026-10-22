namespace ArenaShooter;

/// <summary>
/// Scene-tree group names. Groups are Godot's built-in tagging system: any node can join
/// any number of groups, and GetTree().GetNodesInGroup() finds them without hard references.
/// </summary>
public static class Groups
{
    public const string Player = "player";
    public const string Enemies = "enemies";
}
