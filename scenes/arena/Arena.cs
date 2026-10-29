using Godot;

namespace ArenaShooter;

/// <summary>
/// Rectangular play area from (0,0) to <see cref="Size"/>. Draws the floor and builds
/// invisible walls just outside the edges, so Size is the single source of truth.
/// </summary>
public partial class Arena : Node2D
{
    private const float WallThickness = 64f;
    private const float GridSpacing = 100f;

    [Export] public Vector2 Size { get; set; } = new(1600, 1000);

    public Rect2 Bounds => new(Vector2.Zero, Size);

    public override void _Ready()
    {
        AddToGroup(Groups.Arena);
        ZIndex = -10; // floor draws beneath everything, including coins (ZIndex -1)

        var walls = new StaticBody2D { Name = "Walls", CollisionLayer = 1, CollisionMask = 0 };
        AddChild(walls);

        float t = WallThickness;
        AddWall(walls, new Vector2(Size.X / 2f, -t / 2f), new Vector2(Size.X + 2 * t, t));
        AddWall(walls, new Vector2(Size.X / 2f, Size.Y + t / 2f), new Vector2(Size.X + 2 * t, t));
        AddWall(walls, new Vector2(-t / 2f, Size.Y / 2f), new Vector2(t, Size.Y + 2 * t));
        AddWall(walls, new Vector2(Size.X + t / 2f, Size.Y / 2f), new Vector2(t, Size.Y + 2 * t));
    }

    public Vector2 RandomPoint(float margin)
    {
        return new Vector2(
            (float)GD.RandRange(margin, Size.X - margin),
            (float)GD.RandRange(margin, Size.Y - margin));
    }

    private static void AddWall(StaticBody2D walls, Vector2 center, Vector2 size)
    {
        walls.AddChild(new CollisionShape2D
        {
            Position = center,
            Shape = new RectangleShape2D { Size = size },
        });
    }

    public override void _Draw()
    {
        DrawRect(Bounds, new Color("1c1f26"));

        var gridColor = new Color("262a33");
        for (float x = GridSpacing; x < Size.X; x += GridSpacing)
            DrawLine(new Vector2(x, 0), new Vector2(x, Size.Y), gridColor);
        for (float y = GridSpacing; y < Size.Y; y += GridSpacing)
            DrawLine(new Vector2(0, y), new Vector2(Size.X, y), gridColor);

        DrawRect(Bounds, new Color("5c6370"), filled: false, width: 4f);
    }
}
