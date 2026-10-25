# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Working relationship

Claude acts as **game design and programming partner**. The user is a software architect with deep web, database, and .NET/C# experience but little game-design or Godot experience. That means:

- Explain Godot and game-dev concepts when they come up, mapping them to .NET/web ideas where that helps (scenes ≈ composable components/prefabs, signals ≈ C# events, autoloads ≈ singleton services, Resources ≈ serializable data objects/config).
- Don't over-explain general C#, architecture, or data-modeling topics.
- On design questions, give a recommendation with reasoning (genre conventions from Brotato / Vampire Survivors are a good reference), not just a list of options. Flag decisions that are expensive to reverse later.

## Project state

Playable prototype: mode select (Arcade / Career) → class select (Fighter, Mage, Warlock, Necromancer) → (career: talents screen) → pick 3 of the class's 5 spells → 5-wave run with gold drops and a between-wave shop of stat items. Career characters level up between runs and save to `user://career.json`. Shapes are placeholders drawn with `_Draw()`, no art.

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

`--autoplay` (after `--`) plays a whole run unattended through the real UI. It presses the mode-select (Arcade unless `--mode=career`), class-select, spell-select and shop buttons, circles the arena collecting coins, logs `[autoplay] event key=value` lines and quits when the run ends. A normal launch ignores it. Flags (full list in `scripts/debug/AutoplayOptions.cs`):

```bash
# Full 5-wave run in ~1-2s, uncapped. grep the log for results.
$GODOT --headless --path . --fixed-fps 60 --disable-vsync --quit-after 20000 -- --autoplay --class=mage --seed=1
#   --class=NAME|INDEX  --spells=A,B,C (picks, in slot order; default the list's first N)  --wave=N (start wave)  --seed=N (same seed = identical run, for before/after)
#   --god  --shop=cheapest|first|skip  --move=circle|still  --autocast (all spells, incl. manual ones)
#   --item="Rabbit's Foot" (repeatable; gear is equipped, class weapons work)  --stat=CritChance:20 (repeatable)
#   --buy-potion="Potion of Fury" (repeatable; buys one per shop visit before other buys; --item grants potions too)
#   --menu-delay=SECONDS (wait before each menu click, so Movie Maker records the shop)
#   --mode=arcade|career (default arcade)  --save=PATH (career save; default user://career_autoplay.json, never the real one)
#   --reset-career (delete that save first)  --runs=N (N runs in one launch via the real restart; run k uses seed+k)
#   --talents=spread|none  --heirloom=priciest|keep (career: how to spend points / what to keep after a victory)

# Career progression in one launch against a scratch save (or separate launches without --runs, to test reloading the file):
$GODOT --headless --path . --fixed-fps 60 --disable-vsync --quit-after 60000 -- --autoplay --mode=career --class=fighter --seed=1 --save=<scratch>/c.json --reset-career --runs=3
```

`--quit-after 20000` is a safety net: Autoplay is C#, so if the C# assembly fails to load (build missing, wrong .NET), nothing quits the game. `run_start` logs the .NET runtime version. Events: `run_start` (class, spells, gear), `wave_start`, `wave_end` (kills, gold, bag, hp, potions, reserve, buff potions), `buy` (with what it replaces), `potion` (source charge/bought/reserve, hp before, heal), `heart` (reserve after), `drink` (buff potion; Autoplay drinks one when 5+ enemies are alive and it isn't active), `run_end` (result, wave, kills, gold, gear, trinkets), `error`. Career adds `career` (save path, load status, level, xp, free points, talents, heirloom, record; logged on the talents screen), `talent` (each point spent), `heirloom` (gear kept after a victory) and `career_end` (the character once the run is banked). The `Career` autoload prints `[career] save PATH: fresh|loaded|migrated|corrupt|newer`. Never point `--save` at `user://career.json`. Bad flags log `error` and exit with code 1. To measure something new, add a temporary `GD.Print` where it happens, run with fixed seeds before and after the change, compare, then remove the print. If a test needs control Autoplay doesn't offer yet, add a flag to `AutoplayOptions` rather than a throwaway hook.

### Locally (user's Mac)

Everything above, plus looking at the game. Movie Maker writes every frame as a PNG, which works without screen-capture permission (combine with `--autoplay` to skip the menus):

```bash
$GODOT --path . --write-movie <scratch>/f.png --fixed-fps 20 --quit-after 1250 -- --autoplay --god --menu-delay=1
```

Read a few PNGs (or tile crops into a contact sheet with ffmpeg) rather than asking the user to check. At 20 fps, 1250 frames is about 60s of game time. Docker is installed for testing Linux setups (start Docker Desktop first).

### In the cloud / headless

The environment comes from `tools/cloud-setup.sh` (.NET SDK + Godot .NET binary as `godot`; tested on fresh Ubuntu 24.04, x86_64 and arm64, with net8.0 and net10.0). Run from the repo root, it reads the Godot and .NET versions from `ArenaShooter.csproj`. Its fallback defaults (used when it runs outside the repo) should match the `.csproj`. The cloud environment's network allowlist must include `github.com` (Godot), `dot.net` and `builds.dotnet.microsoft.com` (.NET), and `api.nuget.org` (the Godot SDK package, also needed at build time). Without them, setup fails with no clear error. In a fresh session:

```bash
export GODOT=godot                     # the setup script also puts this in ~/.bashrc
$GODOT --headless --path . --import    # once, if .godot/ is missing
dotnet build
```

Then `dotnet build`, the smoke test and Autoplay runs are the whole toolkit. There is no display or GPU, so no Movie Maker and no screenshots. Say so in the summary: visual changes are unverified until the user playtests. The user pulls the branch and playtests on the Mac.

## Architecture

- `scenes/<feature>/` holds a `.tscn` scene paired with its `.cs` script. `scripts/` holds shared, scene-less code. `data/` holds `.tres` Resource files (game content).
- `Main.cs` is the run controller and the only place signals get wired together. It emits `RunStarted`/`WaveStarted`/`WaveEnded`/`RunEnded`, and when launched with `--autoplay` it adds the `Autoplay` debug driver (`scripts/debug/`), which uses each screen's exposed buttons (`ModeSelect`/`ClassSelect.Choices`, `CareerScreen.TalentButtons`, `Shop.Offers`/`ContinueButton`, `CareerSummary.HeirloomChoices`) to press real buttons. Its flow is ModeSelect → ClassSelect (Back returns to ModeSelect) → [career: CareerScreen] → SpellSelect (Back returns one step) → Wave → Shop → Wave … → Victory or Defeat (arcade: HUD message; career: `CareerSummary`). The tree is paused whenever a wave isn't running. The UI layers (`ModeSelect`, `ClassSelect`, `CareerScreen`, `SpellSelect`, `Shop`, `CareerSummary`, `Hud`) are `process_mode = Always`, so they still work while paused. Restart (R, or Continue on the career summary) reloads the scene; the mode survives in the `Career` autoload, so it lands on class select in the same mode.
- **Classes:** `ClassData` Resource (`data/classes/`) holding base max HP, move speed and armor, a `StartingWeapon` item plus the other `Weapons` only this class can buy, the class's full spell list, and `SpellPicks` (how many the player picks, max 4). `SpellSelect` shows the list; pick order is slot order. `Player.Setup(classData, pickedSpells)` must run before `ApplyStats`. Classes appear on the select screen only once they're listed in `Main.tscn`'s `Classes` array.
- **Abilities:** each class's weapon is its always-on basic attack; spells are stronger attacks on longer cooldowns. Both are `AbilityData` Resources (`data/weapons/`, `data/spells/`). Each subclass holds both the tuning values and the behavior: `ProjectileAbility` (optionally explodes), `AreaAbility` (around the caster or at a target point; target-point auto-casts aim at the densest crowd, and `Delay` > 0 telegraphs the spot with a `DelayedBlast` first, like Meteor), `MeleeArcAbility`, `ChainAbility` (instant hit that jumps between enemies), `ChargeAbility` (ground dash via `Player.Leap(..., airborne: false)` that hits along its path), `BuffAbility` (an `AreaAbility` shove plus timed stat modifiers on the caster; optional `HealthCost`, like Blood Pact), `DrainAbility` (a `DrainBeam` tether that ticks damage and retargets), `SummonAbility` (spawns minions). A new ability with existing behavior is a new `.tres`; a new kind of behavior is a new subclass that overrides `TryAutoTarget` and `Cast`. `LeapAbility` moves the player via `Player.Leap` (untouchable while airborne), then blasts where it lands. Abilities that move the player should set `AutoCastByDefault = false`. Resources are shared, so runtime state (cooldown, auto-cast on/off) lives in `AbilitySlot`.
- **`AbilityCaster`** (child of Player) always auto-fires the weapon, and auto-casts each spell slot that has auto-cast on. Keys 1–4 (`ability_N` actions) cast manually, aiming at the mouse when `NeedsAim` is true. Shift+key or right-clicking a hotbar slot toggles auto-cast. Damage% applies to all abilities, AttackSpeed% only to the weapon's cooldown, and Range to all (melee gets half). Auto-targeting (`Combat.NearestEnemy`) skips enemies that are `IsDoomed`: a projectile cast at an enemy reserves its damage there (`Projectile.ReserveOn`) until it lands, so shots don't overkill. Hit side effects (slow, knockback, DoT, raise-on-death mark) and crit odds are `HitEffects`, applied through `Combat.Hit` and `Combat.DamageArea`; the matching `AbilityData` exports are in its On Hit / On Kill groups. DoTs tick on the `Enemy` (strongest wins, every 0.5s, tinted numbers). Crit is rolled per enemy in `Combat.Hit` from the ability's `CritChance` (percent, plus the Crit Chance stat) and `CritMultiplier`; there's deliberately no global crit damage stat (see `Goals.md`).
- **Life steal:** every player hit ends in `Combat.Deal`, which heals `Player.StealLife` for `HitEffects.LifeSteal` (ability's `LifeSteal` % + the Life Steal stat) of the damage that landed (no overkill). `StealLife` is capped at `Player.LifeStealCap` HP/s (token bucket) and batches the "+N" text. DoT ticks steal too; minion hits don't.
- **Minions:** `Minion` (`scenes/minions/`, built in code like pickups) driven by a `MinionData` Resource (`data/minions/`). Spawned by `Minion.Spawn` (deferred) from `SummonAbility` or from an enemy that dies while marked (`HitEffects.RaiseMinion`, `Enemy.MarkForRaising`). Chases the nearest enemy within a leash of the player, else follows the player. Damage = `MinionData.Damage` × `AbilityContext.MinionDamageMultiplier` (Damage% + Minion Damage%), read live from `Player.Abilities.Context`. Cap per type (`MaxAlive`, oldest crumbles), `Lifetime`, and HP lost to enemy contact (distance check, i-frames). Enemies don't target minions, and enemy shots pass through them. In `Groups.Minions` + `Groups.Transient`, so they clear at wave end. Physics layer 5 (minions): collide with walls and each other only.
- **Run state:** `RunState` is plain C#, owned by `Main`. It holds wave, gold, kills, owned items and `PlayerStats`. A scene reload discards it; anything that must outlive a run lives in the `Career` autoload.
- **Career (`scripts/career/`):** `Career` is an autoload (`project.godot` `[autoload]`, reached via `Career.Instance`) holding the session's `GameMode` and the save. The save (`CareerSave`: one `CareerCharacter` per class `Id`) loads lazily on first career use, so arcade never touches it, and `Persist()` writes it after every change. `CareerStore` does the JSON I/O: temp file then replace; a missing file starts fresh; an unreadable or newer-version file is moved aside (`.corrupt-…`/`.newer-…`) and a fresh career starts. The save stores facts only (total XP, talent ranks, heirloom item id, record); levels and points derive from `CareerSettings` (`data/career/CareerSettings.tres`: XP rewards, level curve, `CareerTalent` list), so retuning needs no migration. Unknown JSON fields are kept (`[JsonExtensionData]`). Characters are keyed by `ClassData.Id` and the heirloom is an `ItemData.Id` (consumables inherit it); talents by `CareerTalent.Id`. `Main._Ready` calls `Career.ValidateIds`, which logs an error for an empty or duplicate `Id`, so give every new class/item a unique one. Never change an `Id` once shipped; `.tres` file names are free to change. Bump `CareerSave.CurrentVersion` only for breaking changes, with a step in `CareerStore.Migrate`. A career run gets talents via `RunState.GrantStat` and the heirloom via `AddItem` (`Main.ApplyCareer`); `Main.EndCareerRun` banks XP and saves before showing `CareerSummary`.
- **Stats:** `StatType` enum plus `PlayerStats` (summed bonuses; percent stats stored as whole numbers). `Player.ApplyStats(runStats)` takes base values from the `ClassData` and layers the bonuses on top. Call `ApplyStats` again whenever run stats change. Timed bonuses live in `Player.Buffs` (`ActiveBuffs`): `Player.AddBuff` adds one, and the player adds them into a copy of the run stats, re-applying when one starts or expires. Buffs clear at each wave start. `StatType` values are serialized as integers in `.tres` files: add new entries, but never renumber existing ones.
- **Items:** `ItemData` (a `[GlobalClass]` Resource holding `StatModifier` sub-resources), one `.tres` per item in `data/items/`. An item only shows up in the shop once it's listed in `data/ItemCatalog.tres` (class weapons: in the class's `Weapons` instead). An item with `Slot = None` is a stacking trinket (`RunState.Trinkets`); any other `EquipSlot` makes it gear, worn in `RunState.Equipment` (6 positions in `Equipment.Layout`, Ring twice). `Equipment.TargetsFor`/`Displaced`/`Equip` hold the swap rules (two-handers, starting-weapon fallback); the shop refunds `SellValue` for what was replaced. A main-hand item's `Weapon` (an `AbilityData` in `data/weapons/`) is the basic attack: `Main.ApplyLoadout` calls `Player.SetWeapon` then `ApplyStats` after every purchase. `RunState.Stats` is recalculated from gear, trinkets and grants, never edited directly (use `GrantStat`). Consumables are `ConsumableData : ItemData` (`data/consumables/`, listed in the catalog's `Consumables`): `RunState.AddItem` puts them in `RunState.Potions`, and the shop sells them on a fixed potion shelf (not rerolled) up to `MaxCarry`.
- **Waves:** `WaveSettings.For(wave)` is a formula giving duration, spawn rate and enemy health/damage/speed scaling. `EnemySpawner.Begin` takes the settings and `Enemy.ApplyWave` scales each spawn.
- **Enemies:** every type shares `Enemy.tscn`. An `EnemyData` Resource (`data/enemies/`) sets shape, stats, knockback resistance, spawn settings (`FirstWave`, `SpawnWeight`) and an optional ranged attack (`ShotRange > 0` makes the enemy keep its distance and shoot). The spawner sets `enemy.Data` before `AddChild` and picks types by weight from those unlocked for the wave. A type only spawns once it's listed in the Spawner's `EnemyTypes` in `Main.tscn`. Enemy shots reuse `Projectile` with `CollisionMask` set to world + player.
- **Pickups:** `Coin` and `Heart` extend `Pickup` (`scenes/pickups/`), which flies to the player once they're within `PickupRange` and calls `Collect()`. Override `Wanted` to make a pickup wait (an attracted pickup that stops being wanted drops where it is). `Heart` fills the potion reserve (`Player.FillReserve`) and waits while it's full. Enemies drop a heart with `EnemyData.HeartDropChance`. `Player.Heal` shows the green "+N".
- **Potions:** `PotionBelt` (plain C#, `RunState.Potions`, handed to `Player.Setup`) holds free charges (`PotionsPerWave`, refilled by `Main.StartWave` → `RefillPotions`), bought consumables, and the heart-filled reserve (capacity = `Player.PotionHeal`, kept between waves). `Player.UsePotion` (Space, `use_potion`) skips at full HP, else drinks a full potion (free charge, then bought) for `PotionHeal`, else the whole reserve; `Player.NextPotion` says which. Buff potions (`PotionBelt.Hotbar`, catalog order) are keys 5, 6… (`consumable_N` actions) and go through `Player.AddBuff`. `PotionHeal` = `BasePotionHeal` + the Potion Heal stat. `Hud.BindPotions` adds the slots (`PotionSlotView`: Health, Reserve, Buff) after the spell bar.
- **Gold:** enemies drop a `Coin` when they die (added deferred, because death happens inside a physics callback). Pickups and projectiles are in `Groups.Transient` and get cleared at wave end. Before that, `Main` adds the floor coins' value to `RunState.Bag`, and each later pickup draws the same amount again from the bag (doubling it) until it's empty.
- Cross-entity lookups use **groups** (`Groups.Player`, `Groups.Enemies`), not node paths. Damage goes through `IDamageable`, and entities hand it to a child `Health` node that emits `Changed`/`Died`.
- `Arena` is the single source of truth for play bounds: the rect from (0,0) to `Size`. It builds its own wall collision in code.
- `EnemySpawner` places a `SpawnMarker` (a blinking X warning) first, then spawns the enemy as its own child. Spawn interval goes from `StartInterval` to `EndInterval` as `Progress` (0 to 1, set by `Main`) increases. `Clear()` removes everything.
- **Game feel:** `AreaFlash`, `LineFlash` (charge paths, jagged lightning), `FloatingText` (damage numbers, pickup cues) and `DeathPop` (`scenes/effects/`) are fire-and-forget visuals with a static `Spawn(...)` that adds them deferred and puts them in `Groups.Transient`. `Enemy.TakeDamage` spawns damage numbers and the hit pop; `EnemyData.ShowHealthBar` draws a bar once damaged. Screen shake is trauma-based: `ShakeCamera.Shake(tree, trauma)` (0–1). Abilities set it with the `ScreenShake` export, applied when an area lands or a projectile explodes.
- Projectiles and `AreaFlash` effects are added to `CurrentScene`, not the player, so they don't move with it. Add them deferred when spawning from a physics callback.
- **Physics layers** (named in project.godot): 1 world, 2 player, 3 enemies, 4 projectiles, 5 minions. Bitmask values are 1, 2, 4, 8, 16. Enemies collide with walls and each other, not the player. Contact damage comes from the player's `Hurtbox` Area2D, with i-frames.
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
