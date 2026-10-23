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
| Spell selection: each class has 5 spells and picks 3 on a new screen after class select (pick order = hotkeys). New spells: Ice Bolt, Chain Lightning, Meteor (Mage); Charge, Shield Wall, War Cry (Fighter). Timed stat buffs (`ActiveBuffs`), delayed telegraphed blasts, area spells aim at the densest crowd, Autoplay `--spells` | `f29e9a8` |
| Healing: enemies sometimes drop a heart (+2 HP, 3% chance, Brutes 15%) that waits on the floor until you're hurt; a health potion on Space (+10 HP, once per wave) with a hotbar slot. Coins and hearts share a `Pickup` base. Autoplay drinks the potion when it would heal fully or HP is under 35% | `f9fac1c` |
| Equipment slots: Main Hand, Off Hand, Armor, Ring ×2, Amulet, next to stackable trinkets. Weapons are main-hand items, so the basic attack changes with what you buy. New weapons Spear, Greatsword (2H) for the Fighter and Frost Scepter, Staff (2H) for the Mage. 13 new gear items, 5 existing items moved into slots. Replaced gear refunds half its price. The shop shows your equipment, what a purchase replaces, and a choice when both rings are full. Autoplay `--menu-delay` for recording menus | `1ab7ebf` |
| Shop equipment panel: hovering a worn item or trinket shows its stats in a tooltip styled like the panels (`UiStyles.TooltipTheme`) | `110e08f` |
| Warlock and Necromancer: 2 new classes with 5 spells and 3 weapons each. Life steal (per-ability % + Life Steal stat, capped 5 HP/s), damage over time, HP-cost buffs, a draining beam (`DrainAbility`), a minion system (`Minion`/`MinionData`, `SummonAbility`, raise-on-death marks), Minion Damage stat, 6 new shop items. Class select fits 4 cards | _this commit_ |

**Playtest status:** the user has beaten wave 5 with both classes and finds it fun so far. Balance hasn't been tuned on purpose yet.

## Next up (suggested order)

1. **More consumables:** the health potion (Space, once per wave) is in. Next: other potions bought in the shop (e.g. a damage potion reusing `ActiveBuffs`), and buying extra potion charges or a bigger heal.
2. **Placeholder sprites:** replace the `_Draw()` shapes with simple SVG sprites Claude authors (Mage, Fighter, the 4 enemies), starting with a Mage + Grunt sample to judge the look. Code side: an optional `Texture2D` on `ClassData`/`EnemyData`/`MinionData`, drawn via `Sprite2D`, falling back to `_Draw()` when unset. Decide the art style (pixel vs vector, sprite size, view angle) first, since changing it later means redoing every asset. Kenney CC0 packs or commissioned art can swap in later through the same fields.
3. **Progression modes:** "career" (persistent character growth between runs) vs "arcade" (fresh each run). Needs save data in an autoload or file, since `RunState` is thrown away on restart.

## Backlog (ideas, not yet scheduled)

- Element tags (fire, frost) that items and spells can scale with. Fire burns (damage over time), frost slows. Weapon × element = build variety.
- Item rarity tiers and class-specific shop items (Minion Damage items are dead offers for non-Necromancers today).
- Fill spell slot 4 during the run: e.g. a spell offer in the shop after wave 2 or 3 (the "mix" answer to when spells get chosen). Also grow each class list toward 6–8 spells.
- Spell upgrades in the shop (Fireball +radius, Chain Lightning +1 jump), tying spells into the item economy.
- Selling items outright (replacing gear already refunds half); item drops during waves.
- Show the equipped weapon on the HUD; a gear screen reachable mid-run (Tab) rather than only in the shop.
- Shared (classless) weapons in the catalog; weapon-specific bonuses per class (Brotato style).
- Vacuum pickup (Vampire Survivors): a rare drop that pulls all gold on screen to the player.
- Hand-authored waves (`WaveData` Resources) replacing the `WaveSettings` formula; an elite or boss on the final wave.
- More stats: cooldown reduction, HP regen, dodge, minion count/HP/duration.
- Minion upgrades (more skeletons, longer lifetime) and enemies that target minions; a Necromancer item that raises from every kill.
- Pause menu (Esc), settings, fullscreen toggle, "retry same class" on the end screen.
- Sound, music, an art pass (replace the `_Draw()` placeholder shapes).

## Open design questions

- Career vs arcade progression: one, the other, or both? What carries over between runs?
- Shop only, or shop plus drops? Can items be sold outright (beyond the half refund when gear is replaced)?

## Known issues and tuning notes

- Autoplay without `--god` dies by wave 2 with either class (it circles and doesn't dodge). It's a bot, but it may also hint that the early waves are tough for new players.

- The HUD gold count behind the shop doesn't update on purchase. It's hidden by the shop and refreshed when the next wave starts.
- Restart (R) always goes back to class select.
- Difficulty in waves 4–5 hasn't been tuned since the new enemy types were added.
- Healing numbers are first guesses: heart +2 HP at 3% (Brutes 15%), potion +10 once per wave. A full run drops roughly 5–8 hearts. +10 is most of the Mage's 16 HP, so the potion may be strong for the Mage and weak for the Fighter (30 HP); a percent-of-max heal is an option.
- New spell numbers are first guesses, set by comparing with the existing four, not by playtest. War Cry (+40% Damage and Attack Speed for 6s, 15s cooldown) and Meteor (40 damage) are the likeliest to be too strong. Charge's drawn lane is narrower than its hit width (40 each side).
- Auto-cast Charge picks a direction toward the nearest enemy and dashes the full range, which can carry you into a crowd. It's manual by default for that reason.
- Crit item values are first guesses: crit chance is worth less per point than Damage% (+10% crit at ×1.5 ≈ +5% damage), so the items give bigger numbers.
- Feel knobs not tuned by playtest yet: `ScreenShake` per ability (Feel group), `Player.HurtShake`, `ShakeCamera.MaxOffset`, Wand knockback 40. Damage numbers stay frozen on screen behind the defeat message.
- Gear and new weapon numbers are first guesses. With seed 2, no shop and `--god`, kills over a full run were: Sword 182, Spear 229, Greatsword 223, Wand 235, Frost Scepter 241, Staff 253. The upgrades beat the starters, but Spear and Greatsword are close, though the Greatsword also costs the off hand.
- Autoplay's `cheapest` shop strategy happily buys a weapon or armor that's worse than what it wears. That's fine for a test bot, but don't read balance into which gear it ends up with.
- Warlock/Necromancer numbers are first guesses. God-mode, no-shop runs (seed 2), total damage dealt over 5 waves: Fighter ~3.5k, Mage 4.1k–6.5k by spell pick, Warlock 4.2k–5.4k, Necromancer 4.0k–5.9k (minions 0.7k–3.3k of it; Bone Golem alone ~2.1k). Without `--god` over seeds 1–5, Autoplay's average death wave: Fighter 2.2, Necromancer 2.4, Mage 2.8, Warlock 3.0 (3.6 with Crimson Nova + Blood Pact + Drain Life, ~100 HP stolen per run). The Warlock is the most survivable, which fits it, but watch it.
- Necromancer runs with both Bone Golem and Grave Curse aren't exactly reproducible with the same `--seed` (kills vary ~±3%). Every other class and spell pair tested was. Cause not found; it isn't minion-minion collisions (ruled out). Use other spell picks for before/after comparisons.
- Minions don't scale with waves (fixed HP, damage only from the player's stats). Skeletons (14 HP) die in 2–3 contact hits by wave 4.
- Enemies can pile up on top of each other. Fine for now; a separation behavior would help once there's art.
