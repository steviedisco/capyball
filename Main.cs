using Godot;

namespace Capyball;

/// <summary>
/// The root scene. Owns the high-level game flow: title menu → level → (next |
/// menu). Builds every screen procedurally so there are no scene files to
/// maintain beyond Main.tscn.
/// </summary>
public partial class Main : Node
{
    public static Main Instance { get; private set; }

    private Node _current;

    public override void _Ready()
    {
        Instance = this;
        GotoMenu();
    }

    public void GotoMenu()
    {
        ClearCurrent();
        var menu = MainMenu.Create();
        _current = menu;
        AddChild(menu);
    }

    public void GotoLevel(string id)
    {
        ClearCurrent();
        var level = new LevelScene();
        _current = level;
        AddChild(level);
        level.Load(id);
        Synth.Instance?.Whoosh();
    }

    private void ClearCurrent()
    {
        if (_current != null)
        {
            _current.QueueFree();
            _current = null;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            // Esc returns to menu from a level.
            if (_current is LevelScene) GotoMenu();
        }
    }
}
