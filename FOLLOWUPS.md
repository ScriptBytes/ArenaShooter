# Follow-ups for review

Open questions, judgment calls and concerns from the unattended batch run (Warlock/Necromancer, consumables + buffer potion, career vs arcade). Each task appends its own section. Answer inline or in chat; resolved items move to `Goals.md` decisions or `ROADMAP.md`.

## 1. Warlock and Necromancer

**Judgment calls to revisit**

- Life steal is a % of damage dealt (D3/PoE style), not Brotato's "% chance to heal 1 HP". Easier to read with our small HP pools. Capped at 5 HP/s from all sources; excess is lost.
- Life steal items (Vampiric Fang, Bloodstone Ring, Blood-Soaked Robes) are in the shared catalog, so any class can build sustain. Minion Damage items (Bone Charm, Book of the Dead, Phylactery) are too, so they're dead offers for non-Necromancers until class-specific shop items exist.
- Minion Damage is a separate stat that adds to Damage% for minions. There's no minion count/HP/duration stat yet.
- Enemies ignore minions (they keep chasing you) and enemy shots pass through them. Minions still lose HP on contact and expire after 18s (skeletons) or 25s (golem). Enemies that target minions would make them real tanks, but it means new enemy AI.
- Raise-on-death applies to Grave Curse (100%), Reaper's Sickle (30%) and Skull Staff (15%). Raised skeletons share the summon cap of 6.
- Blood Pact costs 4 HP but can't kill you. Its auto-cast waits until you'd stay above half HP.
- Warlock color is pink (blood); its spells are crimson, sickly green and shadow purple. The Necromancer is teal with bone-white minions. Class select cards shrank from 320 to 270px wide to fit 4.

**Balance concerns**

- Warlock is the most survivable class in Autoplay (average death wave 3.0, or 3.6 with a sustain spell pick, vs Mage 2.8 and Fighter 2.2). Its damage is in Mage range.
- Bone Golem is the strongest single Necromancer spell (~2.1k damage per run on its own). It may need a shorter lifetime or less splash.
- Necromancer survival is the weakest of the new classes (2.4 average death wave). Skeletons die fast in later waves because minions don't scale with waves.
- Curse of Agony (30 damage per enemy over 6s, area) is strong against crowds, and DoTs ignore the "no overkill" reservations.

**Unverified**

- Feel: drain beam, curse tint and DoT numbers, minion movement in a crowd. Checked in Movie Maker frames only, not hands-on. Manual aiming of Drain Life (it latches onto the enemy nearest the mouse) is untested.
- Same-seed Necromancer runs with Bone Golem + Grave Curse vary slightly (cause unknown, see ROADMAP known issues).

**Questions**

- ~~Should enemies attack minions?~~ **Resolved (§5):** Ans: Le't make minons have an "targetable" toggle or something like that, so we can test out different minions being targetted or not. Turn it on for now.
- ~~Should life steal stay available to every class?~~ **Resolved (§5):** Ans: Life steal itself should be a warlock spell.
- Necromancer pet count: is 6 skeletons + 1 golem the right amount of screen clutter once real sprites arrive? Ans: Not sure, TBD

## 2. Consumables and buffer potion

**Judgment calls to revisit**

