using Godot;

namespace Capyball;

/// <summary>
/// The title screen. Animated gradient backdrop, floating decorative shapes,
/// a big juicy logo with a bobbing capy, and chunky level-select buttons with
/// completion badges and best-time. Pure procedural — no art assets.
/// </summary>
public partial class MainMenu : CanvasLayer
{
    public static MainMenu Create()
    {
        var m = new MainMenu { Layer = 5 };
        m.Build();
        return m;
    }

    private Control _root;
    private float _t;

    private void Build()
    {
        _root = new Control();
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        // Background — gradient via a custom draw.
        var bg = new MenuBackground();
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(bg);

        // Floating decorative capyball silhouettes (animated via tweens, no per-frame redraw).
        for (int i = 0; i < 7; i++)
        {
            var deco = new MenuOrb
            {
                Position = new Vector2((float)GD.RandRange(80, 1500), (float)GD.RandRange(60, 880)),
                Scale = Vector2.One * (float)GD.RandRange(0.4f, 1.1f),
                Color = Palette.Platforms[i % Palette.Platforms.Length],
                Speed = (float)GD.RandRange(0.3f, 0.9f),
                Phase = GD.Randf() * Mathf.Tau,
            };
            _root.AddChild(deco);
            // Gentle bob via tween loop.
            float baseY = deco.Position.Y;
            float amp = 24f * deco.Speed;
            var bob = deco.CreateTween().SetLoops();
            bob.TweenProperty(deco, "position:y", baseY - amp, 2.2f * deco.Speed).SetTrans(Tween.TransitionType.Sine);
            bob.TweenProperty(deco, "position:y", baseY + amp, 2.2f * deco.Speed).SetTrans(Tween.TransitionType.Sine);
            var spin = deco.CreateTween().SetLoops();
            spin.TweenProperty(deco, "rotation", Mathf.Tau, 6f).SetTrans(Tween.TransitionType.Linear);
        }

        // Title.
        var title = MakeLabel("CAPYBALL", 140, Palette.UiCream, outline: Palette.HotPink, outlineSize: 18);
        title.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        title.Position = new Vector2(-450, 80);
        title.Size = new Vector2(900, 160);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _root.AddChild(title);

        var subtitle = MakeLabel("a capybara in a ball, a world to roll", 28, new Color(1, 0.95f, 0.8f), outline: Palette.UiInk, outlineSize: 5);
        subtitle.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        subtitle.Position = new Vector2(-300, 220);
        subtitle.Size = new Vector2(600, 40);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        _root.AddChild(subtitle);

        // Level buttons.
        var grid = new VBoxContainer();
        grid.AddThemeConstantOverride("separation", 18);
        grid.SetAnchorsPreset(Control.LayoutPreset.Center);
        grid.Position = new Vector2(-180, 20);
        grid.CustomMinimumSize = new Vector2(360, 0);
        _root.AddChild(grid);

        for (int i = 0; i < Levels.Order.Length; i++)
        {
            string id = Levels.Order[i];
            var spec = Levels.Get(id);
            bool done = GameState.Instance.IsCompleted(id);
            float best = GameState.Instance.GetBest(id);

            var btn = new LevelButton
            {
                Text = $"{i + 1}.  {spec.Title.ToUpper()}",
                CustomMinimumSize = new Vector2(360, 70),
                Index = i,
                Done = done,
                Best = best,
                Color = Palette.Platforms[i % Palette.Platforms.Length],
            };
            btn.Pressed += () => Main.Instance.GotoLevel(id);
            grid.AddChild(btn);
        }

        // Footer hint.
        var hint = MakeLabel("click a level to begin  ·  ESC returns to this menu", 18, Palette.UiCream, outline: Palette.UiInk, outlineSize: 3);
        hint.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        hint.Position = new Vector2(0, -40);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        _root.AddChild(hint);

        // Animate the title with a gentle bob.
        var tw = CreateTween().SetLoops();
        tw.TweenProperty(title, "position:y", 96, 1.6f).SetTrans(Tween.TransitionType.Sine);
        tw.TweenProperty(title, "position:y", 80, 1.6f).SetTrans(Tween.TransitionType.Sine);
    }

    private Label MakeLabel(string text, int size, Color color, Color outline = default, int outlineSize = 0)
    {
        var l = new Label { Text = text };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color);
        if (outline != default)
        {
            l.AddThemeColorOverride("font_outline_color", outline);
            l.AddThemeConstantOverride("outline_size", outlineSize);
        }
        return l;
    }
}

/// <summary>Custom-drawn gradient backdrop. Drawn once (no per-frame redraw) to
/// avoid churning the message queue; the bands are static, which is plenty for a title screen.</summary>
public partial class MenuBackground : ColorRect
{
    public override void _Ready()
    {
        Color = new Color(0.16f, 0.45f, 0.72f);
        SetAnchorsPreset(Control.LayoutPreset.FullRect);
    }

    public override void _Draw()
    {
        var size = Size;
        // Vertical gradient via horizontal bands.
        int bands = 40;
        for (int i = 0; i < bands; i++)
        {
            float f = i / (float)bands;
            Color c = new Color(
                Mathf.Lerp(0.18f, 0.62f, f),
                Mathf.Lerp(0.50f, 0.90f, f),
                Mathf.Lerp(0.92f, 1.00f, f));
            DrawRect(new Rect2(0, f * size.Y, size.X, size.Y / bands + 1), c);
        }
    }
}

/// <summary>A static translucent orb decoration (drawn once, no per-frame redraw).</summary>
public partial class MenuOrb : Control
{
    public Color Color;
    public float Speed;
    public float Phase;

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 34, new Color(Color.R, Color.G, Color.B, 0.30f));
        DrawCircle(Vector2.Zero, 26, new Color(Color.R, Color.G, Color.B, 0.18f));
    }
}

/// <summary>A chunky level-select button with completion badge + best time.</summary>
public partial class LevelButton : Button
{
    public int Index;
    public bool Done;
    public float Best;
    public Color Color;

    public override void _Ready()
    {
        AddThemeFontSizeOverride("font_size", 24);
        AddThemeColorOverride("font_color", Palette.UiCream);
        AddThemeConstantOverride("outline_size", 4);
        AddThemeColorOverride("font_outline_color", Palette.UiInk);
        var sb = new StyleBoxFlat
        {
            BgColor = Color,
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
            ContentMarginLeft = 24, ContentMarginRight = 24,
            ContentMarginTop = 16, ContentMarginBottom = 16,
            BorderWidthBottom = 7,
            BorderColor = new Color(Color.R * 0.55f, Color.G * 0.55f, Color.B * 0.55f),
        };
        AddThemeStyleboxOverride("normal", sb);
        var hover = (StyleBoxFlat)sb.Duplicate();
        hover.BgColor = new Color(Mathf.Min(1, Color.R + 0.12f), Mathf.Min(1, Color.G + 0.12f), Mathf.Min(1, Color.B + 0.12f));
        AddThemeStyleboxOverride("hover", hover);
        AddThemeStyleboxOverride("pressed", sb);
    }

    public override void _Draw()
    {
        if (Done)
        {
            DrawCircle(new Vector2(Size.X - 34, Size.Y / 2), 12, Palette.Sunny);
            // Best time micro-label.
            var font = GetThemeDefaultFont();
            DrawString(font, new Vector2(Size.X - 130, Size.Y / 2 + 8),
                $"best {Best:0.00}s", HorizontalAlignment.Left, -1f, 18, new Color(1, 0.95f, 0.6f));
        }
    }
}
