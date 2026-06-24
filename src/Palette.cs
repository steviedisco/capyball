using Godot;

namespace Capyball;

/// <summary>
/// Saturated, Sega-bright palette shared across the whole game so every level
/// feels like it belongs to the same candy-coated universe.
/// </summary>
public static class Palette
{
    // Sky / atmosphere
    public static readonly Color SkyTop = new(0.20f, 0.62f, 0.96f);
    public static readonly Color SkyBottom = new(0.62f, 0.88f, 1.00f);
    public static readonly Color ClearColor = new(0.55f, 0.82f, 0.99f);

    // Hero / capybara
    public static readonly Color CapyFur = new(0.66f, 0.45f, 0.25f);
    public static readonly Color CapyFurLight = new(0.90f, 0.71f, 0.50f);
    public static readonly Color CapySnout = new(0.96f, 0.85f, 0.71f);
    public static readonly Color CapyNose = new(0.23f, 0.14f, 0.10f);

    // Ball
    public static readonly Color BallTint = new(0.55f, 0.92f, 1.00f, 0.42f);
    public static readonly Color BallRim = new(0.85f, 0.99f, 1.00f);

    // Platforms — chunky candy colours
    public static readonly Color Mint = new(0.40f, 0.95f, 0.66f);
    public static readonly Color Tangerine = new(1.00f, 0.62f, 0.22f);
    public static readonly Color HotPink = new(1.00f, 0.32f, 0.58f);
    public static readonly Color Sunny = new(1.00f, 0.85f, 0.20f);
    public static readonly Color Grape = new(0.66f, 0.42f, 1.00f);
    public static readonly Color Ocean = new(0.18f, 0.55f, 0.95f);
    public static readonly Color Coral = new(1.00f, 0.48f, 0.42f);

    // FX
    public static readonly Color BoostFlame = new(1.00f, 0.55f, 0.20f);
    public static readonly Color BoostCore = new(1.00f, 0.95f, 0.55f);
    public static readonly Color ConfettiA = new(1.00f, 0.30f, 0.55f);
    public static readonly Color ConfettiB = new(0.40f, 0.95f, 0.66f);
    public static readonly Color ConfettiC = new(1.00f, 0.85f, 0.20f);
    public static readonly Color ConfettiD = new(0.55f, 0.62f, 1.00f);

    // UI
    public static readonly Color UiInk = new(0.12f, 0.16f, 0.30f);
    public static readonly Color UiCream = new(1.00f, 0.97f, 0.90f);

    public static Color[] Confetti => new[] { ConfettiA, ConfettiB, ConfettiC, ConfettiD, HotPink, Ocean };
    public static Color[] Platforms => new[] { Mint, Tangerine, HotPink, Sunny, Grape, Coral, Ocean };
}