- The heart "buffer" is called the **Reserve** in game. Capacity = one potion's heal (10, +Potion Heal). It keeps between waves and never turns into a regular charge; a full reserve is simply a second full potion.
- A heart bigger than the room left tops the reserve up and the overflow is lost (only matters if heart values change; today 2 HP into a 10 HP reserve). A heart already flying to you drops back to the floor if another one fills the reserve first.
- Hearts fill the reserve even at full HP (they no longer care about HP). Floor hearts are still cleared at wave end.
- Space order: free charge → bought Health Potion → reserve. The reserve is always drunk whole, even if you're only missing 1 HP. Space does nothing at full HP.
- Consumables sit on a fixed potion shelf below the 4 random offers instead of competing for offer slots (Brotato has no consumables to copy here). Carry limits: Health Potion 2, buff potions 3. `--item` grants ignore the limit.
- Buff potions are bound to keys 5 and 6 in catalog order; the HUD always shows their slots (x0 when empty) so keys don't shift. A 3rd buff potion would take key 7 (needs a new `consumable_3` input action).
- Drinking a buff potion that's already active refreshes its timer (wastes the rest), matching the spell-buff rule. Buffs still clear at wave start, so drinking in the last seconds is wasted.
- "Bigger heal" is a stat (Potion Heal) on a trinket (Alchemist's Flask +4, 14 gold), not a consumable. It also grows the reserve, so the Flask makes hearts worth less relative to the reserve but the reserve holds more.

**Balance concerns**

- Health Potion at 10 gold is a strong buy for the Mage (+10 of 16 HP) and weak for the Fighter; same concern as the free potion. A percent-of-max heal would fix both.
- Fury (+30% Damage for 10s, 8 gold) is cheap compared with permanent items; with 3 carried and a 30–60s wave, it's +5–10% damage for the wave. Swiftness's value depends on how much dodging matters.
- Hearts now only feed the reserve, so a player who never presses Space gets nothing from them. Survival in Autoplay barely moved (see ROADMAP known issues).

**Unverified**

- Feel of pressing Space with an empty potion but a partial reserve (you get a small heal you might not expect). HUD readability of the reserve flask at a glance, the shop potion shelf's uneven card widths, and the "+2" pink pickup text: checked in Movie Maker frames only.
- Keys 5/6 tested only through Autoplay's input actions, not a real keyboard.

**Questions**

- ~~Full reserve / excess hearts?~~ **Resolved (§5):** Ans: if the player is <100% health, hearts should always heal the player. Hearts only go into the buffer if the player is full health
- ~~Consumable drops?~~ **Resolved (Goals.md):** Ans: I think for now, shop only.
- ~~Space reserve-first when full?~~ **Resolved (§5):** Ans: yes, if it's full, use the reserve

## 3. Career vs arcade

**Questions (my recommendation in bold; answer "yes" to keep it)**

1. Shape of progression: per-class characters with XP → levels → talent points spent on permanent stats, plus one heirloom gear piece. **Keep.** It's the Brotato/VS "meta upgrades" idea with an RPG face. Alternative: a shared account-wide currency (VS gold) spent in one shop for all classes.
2. One character per class, one save file, no save slots or multiple characters per class. **Keep for now**; the save is a dictionary keyed by class id, so slots can be added later as a wrapper.
3. XP: 1/kill, 20/wave cleared, +50 victory, earned on defeat too. **Keep**: a loss still feels like progress. Kill XP rewards classes that kill more (Necromancer > Fighter today), which may need per-wave XP instead.
4. Level curve 60, +40 per level, cap 25 (24 points for 33 ranks). About 3–4 wins per level by level 10, ~30 wins to cap. **Probably too slow late**; playtest a few runs first.
5. Talent reset is free and unlimited. **Keep** (talents are small, and experimenting is the fun part). A gold cost needs a persistent currency, which we don't have.
6. Heirloom only on victory, one per character, any worn gear except the starting weapon, replaces the previous one. **Keep**. Open: should the heirloom be upgradable (+1 tier per victory it's carried, e.g. +25% to its modifiers)? This is the natural home for "incrementally upgrading gear" from the brief, but needs rarity tiers first.
7. Gear "stash" (bank several items, pick a loadout) was **not built**: a full starting loadout at wave 1 breaks the shop economy faster than talents do. Recommend: no stash; grow the heirloom instead.
8. Career runs use the same 5 waves and difficulty as arcade. **Change later**: add difficulty tiers ("Danger 1–5" in Brotato) unlocked per class by career victories, each +X% enemy health/damage and +XP. That's the main lever against the power creep below.
9. Arcade characters get no career bonuses and nothing is recorded (no arcade high scores). **Keep**; an arcade best-wave/kills board could come later.
10. Quitting mid-run banks nothing (no XP for a run in progress). **Keep**, matches Brotato. There's no in-game abandon button yet (no pause menu).
11. Mode is remembered for the session, not saved: launching the game always starts on mode select. **Keep.**

**Balance concerns**

- Talents only add power. At the cap a character can have e.g. +10 Max HP (+62% for the Mage's 16), +15% Damage, +15% Attack Speed, +3 Armor. Armor is flat reduction, so +3 (on the Fighter's 1) turns a wave-1 Runner's 3-damage hit into 1 and a Grunt's 5 into 1. Iron Skin may need fewer ranks or a percent-based armor model.
- Toughness is far stronger for low-HP classes (Mage, Necromancer) than the Fighter. Percent-of-base HP would even it out.
- Magnetism and Fleet Foot are weak next to Might/Haste; expect everyone to buy Might/Haste first.
- Heirloom: a wave-5 shop item at wave 1. With refunds, replacing the heirloom in the shop pays out half its price (free gold). Consider no refund for heirlooms.
- Suggested wave scaling (not built): career difficulty tier `t` multiplies enemy health/damage by `1 + 0.15·t` in `WaveSettings.For`, with XP × `1 + 0.25·t`. Or scale by the character's level directly (`1 + 0.02·level`), which keeps runs tense but makes leveling feel pointless; tiers are the better-known pattern.
- Autoplay check: Fighter `--god` seed 2 kills 184 → 215 with 3 talent ranks + Ring of Vigor heirloom (max HP 30 → 40). Mage without `--god`, 4 runs (seeds 1–4) spending each point: died wave 2, 3, 3, 3. Not a balance read, the bot doesn't dodge.

**Save format (expensive to change, please review)**

- `user://career.json`, JSON, `version: 1`. Stores facts only: total XP (not level), talent ranks by id, heirloom by item file name, runs/victories/bestWave/kills. Levels and points re-derive from `CareerSettings`, so retuning the curve or talents needs no migration; if a character ends up with more ranks than points, its talents reset (logged).
- Unknown fields are preserved on write, so an older build doesn't erase newer data. A file from a newer version is moved aside (`.newer-<time>`) and a fresh career starts, rather than being overwritten. A corrupt file goes to `.corrupt-<time>`. Writes use a temp file and replace.
- ~~IDs are file names (`fighter`, `spear`) and talent `Id` strings, so renaming a class/item `.tres` orphans saved data.~~ **Resolved:** `ClassData`/`ItemData` now have explicit `Id` exports (equal to the old file names, so existing saves load unchanged); see ROADMAP.
- System.Text.Json uses reflection; fine for desktop. A trimmed/AOT export (iOS) would need a source-generated `JsonSerializerContext`.

**Unverified**

- New screens (mode select, talents, career summary, the class select Back button and level badges) checked in Movie Maker frames only, not with mouse/keyboard. Keyboard focus between the screens, and the R key on the career summary, untested by hand.
- Restart (R) after an arcade run was tested through Autoplay's input action only.
- The heirloom choice row could overflow the panel with 6 gear pieces (buttons are in one row).

## 4. Explicit IDs

**Judgment calls**

- No save version bump: every `Id` equals the old file name, so a v1 save loads as-is (verified: a save made by the previous build reloads with the same character, talents and heirloom, and the same-seed run matches exactly).
- A class or item with an empty `Id` still works, falling back to its file name, but logs an error at startup. Duplicates log an error too; the game still starts.
- IDs are snake_case, the same style as the file names. Spells (`AbilityData`) got no `Id`, since nothing saves them yet; add one when something does (e.g. per-class spell unlocks).

## 5. Follow-up answers

Implemented the answered questions from §1 and §2; decisions are in `Goals.md`. Pet count (§1) is still TBD.

**Judgment calls**

- **Targetable minions:** `MinionData.Targetable` (default on; skeleton and golem use the default). On: the minion joins `Groups.EnemyTargets` with the player; each enemy re-picks the *nearest* member every 0.25s (staggered by instance id, not RNG, so seeded runs stay repeatable). Melee enemies chase it and their contact hurts it (the existing distance check), ranged enemies keep their distance from it and shoot it, and enemy shots hit it. Off: the minion is fully ignored and **can't be hurt at all** (before this, minions lost HP on contact even though enemies ignored them). I picked the clean "targetable = can be hurt" meaning so the toggle tests two clear designs; say if you want the old middle ground back as a third option.
- Pure nearest-target means a skeleton next to an enemy always pulls it off you. No aggro range, threat or stickiness yet; enemies can flip between you and a minion as distances change.
- **Life steal is Warlock-only.** Read "life steal itself should be a Warlock spell" as: the shared shop sells no life steal; it comes from Warlock spells and weapons. Vampiric Fang (trinket) left the catalog (`.tres` kept; trinkets can't be heirlooms, so no save references it). Bloodstone Ring (now +4 Max HP, +8% Damage) and Blood-Soaked Robes (now +6 Max HP, +6% Attack Speed) stay in the catalog with their `Id`s, so a saved heirloom of either still loads, just with the new stats.
- I didn't add a new life steal spell: Blood Pact already is the timed life steal buff (+15% Life Steal, +50% Damage), and Drain Life / Crimson Nova / Curse of Agony steal on their own. A 6th Warlock spell would also need the spell select screen checked for 6 cards. If you want a purer one, a "Vampiric Embrace" (+30% Life Steal for 8s, no HP cost) could replace or join Blood Pact.
- **Hearts:** when hurt, a heart heals you; only the part past full HP goes into the reserve (a +2 heart at 1 HP missing heals 1 and stores 1). At full HP it all goes into the reserve. A heart waits on the floor only while HP *and* reserve are both full. Heal text is green, reserve text pink.
- **Space:** a full reserve is drunk first; otherwise free charge → bought potion → partial reserve, as before.
- Consumable drops: shop only (recorded in Goals.md, no change).

**Balance concerns**

- Autoplay average death wave (seeds 1–5, default spells): Necromancer 2.4 → 2.8, Warlock 2.6 → 3.0. Minions soaking hits helps the Necromancer most; both gain from hearts healing directly.
- Skeletons (14 HP, no wave scaling) now die much faster: about 100 hits on minions in a 3-wave seed-1 run. Their 18s lifetime rarely matters any more. Bone Golem as a tank is likely strong (it pulls a crowd off you).
- Without shop life steal the Life Steal stat only exists inside Blood Pact. Warlock sustain is now fixed by spell picks, not by build.

**Unverified**

- Feel of enemies switching between you and minions: seen in a few Movie Maker frames (enemies on the golem, damaged skeleton health bars), not played. Minions show no damage numbers, just a hit flash and health bar.
- Space reserve-first and heart-overflow checked through Autoplay `potion`/`heart` events (with a temporarily raised heart drop rate), not by keyboard.

**Questions**

- Should some minion types be non-targetable (e.g. skeletons as tanks, golem untouchable, or the reverse)? Today both are targetable.
- Should non-targetable minions still lose HP on contact (the old behavior), or stay untouchable?
- Should enemies prefer the player (e.g. only switch to a minion that's much closer), so minions don't trivialize melee waves?
