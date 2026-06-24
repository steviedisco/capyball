# 🦫 CAPYBALL

A **Super Monkey Ball** clone starring a capybara, built in **Godot 4.3 (.NET)** + **C#**.

Tilt the world to roll a capybara in a glowing ball through three stages — collect
melons, hit boost pads, ricochet off bumpers, and roll through the goal gate.

![capyball icon](icon.svg)

## Run

```bash
# Godot 4.3 .NET on PATH:
godot --path . res://Main.tscn
```

Or open the project folder in the Godot 4.3 (.NET) editor and press **F5**.

## Open in Rider / VS

Open `Capyball.sln` (targets `net8.0`, `Godot.NET.Sdk` 4.3.0), or `dotnet build`.

## Controls

- **WASD / Arrows** — tilt the course to roll
- **R** — restart level · **Esc** — back to menu

## Notes

- **Mechanic:** input banks the actual course geometry (a tilt pivot); the ball is
  plain `RigidBody3D` physics that rolls down the real slope. The camera stays
  upright.
- **Assets:** materials (`.tres`), textures (`.png`), and the capybara model
  (`.tscn`) live in `assets/` and are editable in the Godot Inspector. Audio is
  synthesized at runtime. Regenerate textures with `python3 tools/gen_textures.py`.
- **Levels** are plain C# data (`src/Levels/Level*.cs`).

> The code is the source of truth — browse `src/` for details on feel knobs,
> the synth, FX, and level authoring.

Requires **Godot 4.3 (.NET)** and the **.NET 8 SDK**.
