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

**Playtest status:** the user has beaten wave 5 with both classes and finds it fun so far. Balance hasn't been tuned on purpose yet.

## Next up (suggested order)

1. **Game feel:** floating damage numbers, hit pop or small knockback on weapon hits, screen shake on big spells, a death pop effect, health bars on Brutes. Cheap and makes a big difference to how the game feels.
2. **Spell selection at run start:** each class gets a larger spell list (6–8), and the player picks 3–4 into slots 1–4 before wave 1. Needs more spells per class: Ice Bolt, Chain Lightning, Meteor (Mage); Charge, Shield Wall, War Cry buff (Fighter). A temporary-buff spell needs a timed stat modifier system.
3. **Equipment slots:** main hand (weapon = basic attack), off hand (shield, tome, orb), armor, rings, trinkets. Weapons become items you can buy or swap. Two-handed weapons take both hand slots and are stronger.
4. **Warlock and Necromancer:** Warlock needs life steal / drain (heal-on-damage hook, a LifeSteal stat). Necromancer needs a minion system (allied units with their own AI that target enemies).
5. **Consumables:** potions bought in the shop or dropped by enemies, used from hotkeys 5+.
6. **Progression modes:** "career" (persistent character growth between runs) vs "arcade" (fresh each run). Needs save data in an autoload or file, since `RunState` is thrown away on restart.

## Backlog (ideas, not yet scheduled)

- Element tags (fire, frost) that items and spells can scale with. Fire burns (damage over time), frost slows. Weapon × element = build variety.
- Item rarity tiers and class-specific shop items.
- Selling items; item drops during waves.
- Brotato-style "bag" for gold left uncollected at wave end (bonus later) instead of losing it.
- Hand-authored waves (`WaveData` Resources) replacing the `WaveSettings` formula; an elite or boss on the final wave.
- More stats: cooldown reduction, life steal, crit, HP regen, dodge.
- Pause menu (Esc), settings, fullscreen toggle, "retry same class" on the end screen.
- Sound, music, an art pass (replace the `_Draw()` placeholder shapes).

## Open design questions

- Career vs arcade progression: one, the other, or both? What carries over between runs?
- When do spells get chosen: all at run start, unlocked during the run, or a mix?
- Shop only, or shop plus drops? Can items be sold?
- Is losing uncollected gold fun, or does it need a softer rule?

## Known issues and tuning notes

- The HUD gold count behind the shop doesn't update on purchase. It's hidden by the shop and refreshed when the next wave starts.
- Restart (R) always goes back to class select.
- Difficulty in waves 4–5 hasn't been tuned since the new enemy types were added.
- Enemies can pile up on top of each other. Fine for now; a separation behavior would help once there's art.
