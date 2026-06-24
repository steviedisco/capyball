using Godot;

namespace Capyball;

/// <summary>
/// In-level HUD: chunky Sega-style timer + melon counter with pop animations,
/// plus the win / lose overlays. All drawn procedurally — no font files shipped,
/// uses the engine default font with custom styling.
/// </summary>
public partial class Hud : CanvasLayer
{
    private Label _title;
    private Label _subtitle;
    private Label _timer;
    private Label _melons;
    private Label _hint;
    private Control _overlay;
    private Label _overlayTitle;
    private Label _overlayBody;
    private Button _overlayPrimary;
    private Button _overlaySecondary;

    private LevelScene _level;

    public static Hud Create()
    {
        var hud = new Hud { Layer = 10 };
        hud.Build();
        return hud;
    }

    private Theme ChunkyTheme(int size, Color color)
    {
        var theme = new Theme();
        var fontFile = new FontFile();
        // Use the default font variation for portability.
        theme.DefaultFont = new SystemFont { FontWeight = 800 };
        theme.SetFontSize("font_size", "Label", size);
        theme.SetColor("font_color", "Label", color);
        theme.SetFontSize("font_size", "Button", size - 4);
        theme.SetColor("font_color", "Button", Palette.UiCream);
        return theme;
    }

    private void Build()
    {
        // Title block (top-left).
        var top = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(top);
        top.SetAnchorsPreset(Control.LayoutPreset.TopLeft);

        _title = MakeLabel("", 40, Palette.UiCream, outline: Palette.UiInk);
        _title.Position = new Vector2(28, 18);
        top.AddChild(_title);

        _subtitle = MakeLabel("", 20, new Color(1, 0.95f, 0.8f), outline: Palette.UiInk);
        _subtitle.Position = new Vector2(30, 62);
        top.AddChild(_subtitle);

        // Timer (top-right).
        _timer = MakeLabel("0.00", 56, Palette.Sunny, outline: Palette.UiInk);
        _timer.HorizontalAlignment = HorizontalAlignment.Right;
        _timer.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _timer.Position = new Vector2(-180, 18);
        _timer.Size = new Vector2(150, 64);
        top.AddChild(_timer);

        // Melon counter (under timer).
        _melons = MakeLabel("0/0", 28, new Color(0.5f, 1f, 0.5f), outline: Palette.UiInk);
        _melons.HorizontalAlignment = HorizontalAlignment.Right;
        _melons.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _melons.Position = new Vector2(-180, 84);
        _melons.Size = new Vector2(150, 36);
        top.AddChild(_melons);

        // Hint (bottom).
        _hint = MakeLabel("Tilt to roll (WASD / Arrows)  ·  R restart  ·  ESC menu", 16, Palette.UiCream);
        _hint.HorizontalAlignment = HorizontalAlignment.Center;
        _hint.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        _hint.Position = new Vector2(0, -36);
        top.AddChild(_hint);

        // Overlay (win / lose).
        _overlay = new Control();
        _overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _overlay.MouseFilter = Control.MouseFilterEnum.Stop;
        _overlay.Visible = false;
        AddChild(_overlay);

        var dim = new ColorRect { Color = new Color(0.05f, 0.07f, 0.15f, 0.55f) };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _overlay.AddChild(dim);

        var panel = new Panel();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(620, 320);
        panel.Position = new Vector2(-310, -160);
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.98f, 0.95f, 0.88f, 0.96f),
            BorderWidthBottom = 12,
            BorderColor = Palette.HotPink,
            CornerRadiusTopLeft = 16,
            CornerRadiusTopRight = 16,
            CornerRadiusBottomLeft = 16,
            CornerRadiusBottomRight = 16,
            ContentMarginLeft = 28,
            ContentMarginRight = 28,
            ContentMarginTop = 24,
            ContentMarginBottom = 24,
        };
        panel.AddThemeStyleboxOverride("panel", sb);
        _overlay.AddChild(panel);

        _overlayTitle = MakeLabel("", 52, Palette.UiInk, grow: true);
        _overlayTitle.HorizontalAlignment = HorizontalAlignment.Center;
        _overlayTitle.Position = new Vector2(0, 24);
        _overlayTitle.Size = new Vector2(620, 70);
        panel.AddChild(_overlayTitle);

        _overlayBody = MakeLabel("", 24, new Color(0.2f, 0.25f, 0.35f), grow: true);
        _overlayBody.HorizontalAlignment = HorizontalAlignment.Center;
        _overlayBody.Position = new Vector2(0, 110);
        _overlayBody.Size = new Vector2(620, 90);
        panel.AddChild(_overlayBody);

        _overlayPrimary = MakeButton("CONTINUE", Palette.HotPink);
        _overlayPrimary.Position = new Vector2(160, 220);
        _overlayPrimary.Size = new Vector2(300, 60);
        panel.AddChild(_overlayPrimary);

        _overlaySecondary = MakeButton("RETRY", Palette.Ocean);
        _overlaySecondary.Position = new Vector2(160, 220);
        _overlaySecondary.Size = new Vector2(300, 60);
        _overlaySecondary.Visible = false;
        panel.AddChild(_overlaySecondary);
    }

    private Label MakeLabel(string text, int size, Color color, Color outline = default, bool grow = false)
    {
        var l = new Label { Text = text };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color);
        if (outline != default)
        {
            l.AddThemeColorOverride("font_outline_color", outline);
            l.AddThemeConstantOverride("outline_size", Mathf.Max(2, size / 12));
        }
        if (grow)
        {
            l.AddThemeConstantOverride("outline_size", 3);
        }
        return l;
    }

    private Button MakeButton(string text, Color color)
    {
        var b = new Button { Text = text };
        b.AddThemeFontSizeOverride("font_size", 22);
        b.AddThemeColorOverride("font_color", Palette.UiCream);
        var sb = new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 16, ContentMarginRight = 16,
            ContentMarginTop = 10, ContentMarginBottom = 10,
            BorderWidthBottom = 6,
            BorderColor = new Color(color.R * 0.6f, color.G * 0.6f, color.B * 0.6f),
        };
        b.AddThemeStyleboxOverride("normal", sb);
        var hover = (StyleBoxFlat)sb.Duplicate();
        hover.BgColor = new Color(Mathf.Min(1, color.R + 0.15f), Mathf.Min(1, color.G + 0.15f), Mathf.Min(1, color.B + 0.15f));
        b.AddThemeStyleboxOverride("hover", hover);
        b.AddThemeStyleboxOverride("pressed", sb);
        return b;
    }

    public void Bind(LevelScene level)
    {
        _level = level;
        _overlayPrimary.Pressed += () => _level.GoNext();
        _overlaySecondary.Pressed += () => _level.Restart();
    }

    public void SetTitle(string t, string s) { _title.Text = t; _subtitle.Text = s; }

    public void UpdateTimer(float t)
    {
        _timer.Text = t.ToString("0.00");
        // Tiny scale-pop each second to feel alive.
        if (Mathf.FloorToInt(t) != _lastSecond)
        {
            _lastSecond = Mathf.FloorToInt(t);
            var tw = CreateTween();
            tw.TweenProperty(_timer, "scale", new Vector2(1.18f, 1.18f), 0.08f);
            tw.TweenProperty(_timer, "scale", Vector2.One, 0.12f);
        }
    }

    public void UpdateMelons(int have, int total)
    {
        if (have > _lastMelons)
        {
            var tw = CreateTween();
            tw.TweenProperty(_melons, "scale", new Vector2(1.3f, 1.3f), 0.08f);
            tw.TweenProperty(_melons, "scale", Vector2.One, 0.14f);
        }
        _lastMelons = have;
        _melons.Text = $"🍉 {have}/{total}";
    }

    public void ShowWin(float time, int melons, int total, bool last)
    {
        _overlayTitle.Text = last ? "YOU DID IT!" : "LEVEL CLEAR!";
        _overlayBody.Text = $"Time: {time:0.00}s    ·    Melons: {melons}/{total}";
        _overlayPrimary.Text = last ? "BACK TO MENU" : "NEXT LEVEL";
        _overlaySecondary.Visible = true;
        _overlaySecondary.Text = "RETRY";
        _overlay.Visible = true;
        PopOverlay();
    }

    public void ShowLose()
    {
        _overlayTitle.Text = "WHOOPS!";
        _overlayBody.Text = "The capybara tumbled into the void.\nGive it another roll?";
        _overlayPrimary.Text = "RETRY";
        _overlaySecondary.Visible = false;
        _overlay.Visible = true;
        PopOverlay();
    }

    private void PopOverlay()
    {
        var panel = _overlay.GetChild<Panel>(1);
        panel.Scale = Vector2.One * 0.8f;
        panel.PivotOffset = panel.Size / 2f;
        var tw = CreateTween();
        tw.TweenProperty(panel, "scale", Vector2.One * 1.05f, 0.18f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tw.TweenProperty(panel, "scale", Vector2.One, 0.10f);
    }

    private int _lastSecond = -1;
    private int _lastMelons = 0;
}

