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
- **.NET:** the project targets `net10.0` (LTS, supported to Nov 2028). SDK 10.0.402 is installed at `/usr/local/share/dotnet`. The Godot editor builds with the SDK under `/usr/local/share/dotnet` even when `DOTNET_ROOT` points elsewhere, so a future .NET upgrade needs Microsoft's installer there. A user-local SDK isn't enough for the editor.

## Commands

`$GODOT` is the Godot .NET binary: `/Applications/Godot_mono.app/Contents/MacOS/Godot` on the user's Mac, plain `godot` in the cloud (see below).

```bash
$GODOT -e --path .                  # open the editor on this project
$GODOT --path .                     # run the main scene
$GODOT --path . res://path/to/scene.tscn   # run a specific scene
$GODOT --headless --path . --import # re-import assets / regenerate .godot cache (needed once in a fresh clone)
dotnet build                        # compile C#
$GODOT --headless --path . --quit-after 400   # smoke test: surfaces runtime errors in stderr
```

## Testing

No test framework yet. Verify features by running the game and reading logs. Check how you're running first: the user's Mac has the editor, a display and ffmpeg; a cloud session is headless Linux with only .NET and Godot.

### Autoplay (works everywhere)

`--autoplay` (after `--`) plays a whole run unattended through the real UI. It presses the class-select and shop buttons, circles the arena collecting coins, logs `[autoplay] event key=value` lines and quits when the run ends. A normal launch ignores it. Flags (full list in `scripts/debug/AutoplayOptions.cs`):

```bash
# Full 5-wave run in ~1-2s, uncapped. grep the log for results.
$GODOT --headless --path . --fixed-fps 60 --disable-vsync --quit-after 20000 -- --autoplay --class=mage --seed=1
#   --class=NAME|INDEX  --wave=N (start wave)  --seed=N (same seed = identical run, for before/after)
#   --god  --shop=cheapest|first|skip  --move=circle|still  --autocast (all spells, incl. manual ones)
#   --item="Rabbit's Foot" (repeatable)  --stat=CritChance:20 (repeatable)
```

`--quit-after 20000` is a safety net: Autoplay is C#, so if the C# assembly fails to load (build missing, wrong .NET), nothing quits the game. `run_start` logs the .NET runtime version. Events: `run_start`, `wave_start`, `wave_end` (kills, gold, bag, hp), `buy`, `run_end` (result, wave, kills, gold, items), `error`. Bad flags log `error` and exit with code 1. To measure something new, add a temporary `GD.Print` where it happens, run with fixed seeds before and after the change, compare, then remove the print. If a test needs control Autoplay doesn't offer yet, add a flag to `AutoplayOptions` rather than a throwaway hook.

### Locally (user's Mac)

Everything above, plus looking at the game. Movie Maker writes every frame as a PNG, which works without screen-capture permission (combine with `--autoplay` to skip the menus):

```bash
$GODOT --path . --write-movie <scratch>/f.png --fixed-fps 20 --quit-after 1250 -- --autoplay --god
```

Read a few PNGs (or tile crops into a contact sheet with ffmpeg) rather than asking the user to check. At 20 fps, 1250 frames is about 60s of game time. Docker is installed for testing Linux setups (start Docker Desktop first).

### In the cloud / headless

The environment comes from `tools/cloud-setup.sh` (.NET SDK + Godot .NET binary as `godot`; tested on fresh Ubuntu 24.04, x86_64 and arm64, with net8.0 and net10.0). Run from the repo root, it reads the Godot and .NET versions from `ArenaShooter.csproj`. Its fallback defaults (used when it runs outside the repo) should match the `.csproj`. In a fresh session:

```bash
export GODOT=godot                     # the setup script also puts this in ~/.bashrc
$GODOT --headless --path . --import    # once, if .godot/ is missing
dotnet build
```

