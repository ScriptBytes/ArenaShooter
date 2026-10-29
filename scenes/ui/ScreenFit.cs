using Godot;

namespace ArenaShooter;

/// <summary>
/// Keeps a centered menu layout on screen. A CenterContainer pushes content that's bigger than the
/// window off both edges; this shrinks the layout around its center until it fits instead.
/// Add it as a child of the layout (<see cref="Attach"/>); containers ignore non-Control children.
/// </summary>
public partial class ScreenFit : Node
{
    /// <summary>Space kept free around the layout, in pixels (both sides together).</summary>
    private static readonly Vector2 Margin = new(32f, 24f);

    private Control _target;
    private Vector2 _lastNeeded;

    public static void Attach(Control layout) => layout.AddChild(new ScreenFit());

    public override void _Ready()
    {
        _target = GetParent<Control>();
        // Menus are shown while the tree is paused.
        ProcessMode = ProcessModeEnum.Always;
    }

    // Checked every frame while shown: a wrapped label reports a huge minimum size until the layout
    // has given it a width (a frame or two after the content changes), and no Resized signal
    // reliably follows once it settles.
    public override void _Process(double delta)
    {
        if (!_target.IsVisibleInTree())
            return;

        var needed = _target.GetCombinedMinimumSize();
        var available = _target.GetViewportRect().Size - Margin;
        // Unsettled labels measure several screens tall (real menus barely pass one). Trust such a
        // size only once it holds for two frames, so settling doesn't flash a tiny menu.
        bool settled = needed == _lastNeeded;
        _lastNeeded = needed;
        if (!settled && needed.Y > available.Y * 2f)
            return;

        float scale = Mathf.Min(1f, Mathf.Min(available.X / needed.X, available.Y / needed.Y));
        _target.PivotOffset = _target.Size / 2f;
        _target.Scale = Vector2.One * scale;
    }
}
