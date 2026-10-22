# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Working relationship

Claude acts as **game design and programming partner**. The user is a software architect with deep web, database, and .NET/C# experience but little game-design or Godot experience. That means:

- Explain Godot and game-dev concepts when they come up, mapping them to .NET/web ideas where that helps (scenes ≈ composable components/prefabs, signals ≈ C# events, autoloads ≈ singleton services, Resources ≈ serializable data objects/config).
- Don't over-explain general C#, architecture, or data-modeling topics.
- On design questions, give a recommendation with reasoning (genre conventions from Brotato / Vampire Survivors are a good reference), not just a list of options. Flag decisions that are expensive to reverse later.

## Project state

Playable prototype: class select (Fighter, Mage) → 5-wave run with gold drops and a between-wave shop of stat items. Shapes are placeholders drawn with `_Draw()`, no art.

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

Use the Movie Maker command to look at gameplay yourself (read a few PNGs) rather than asking the user to check. At 20 fps, 1250 frames is about 60s of game time.

`--headless --fixed-fps 60 --disable-vsync --quit-after N` runs the game uncapped, so a full 5-wave run takes about a second. To exercise flows that need input (shop purchases, continuing), add a temporary hook gated on `OS.GetCmdlineUserArgs()` (pass args after `--`), log with `GD.Print`, then remove it. No test framework is set up yet.

## Architecture

- `scenes/<feature>/` holds a `.tscn` scene paired with its `.cs` script. `scripts/` holds shared, scene-less code. `data/` holds `.tres` Resource files (game content).
- `Main.cs` is the run controller and the only place signals get wired together. Its flow is ClassSelect → Wave → Shop → Wave … → Victory or Defeat. The tree is paused whenever a wave isn't running. The UI layers (`ClassSelect`, `Shop`, `Hud`) are `process_mode = Always`, so they still work while paused. Restart (R) reloads the scene.
- **Classes:** `ClassData` Resource (`data/classes/`) holding base max HP, move speed and armor, a weapon, and up to 4 spells. `Player.Setup(classData)` must run before `ApplyStats`. Classes appear on the select screen only once they're listed in `Main.tscn`'s `Classes` array.
- **Abilities:** each class's weapon is its always-on basic attack; spells are stronger attacks on longer cooldowns. Both are `AbilityData` Resources (`data/weapons/`, `data/spells/`). Each subclass holds both the tuning values and the behavior: `ProjectileAbility` (optionally explodes), `AreaAbility` (around the caster or at a target point), `MeleeArcAbility`. A new ability with existing behavior is a new `.tres`; a new kind of behavior is a new subclass that overrides `TryAutoTarget` and `Cast`. `LeapAbility` moves the player via `Player.Leap` (untouchable while airborne), then blasts where it lands. Abilities that move the player should set `AutoCastByDefault = false`. Resources are shared, so runtime state (cooldown, auto-cast on/off) lives in `AbilitySlot`.
- **`AbilityCaster`** (child of Player) always auto-fires the weapon, and auto-casts each spell slot that has auto-cast on. Keys 1–4 (`ability_N` actions) cast manually, aiming at the mouse when `NeedsAim` is true. Shift+key or right-clicking a hotbar slot toggles auto-cast. Damage% applies to all abilities, AttackSpeed% only to the weapon's cooldown, and Range to all (melee gets half). Hit side effects (slow, knockback) are `HitEffects`, applied through `Combat.Hit` and `Combat.DamageArea`.
- **Run state:** `RunState` is plain C#, owned by `Main`. It holds wave, gold, kills, owned items and `PlayerStats`. A scene reload discards it, so persistent "career" progress will need to live somewhere else, such as an autoload.
- **Stats:** `StatType` enum plus `PlayerStats` (summed bonuses; percent stats stored as whole numbers). `Player.ApplyStats` takes base values from the `ClassData` and layers the bonuses on top. Call `ApplyStats` again whenever stats change. `StatType` values are serialized as integers in `.tres` files: add new entries, but never renumber existing ones.
- **Items:** `ItemData` (a `[GlobalClass]` Resource holding `StatModifier` sub-resources), one `.tres` per item in `data/items/`. An item only shows up in the shop once it's listed in `data/ItemCatalog.tres`.
- **Waves:** `WaveSettings.For(wave)` is a formula giving duration, spawn rate and enemy health/damage/speed scaling. `EnemySpawner.Begin` takes the settings and `Enemy.ApplyWave` scales each spawn.
- **Enemies:** every type shares `Enemy.tscn`. An `EnemyData` Resource (`data/enemies/`) sets shape, stats, knockback resistance, spawn settings (`FirstWave`, `SpawnWeight`) and an optional ranged attack (`ShotRange > 0` makes the enemy keep its distance and shoot). The spawner sets `enemy.Data` before `AddChild` and picks types by weight from those unlocked for the wave. A type only spawns once it's listed in the Spawner's `EnemyTypes` in `Main.tscn`. Enemy shots reuse `Projectile` with `CollisionMask` set to world + player.
- **Gold:** enemies drop a `Coin` when they die (added deferred, because death happens inside a physics callback). Coins fly to the player once they're within `PickupRange`. Coins and projectiles are in `Groups.Transient` and get cleared at wave end, so uncollected gold is lost.
- Cross-entity lookups use **groups** (`Groups.Player`, `Groups.Enemies`), not node paths. Damage goes through `IDamageable`, and entities hand it to a child `Health` node that emits `Changed`/`Died`.
- `Arena` is the single source of truth for play bounds: the rect from (0,0) to `Size`. It builds its own wall collision in code.
- `EnemySpawner` places a `SpawnMarker` (a blinking X warning) first, then spawns the enemy as its own child. Spawn interval goes from `StartInterval` to `EndInterval` as `Progress` (0 to 1, set by `Main`) increases. `Clear()` removes everything.
- Projectiles and `AreaFlash` effects are added to `CurrentScene`, not the player, so they don't move with it. Add them deferred when spawning from a physics callback.
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
