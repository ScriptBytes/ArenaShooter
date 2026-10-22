# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Working relationship

Claude acts as **game design and programming partner**. The user is a software architect with deep web, database, and .NET/C# experience but little game-design or Godot experience. That means:

- Explain Godot and game-dev concepts when they come up, mapping them to .NET/web ideas where that helps (scenes ≈ composable components/prefabs, signals ≈ C# events, autoloads ≈ singleton services, Resources ≈ serializable data objects/config).
- Don't over-explain general C#, architecture, or data-modeling topics.
- On design questions, give a recommendation with reasoning (genre conventions from Brotato / Vampire Survivors are a good reference), not just a list of options. Flag decisions that are expensive to reverse later.

## Project state

Playable prototype of the core loop: one 60s round, placeholder shapes drawn with `_Draw()`, no art. The repo is not under git yet.

- **Engine:** Godot 4.7, **.NET (C#) build** (`/Applications/Godot_mono.app`). Assembly name `ArenaShooter`. Write gameplay code in C#, not GDScript, unless there's a specific reason.
- **Renderer:** currently `mobile`, with Jolt set for 3D physics (scaffold defaults). The game is 2D; revisit the renderer choice (Forward+ / Compatibility) before relying on renderer-specific features.
- **.NET SDK:** 8.0.100 installed at `/usr/local/share/dotnet`.

## Commands

```bash
GODOT=/Applications/Godot_mono.app/Contents/MacOS/Godot

$GODOT -e --path .                  # open the editor on this project
$GODOT --path .                     # run the main scene
$GODOT --path . res://path/to/scene.tscn   # run a specific scene
$GODOT --headless --path . --import # re-import assets / regenerate .godot cache
dotnet build                        # compile C#
$GODOT --headless --path . --quit-after 400   # smoke test: surfaces runtime errors in stderr

# Visual check without screen-capture permissions: Movie Maker writes every frame as PNG
$GODOT --path . --write-movie <scratch>/f.png --fixed-fps 20 --quit-after 1250
```

Use the Movie Maker command to look at gameplay yourself (read a few PNGs) rather than asking the user to check. At 20 fps, 1250 frames covers a full 60s round. No test framework is set up yet.

## Architecture

- `scenes/<feature>/` holds a `.tscn` scene paired with its `.cs` script. `scripts/` holds shared, scene-less code (`Health` component, `IDamageable`, `Groups` constants).
- `Main.tscn` is the round controller. `Main.cs` wires everything together in `_Ready`: it positions the player, sets camera limits from `Arena.Size`, connects signals to the `Hud`, runs the round timer, and pauses the tree on win or loss. `Hud` is `process_mode = Always`, so it can catch the restart key (R) while paused. Restart reloads the scene.
- Cross-entity lookups use **groups** (`Groups.Player`, `Groups.Enemies`), not node paths. Damage goes through `IDamageable`, and entities hand it to a child `Health` node that emits `Changed`/`Died`.
- `Arena` is the single source of truth for play bounds: the rect from (0,0) to `Size`. It builds its own wall collision in code.
- `EnemySpawner` places a `SpawnMarker` (a blinking X warning) first, then spawns the enemy as its own child. Spawn interval goes from `StartInterval` to `EndInterval` as `Progress` (0 to 1, set by `Main`) increases. `Clear()` removes everything.
- `Weapon` auto-aims at the nearest enemy in range (Brotato-style). Projectiles are added to `CurrentScene`, not the player, so they don't move with it.
- **Physics layers** (named in project.godot): 1 world, 2 player, 3 enemies, 4 projectiles. Bitmask values are 1, 2, 4, 8. Enemies collide with walls and each other, not the player. Contact damage comes from the player's `Hurtbox` Area2D, with i-frames.
- Balance numbers are `[Export]` properties, editable in the Inspector or in the `.tscn` files.

## Godot C# conventions to follow

- Script classes must be `partial` and the class name must match the file name.
- Signals: `[Signal] public delegate void SomethingHappenedEventHandler(...)`; emit with `EmitSignal(SignalName.SomethingHappened, ...)`.
- `[Export]` fields only appear in the editor after a C# build.
- `.tscn`/`.tres` are text and can be edited directly, but keep `uid://` references and `ext_resource` ids consistent. Commit the `.uid` files Godot generates next to scripts/resources. `.godot/` is generated and ignored.

## Game design direction

See `Goals.md`, which is a **rough, non-binding brainstorm**: treat it as a baseline, not a spec. Summary:

- Fantasy arena-survivor (Brotato / Vampire Survivors style): short timed rounds (1–2 min), enemies spawn around the map, and players manage inventory/shop between rounds.
- RPG layer: equipment slots (armor, rings, trinkets), consumable potions/buffs usable mid-round.
- Distinct classes: Fighter (weapons, sturdy), Mage (magic, fragile, elemental builds like frost/fire), Warlock (life drain), Necromancer (minions). Each class picks 3–4 spells from a spell list or skill tree at run start.
- Open questions: persistent progression vs. fresh runs (possibly both, "career" vs. "arcade" modes); what the currency is; pickup vs. auto-collected currency; shop vs. drops vs. both, with selling.

When decisions get made, record them in `Goals.md` (or a design doc) so they don't get re-litigated.
