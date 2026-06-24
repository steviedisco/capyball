# 🦫 CAPYBALL

A juicy, chunky, Sega-bright **Super Monkey Ball** clone starring a capybara.
Roll a capybara around inside a glowing ball, collect melons, hit boost pads,
ricochet off bumpers, and reach the glowing goal across three escalating stages.

Built in **Godot 4.3 (.NET)** with **C#** — zero external art or audio assets.
Every model, material, particle, sound, menu, and level is generated in code.

![capyball icon](icon.svg)

---

## ✨ Features

**Game feel / "juice"**
- Procedural **squash & stretch** on jumps, landings, boosts, and bumps
- **Screen shake** with trauma² falloff, driven through a central `Fx` autoload
- **Hit-stop** (brief slow-motion) on big impacts, goals, and bumper hits
- **Glow / bloom** post-processing with boosted saturation and filmic tonemapping
- **Particle bursts** on every contact, pickup, launch, and level clear
- **Confetti** showers and a triumphant arpeggio on goal reach
- **Camera lead-ahead** based on ball velocity for a sense of speed
- Speed-reactive **trail** and **boost flame jet**

**Audio — fully synthesized**
- A tiny runtime synth (`Synth.cs`) generates every SFX from waveforms:
  rolling rumble, bumps, landings, boost sweeps, pickup arps, goal fanfares,
  fall whistles, UI clicks, whooshes. No `.wav`/`.ogg` files ship.
- Dedicated SFX bus with a hard limiter so stacked sounds never clip.

**Presentation — fully procedural**
- Capybara model (body, head, snout, ears, glossy eyes) built from primitives
- Chunky beveled platforms with emissive glow rims in a saturated candy palette
- Spinning glow-rings, energy beams, point lights on every interactive prop
- Gradient skies, fog, sun + ambient lighting, ambient sparkles for depth

**Gameplay**
- Arcade ball-rolling physics: tilt-to-roll, hard speed cap, air control
- **Boost** (impulse along facing) and **jump** with ground check
- Fall detection into the void → retry
- Best-time + completion persistence (`user://progress.cfg`)

---

## 🎮 Controls

| Action | Keyboard | Gamepad |
|--------|----------|---------|
| Roll   | `WASD` / Arrow keys | Left stick / D-pad |
| Jump   | `Space` / LMB | A / Cross |
| Boost  | `Shift` | B / Circle |
| Restart level | `R` | — |
| Back to menu | `Esc` | — |

---

## 🗺️ The three levels

Each stage gets a little more complex and introduces a new mechanic.

1. **First Steps** — a wide, forgiving runway with a gentle bend. Pure rolling
   and jumping. A forgiving intro.
2. **Ups and Downs** — ramps launch you, a boost pad carries you across a gap,
   and a moving bridge spans the void. Melons tempt you off the safe line.
3. **Pinball Palace** — the showpiece. A zig-zag of narrow platforms, a bumper
   arena to ricochet through, and two boost jumps over a big gap to a dramatic
   goal pedestal ringed by glowing pillars.

---

## 🛠️ Opening in Rider / VS

This is a standard Godot C# project. The `Capyball.sln` + `Capyball.csproj`
target **net8.0** with `Godot.NET.Sdk` 4.3.0.

```
# From Rider: File → Open → Capyball.sln
# Or build on the command line:
dotnet build
```

To open the project in the Godot editor (to run/edit scenes):
- **Godot 4.3 (.NET)** — open the project folder. The C# solution is picked up
  automatically.

---

## 🚀 Running

The fastest way to run:

```bash
# With Godot 4.3 .NET installed and on PATH:
godot --path . res://Main.tscn
```

Or from the Godot editor: press **F5** (the main scene is `res://Main.tscn`).

**Headless / CI smoke test** (loads each level and runs physics with no GPU):

```bash
godot --headless --quit-after 150 res://Main.tscn
```

---

## 📁 Project structure

```
.
├── project.godot          # Godot project config: autoloads, input map, rendering
├── Capyball.sln / .csproj # C# solution for Rider / VS
├── Main.tscn / Main.cs    # Root scene + game-flow controller (menu ↔ level)
├── icon.svg               # Procedural app icon
└── src/
    ├── Palette.cs         # Shared Sega-bright colour palette
    ├── Procedural.cs      # Procedural mesh + material builders (no asset files)
    ├── Fx.cs              # Centralized particle bursts / shake / hit-stop
    ├── Synth.cs           # Runtime procedural audio engine
    ├── GameState.cs       # Progress / best-time persistence autoload
    ├── Actors/
    │   ├── CapyballBall.cs   # The player: rolling physics, juice, trail
    │   └── CameraFollow.cs   # Smooth chase cam + screen shake
    ├── Levels/
    │   ├── LevelSpec.cs      # Declarative level data + registry
    │   ├── LevelScene.cs     # Runtime: builds geometry, runs the loop, HUD
    │   ├── Level1.cs / Level2.cs / Level3.cs
    │   ├── Goal.cs / Melon.cs / BoostPad.cs / Bumper.cs / MovingPlatform.cs
    ├── Effects/
    │   └── Stage.cs          # Environment, sky, lighting, post-processing
    └── UI/
        ├── MainMenu.cs       # Animated title + level select
        └── Hud.cs            # In-level timer / melon HUD + win/lose overlays
```

---

## 🧪 Design notes

- **No scene files for gameplay.** Levels are plain C# data (`LevelSpec`)
  consumed by a single `LevelScene` runtime. This keeps authoring terse and
  diff-friendly, and means the whole game fits in a handful of scripts.
- **No external assets** by design — the repo is tiny, builds anywhere, and
  every visual/audio is tweakable from code.
- **Physics:** the ball is a `RigidBody3D` with a camera-relative steering model
  and a hard speed cap for arcade feel; ground is detected via a shape query.

---

## ⚠️ Requirements

- **Godot 4.3 (.NET / mono)** — the project uses the C# bindings.
- **.NET 8 SDK** (the Godot .NET editor bundles this).

Made with rolling capybaras and saturated colours. Enjoy. 🍉
