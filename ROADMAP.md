# Roadmap

Where the project stands and what's next. `Goals.md` holds the vision and the design decisions; this file tracks progress. Update both at the end of each feature.

## Done

| Milestone | Commit |
|---|---|
| Core loop: arena, WASD movement, auto-aim weapon, telegraphed spawns, 60s round | `d493576` |
| Run loop: 5 waves, gold drops + pickup range, between-wave shop with 12 stat items and rerolls | `721b6d7` |
| Camera locked to player; window opens at 2560×1440 (viewport stays 1280×720) | `39d7da7` |
| Classes + ability system: Fighter and Mage, weapon = always-on basic attack, spells in slots 1–4 with auto/manual cast, class select screen | `36cb37d` |
| Enemy variety: Grunt, Runner, Archer (ranged), Brute; per-wave weighted spawn mix | `6ffd010` |
| Ground Slam is a real leap; abilities can default to manual cast | `c1bc250` |
| Game feel: floating damage numbers (red on the player), enemy hit pop, Wand knockback, per-ability screen shake + shake when hurt, death pop, Brute health bars | `da42f55` |
| No overkill: projectiles reserve their damage on the target, and auto-targeting skips enemies already doomed by shots in flight (wasted Fireballs ~1 in 6 → 0 on wave 1, wasted Wand bolts ~16% → 0) | `18dcf05` |
| Crits: per-ability crit chance + multiplier, global Crit Chance stat, yellow "N!" crit numbers, 3 crit items (Rabbit's Foot, Assassin's Mark, Duelist's Gloves) | `cf1b975` |
| Gold bag: floor gold at wave end goes into a bag; later pickups pay double from it (Brotato/Pickle Pete style), "x2" cue, bag shown in HUD and shop | `560880e` |
| Testing infrastructure: `--autoplay` debug mode (plays full runs through the real UI, seeded, logs events), cloud setup script `tools/cloud-setup.sh` (tested in Docker, x86_64 + arm64), local vs cloud testing notes in CLAUDE.md | `1473151` |
| Moved to .NET 10 (LTS): editor and `dotnet build` clean, 12/12 seeded Autoplay runs identical to .NET 8, cloud script verified with it | `1473151` |
| Spell selection: each class has 5 spells and picks 3 on a new screen after class select (pick order = hotkeys). New spells: Ice Bolt, Chain Lightning, Meteor (Mage); Charge, Shield Wall, War Cry (Fighter). Timed stat buffs (`ActiveBuffs`), delayed telegraphed blasts, area spells aim at the densest crowd, Autoplay `--spells` | _this commit_ |

**Playtest status:** the user has beaten wave 5 with both classes and finds it fun so far. Balance hasn't been tuned on purpose yet.

## Next up (suggested order)

1. **Equipment slots:** main hand (weapon = basic attack), off hand (shield, tome, orb), armor, rings, trinkets. Weapons become items you can buy or swap. Two-handed weapons take both hand slots and are stronger.
2. **Warlock and Necromancer:** Warlock needs life steal / drain (heal-on-damage hook, a LifeSteal stat). Necromancer needs a minion system (allied units with their own AI that target enemies).
3. **Consumables:** potions bought in the shop or dropped by enemies, used from hotkeys 5+. Timed potion effects (e.g. a damage potion) can reuse `ActiveBuffs`.
4. **Placeholder sprites:** replace the `_Draw()` shapes with simple SVG sprites Claude authors (Mage, Fighter, the 4 enemies), starting with a Mage + Grunt sample to judge the look. Code side: an optional `Texture2D` on `ClassData`/`EnemyData`, drawn via `Sprite2D`, falling back to `_Draw()` when unset. Decide the art style (pixel vs vector, sprite size, view angle) first, since changing it later means redoing every asset. Kenney CC0 packs or commissioned art can swap in later through the same fields.
5. **Progression modes:** "career" (persistent character growth between runs) vs "arcade" (fresh each run). Needs save data in an autoload or file, since `RunState` is thrown away on restart.

## Backlog (ideas, not yet scheduled)

- Element tags (fire, frost) that items and spells can scale with. Fire burns (damage over time), frost slows. Weapon × element = build variety.
- Item rarity tiers and class-specific shop items.
- Fill spell slot 4 during the run: e.g. a spell offer in the shop after wave 2 or 3 (the "mix" answer to when spells get chosen). Also grow each class list toward 6–8 spells.
- Spell upgrades in the shop (Fireball +radius, Chain Lightning +1 jump), tying spells into the item economy.
- Selling items; item drops during waves.
- Vacuum pickup (Vampire Survivors): a rare drop that pulls all gold on screen to the player.
- Hand-authored waves (`WaveData` Resources) replacing the `WaveSettings` formula; an elite or boss on the final wave.
- More stats: cooldown reduction, life steal, crit, HP regen, dodge.
- Pause menu (Esc), settings, fullscreen toggle, "retry same class" on the end screen.
- Sound, music, an art pass (replace the `_Draw()` placeholder shapes).

## Open design questions

- Career vs arcade progression: one, the other, or both? What carries over between runs?
- Shop only, or shop plus drops? Can items be sold?

## Known issues and tuning notes

- Autoplay without `--god` dies by wave 2 with either class (it circles and doesn't dodge). It's a bot, but it may also hint that the early waves are tough for new players.

- The HUD gold count behind the shop doesn't update on purchase. It's hidden by the shop and refreshed when the next wave starts.
- Restart (R) always goes back to class select.
- Difficulty in waves 4–5 hasn't been tuned since the new enemy types were added.
- New spell numbers are first guesses, set by comparing with the existing four, not by playtest. War Cry (+40% Damage and Attack Speed for 6s, 15s cooldown) and Meteor (40 damage) are the likeliest to be too strong. Charge's drawn lane is narrower than its hit width (40 each side).
- Auto-cast Charge picks a direction toward the nearest enemy and dashes the full range, which can carry you into a crowd. It's manual by default for that reason.
- Crit item values are first guesses: crit chance is worth less per point than Damage% (+10% crit at ×1.5 ≈ +5% damage), so the items give bigger numbers.
- Feel knobs not tuned by playtest yet: `ScreenShake` per ability (Feel group), `Player.HurtShake`, `ShakeCamera.MaxOffset`, Wand knockback 40. Damage numbers stay frozen on screen behind the defeat message.
- Enemies can pile up on top of each other. Fine for now; a separation behavior would help once there's art.