Then `dotnet build`, the smoke test and Autoplay runs are the whole toolkit. There is no display or GPU, so no Movie Maker and no screenshots. Say so in the summary: visual changes are unverified until the user playtests. The user pulls the branch and playtests on the Mac.

## Architecture

- `scenes/<feature>/` holds a `.tscn` scene paired with its `.cs` script. `scripts/` holds shared, scene-less code. `data/` holds `.tres` Resource files (game content).
- `Main.cs` is the run controller and the only place signals get wired together. It emits `RunStarted`/`WaveStarted`/`WaveEnded`/`RunEnded`, and when launched with `--autoplay` it adds the `Autoplay` debug driver (`scripts/debug/`), which uses `ClassSelect.Choices` and `Shop.Offers`/`ContinueButton` to press real buttons. Its flow is ClassSelect → Wave → Shop → Wave … → Victory or Defeat. The tree is paused whenever a wave isn't running. The UI layers (`ClassSelect`, `Shop`, `Hud`) are `process_mode = Always`, so they still work while paused. Restart (R) reloads the scene.
- **Classes:** `ClassData` Resource (`data/classes/`) holding base max HP, move speed and armor, a weapon, and up to 4 spells. `Player.Setup(classData)` must run before `ApplyStats`. Classes appear on the select screen only once they're listed in `Main.tscn`'s `Classes` array.
- **Abilities:** each class's weapon is its always-on basic attack; spells are stronger attacks on longer cooldowns. Both are `AbilityData` Resources (`data/weapons/`, `data/spells/`). Each subclass holds both the tuning values and the behavior: `ProjectileAbility` (optionally explodes), `AreaAbility` (around the caster or at a target point), `MeleeArcAbility`. A new ability with existing behavior is a new `.tres`; a new kind of behavior is a new subclass that overrides `TryAutoTarget` and `Cast`. `LeapAbility` moves the player via `Player.Leap` (untouchable while airborne), then blasts where it lands. Abilities that move the player should set `AutoCastByDefault = false`. Resources are shared, so runtime state (cooldown, auto-cast on/off) lives in `AbilitySlot`.
- **`AbilityCaster`** (child of Player) always auto-fires the weapon, and auto-casts each spell slot that has auto-cast on. Keys 1–4 (`ability_N` actions) cast manually, aiming at the mouse when `NeedsAim` is true. Shift+key or right-clicking a hotbar slot toggles auto-cast. Damage% applies to all abilities, AttackSpeed% only to the weapon's cooldown, and Range to all (melee gets half). Auto-targeting (`Combat.NearestEnemy`) skips enemies that are `IsDoomed`: a projectile cast at an enemy reserves its damage there (`Projectile.ReserveOn`) until it lands, so shots don't overkill. Hit side effects (slow, knockback) and crit odds are `HitEffects`, applied through `Combat.Hit` and `Combat.DamageArea`. Crit is rolled per enemy in `Combat.Hit` from the ability's `CritChance` (percent, plus the Crit Chance stat) and `CritMultiplier`; there's deliberately no global crit damage stat (see `Goals.md`).
- **Run state:** `RunState` is plain C#, owned by `Main`. It holds wave, gold, kills, owned items and `PlayerStats`. A scene reload discards it, so persistent "career" progress will need to live somewhere else, such as an autoload.
- **Stats:** `StatType` enum plus `PlayerStats` (summed bonuses; percent stats stored as whole numbers). `Player.ApplyStats` takes base values from the `ClassData` and layers the bonuses on top. Call `ApplyStats` again whenever stats change. `StatType` values are serialized as integers in `.tres` files: add new entries, but never renumber existing ones.
- **Items:** `ItemData` (a `[GlobalClass]` Resource holding `StatModifier` sub-resources), one `.tres` per item in `data/items/`. An item only shows up in the shop once it's listed in `data/ItemCatalog.tres`.
- **Waves:** `WaveSettings.For(wave)` is a formula giving duration, spawn rate and enemy health/damage/speed scaling. `EnemySpawner.Begin` takes the settings and `Enemy.ApplyWave` scales each spawn.
- **Enemies:** every type shares `Enemy.tscn`. An `EnemyData` Resource (`data/enemies/`) sets shape, stats, knockback resistance, spawn settings (`FirstWave`, `SpawnWeight`) and an optional ranged attack (`ShotRange > 0` makes the enemy keep its distance and shoot). The spawner sets `enemy.Data` before `AddChild` and picks types by weight from those unlocked for the wave. A type only spawns once it's listed in the Spawner's `EnemyTypes` in `Main.tscn`. Enemy shots reuse `Projectile` with `CollisionMask` set to world + player.
- **Gold:** enemies drop a `Coin` when they die (added deferred, because death happens inside a physics callback). Coins fly to the player once they're within `PickupRange`. Coins and projectiles are in `Groups.Transient` and get cleared at wave end. Before that, `Main` adds the floor coins' value to `RunState.Bag`, and each later pickup draws the same amount again from the bag (doubling it) until it's empty.
- Cross-entity lookups use **groups** (`Groups.Player`, `Groups.Enemies`), not node paths. Damage goes through `IDamageable`, and entities hand it to a child `Health` node that emits `Changed`/`Died`.
- `Arena` is the single source of truth for play bounds: the rect from (0,0) to `Size`. It builds its own wall collision in code.
- `EnemySpawner` places a `SpawnMarker` (a blinking X warning) first, then spawns the enemy as its own child. Spawn interval goes from `StartInterval` to `EndInterval` as `Progress` (0 to 1, set by `Main`) increases. `Clear()` removes everything.
- **Game feel:** `AreaFlash`, `FloatingText` (damage numbers, pickup cues) and `DeathPop` (`scenes/effects/`) are fire-and-forget visuals with a static `Spawn(...)` that adds them deferred and puts them in `Groups.Transient`. `Enemy.TakeDamage` spawns damage numbers and the hit pop; `EnemyData.ShowHealthBar` draws a bar once damaged. Screen shake is trauma-based: `ShakeCamera.Shake(tree, trauma)` (0–1). Abilities set it with the `ScreenShake` export, applied when an area lands or a projectile explodes.
- Projectiles and `AreaFlash` effects are added to `CurrentScene`, not the player, so they don't move with it. Add them deferred when spawning from a physics callback.
- **Physics layers** (named in project.godot): 1 world, 2 player, 3 enemies, 4 projectiles. Bitmask values are 1, 2, 4, 8. Enemies collide with walls and each other, not the player. Contact damage comes from the player's `Hurtbox` Area2D, with i-frames.
- Balance numbers are `[Export]` properties, editable in the Inspector or in the `.tscn` files.

## Godot C# conventions to follow

- Script classes must be `partial` and the class name must match the file name.
- Signals: `[Signal] public delegate void SomethingHappenedEventHandler(...)`; emit with `EmitSignal(SignalName.SomethingHappened, ...)`.
- `[Export]` fields only appear in the editor after a C# build.
- `.tscn`/`.tres` are text and can be edited directly, but keep `uid://` references and `ext_resource` ids consistent. Commit the `.uid` files Godot generates next to scripts/resources. `.godot/` is generated and ignored.

## Game design and project tracking

Read both of these at the start of a session:

- **`Goals.md`:** the original vision (a rough, non-binding brainstorm, so treat it as a baseline rather than a spec) plus a **"Decisions so far"** section. Once something is decided, record it there so it doesn't get debated again.
- **`ROADMAP.md`:** what's done (with commits), the suggested next steps, the backlog of ideas, open design questions, and known issues.

The game in brief: a fantasy arena-survivor (Brotato / Vampire Survivors style) with an RPG layer of classes, spells, equipment slots and potions.

**Workflow:** at the end of each feature, update `ROADMAP.md` (and the decisions in `Goals.md` if any were made) in the same commit as the feature.
