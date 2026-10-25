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

## 6. Item rarity

**Judgment calls**

- **4 tiers:** Common (white), Uncommon (green), Rare (blue), Epic (purple), Diablo colors. No Legendary yet: I'd rather keep orange for a later tier with a unique effect (Diablo legendaries, Brotato's tier IV) than a fifth "bigger numbers" step. Adding one is a new `Rarity` entry plus a `RarityTier` in `data/RaritySettings.tres`, no save change.
- **Numbers (all in `data/RaritySettings.tres`):** stats ×1 / 1.25 / 1.5 / 2, price ×1 / 1.2 / 1.45 / 1.8, weapon damage ×1 / 1.15 / 1.3 / 1.5. Price grows slower than stats, so rarer items are slightly better value per gold (gear slots are limited, so power per slot matters more than per gold).
- **Rounding:** stats are whole numbers, rounded half up, and a higher tier always adds at least 1 over Common so the card never shows the same number. Side effect: tiny stats jump a lot (+1 Armor → +2 at Uncommon, Rare and Epic alike).
- **Downsides don't scale.** Chainmail's −5% Move Speed and Assassin's Mark's −3 Max HP stay the same at every tier, so a rarer copy is strictly better. Scaling them too would be "more of the item", which reads oddly as an upgrade.
- **Weapons:** rarity scales the basic attack's damage (and its DoT) through a separate weapon `AbilityContext`, multiplied with the Damage stat. Spells and minions aren't affected. A smaller curve than stats, because the weapon is a big share of total damage. Starting weapons are always Common.
- **Odds:** each non-Common tier has a chance at the first shop plus a per-wave increase, capped; Common gets the rest. Uncommon 15% +8/wave (max 45), Rare 3% +5/wave (max 30), Epic 0% +2.5/wave (max 15). The shop shows the current odds in tier colors under the gold line. Rerolls roll rarity again.
- **Shop still hides gear you wear, at any rarity** (rings excepted). So a Rare Spear never shows while you hold a Common Spear. Alternative: allow it when the offer is rarer than the worn one.
- **Refund** = half the rarity-scaled base price (no wave scaling, as before).
- Trinkets of different rarities are separate rows in the equipment panel (Whetstone, Whetstone x2 in green…) and their stats just add up.
- `CareerSummary.HeirloomChosen` became a plain C# event instead of a `[Signal]`, because signal arguments must be Godot types and `ItemInstance` is plain C#.
- **Heirloom save:** new `heirloomRarity` field next to `heirloom` (enum name, e.g. "Epic"), no version bump. Missing or unknown → Common (unknown logs a warning). Caveat: an older build keeps the field as unknown data, so if an older build changed the heirloom, the new build would apply the stale rarity to the new item. Only matters if you switch between builds.

**Balance concerns**

- The last shop is now ~64% non-Common, so late-run gold buys more power than before; waves 4–5 may get easier. Epic gear (+100% stats) on a Ring of Fury or Tome of Power is a big spike.
- An Epic heirloom from one lucky victory starts every later run strong (e.g. Epic Bloodstone Ring: +8 Max HP, +16% Damage at wave 1). This adds to the talent power creep in §3. Heirloom upgrades should probably cap or cost something.
- Price ×1.8 for Epic at the last shop can be 70–90 gold, more than a typical wave's income, so Epics may often be unaffordable. That's partly the point (a reason to save), but watch it.

**Unverified**

- Shop card colors were checked in Movie Maker frames only. The blue and purple were picked for contrast on the dark panels; not checked for color blindness (green/white is the risky pair, but the slot line also spells out the tier name).
- Tooltips (equipment panel, heirloom buttons) now include the tier and slot line; not hovered by hand.

**Questions**

- Is ×2 for Epic the right ceiling, or should Epic be ×1.75 and leave room for Legendary?
- Should the shop offer a rarer copy of gear you already wear (as an upgrade)?
- Heirloom upgrades (§3 Q6): bump the tier by one per victory it's carried, capped at Epic? That uses this ladder directly.

## 7. New stats

**Judgment calls**

- **Max HP and Attack Speed** already worked end to end (checked with `--stat`: Max HP 16 → 26, Wand cooldown 0.35 → 0.23s at +50%) and were well covered (6 items each, plus the Toughness and Haste talents), so I added no items for them.
- **Cooldown Reduction** affects **spells only**. Attack Speed already speeds up the weapon; letting CDR do it too would make it a strictly better Attack Speed. Cap **50%** (LoL's old 40% felt low for a game where spells are the stars; 50% means at most twice as often). A cooldown already running keeps its time; the next cast uses the new value (matters for War Cry-style buffs later).
- **HP Regen = HP per 10 seconds.** Item stats are whole numbers (rarity rounds them), so "per second" would need decimals and "per 5s" would make +1 the smallest step and too coarse for rarity (+1 → +2 is ×2). Per 10s gives +2/+3 items that rarity can scale. Heals every frame (smooth), the green "+N" shows at most once a second and only once a whole HP is healed. It only heals HP, never fills the potion reserve. No cap.
- **Dodge** covers contact and enemy shots, cap **60%**. A dodge **does** start i-frames: contact damage is checked every frame, so without them a crowd would roll 60 times a second and dodge would never save you from contact. Side effect: the player blinks after a dodge, like after a hit. `--god` skips the roll entirely.
- **Determinism:** dodge only draws a random number when the chance is above 0, so seeded runs without dodge are identical to before (verified: Fighter/Mage/Warlock seeds 1–5 identical with the old catalog; with the new catalog only `offers` lines differ). Necromancer runs weren't comparable (the known minion non-determinism; three runs of the same build gave 102/104/106 kills).
- **Caps on display:** item cards and tooltips add a "Dodge caps at 60%" line. The shop's "Your stats" panel now lists **only non-zero stats** (with 14 stat types the full list pushed the shop off screen) and shows "+60% Dodge (capped)" past the cap. Totals over the cap are kept, so a later minus can eat into the excess.
- Items: Hourglass (trinket, +8% CDR, 25), Sage's Amulet (amulet, +15% CDR +5% Damage, 35), Mending Herb (trinket, +2 Regen, 20), Ring of Regeneration (ring, +3 Regen +3 Max HP, 25), Feather Charm (trinket, +5% Dodge, 20), Shadow Cloak (armor, +10% Dodge +5% Move Speed, 30). Ids = file names.
- Talents: Focus +3% CDR ×5, Evasion +2% Dodge ×5, Recovery +1 Regen ×3. Total ranks 33 → 46 for 24 points.

**Balance concerns**

- Dodge stacks with trinkets without limit up to 60%: 12 Feather Charms (240 gold) hits the cap. Brotato has the same shape; watch for a dodge-stacking Mage being unkillable.
- Autoplay average death wave, seeds 1–5: Fighter none 2.6 / Dodge 30 3.0 / Regen 4 2.8 / CDR 30 2.6; Mage none 2.6 / 3.0 / 3.0 / 3.2. Dodge 30% removed about a third of hits as expected (e.g. Fighter seed 3: 30 landed, 15 dodged).
- Regen on the Mage (16 HP) is relatively strong: Ring of Regeneration ≈ 18 HP per 60s wave, more than a potion.
- CDR 50% on Meteor/War Cry may be strong (War Cry 15s → 7.5s, 6s uptime = 80%).

**Unverified**

- "Dodge" and regen "+1.5" texts seen in Movie Maker frames only, not played; the shop and career talent screens checked in frames (they fit).
- The equipment panel tooltip for the new items wasn't hovered (same `Describe()` text as cards, checked via a temporary print).

**Questions**

- Should Cooldown Reduction also shorten the weapon (and Attack Speed become weapon-only damage tempo)? I chose spells only.
- Regen per 10s OK, or would you prefer Brotato's abstract "HP Regeneration" points?
- Should a dodge give the full 0.5s i-frames (current) or a shorter window?

## 8. New spells

Each class went from 5 to 8 spells (pick 3 stays). Numbers and per-spell damage are in `ROADMAP.md` tuning notes.

**What was added**

- Fighter: **Throwing Axe** (returning, piercing blade), **Shockwave** (line crack that stuns 1.5s), **Second Wind** (heal 12 HP over 5s via +HP Regen, shove; auto-cast below half HP).
- Mage: **Arcane Missiles** (5 homing darts, each its own target), **Blizzard** (4s ice zone on the densest crowd, damage + slow), **Blink** (short teleport, frost burst where you left; manual).
- Warlock: **Siphoning Aura** (5s aura around you, 25% life steal), **Corruption** (DoT that spreads to 2 neighbors when its host dies, 10% life steal), **Vampire Bats** (4 homing bats, 30% life steal).
- Necromancer: **Skeletal Mage** (ranged, non-targetable minion, up to 2), **Bone Armor** (10-HP barrier for 8s + shove), **Corpse Explosion** (up to 3 skeletons burst for 14 each).

**Judgment calls**

- Whirlwind and Frost Nova already existed, so the suggested "whirlwind" and "frost nova" became Second Wind (the Fighter had no sustain at all) and Blizzard (a lasting zone rather than a burst).
- New reusable behaviors, kept small: `LineAbility`, `AuraAbility`/`AuraField`, `BlinkAbility`, `DetonateAbility`, plus options on existing ones (projectile `Count`/`Pierce`/`Returns`/`Homing`/`Shape`, `BuffAbility.Shield`/`CastBelowHealth`, `MinionData.ShotSpeed`/`Detonatable`) and two hit effects (`StunDuration`, `DotSpreads`).
- **Stun** is a new effect, not a 0% slow, so stunned archers also stop shooting. Knockback resistance shortens it (Brute 1.5s → ~1s). No diminishing returns: two Shockwaves could chain-stun. Contact damage still applies while stunned.
- **Barrier** absorbs after armor, so armor and a barrier stack. A hit fully absorbed shows "Blocked" and still starts i-frames. It's not shown on the HUD, only as bone plates around the player. Recasting refreshes to max(current, new); no stacking.
- **Corruption** spreads at full duration to the 2 nearest enemies that don't already carry a spreading DoT, and keeps spreading as long as it kills. It ran ~120 spreads in a seed-2 run, not a runaway: by wave 3–4 enemies outlive 15 damage. If a stronger DoT (Curse of Agony) replaces it, the spread flag sticks and the *stronger* DoT spreads. That's a deliberate synergy; say if you'd rather it be lost.
- **Skeletal Mage is non-targetable** (enemies ignore it, it can't be hurt) as a first use of the Targetable toggle. It deals more than skeletons but tanks nothing, which shows in survival (see concerns).
- **Corpse Explosion only blows up skeletons** (`Detonatable` off for the golem and skeletal mages): blowing up a 25s tank or your turret felt like a trap. Damage scales with Damage% + Minion Damage%, like minions, not like other spells. With no skeletons it just puffs (the cooldown is still spent on a manual cast; auto-cast waits for a skeleton with 3 enemies around it).
- **Second Wind** reuses the HP Regen stat as a timed buff (+24 per 10s for 5s = 12 HP), so it also shows the regen "+N" text, and Life-steal-cap rules don't apply.
- **Blink**'s auto-cast (only with auto-cast switched on) jumps away from the crowd's center and can run into a wall (walls cut it short). It deals 5 damage at the spot you left.
- **Spell `Id`s:** every spell has one (its file name) and startup validation checks them with the other save Ids. Weapons (`data/weapons/`) didn't get one; their items already have Ids.
- **Autoplay `damage` event:** damage landed per source at run end (`Combat.DamageBySource`, filled through `Combat.Deal`; DoT ticks keep their source, minions use their type name). Kept as a permanent tool rather than a temporary print, since it's what spell tuning needs. `run_end`/`wave_end` also log `shielded`.
- Spell select: 4×2 grid of 285×262 cards with the color swatch next to the name; fits 1280×720 with ~20px to spare.

**Balance concerns**

- Throwing Axe (~1600) and Shockwave (~1500) are near the top of the Fighter list; the Fighter's new-spell pick survived better than its default (3.0 vs 2.6). Fine while the Fighter is the weakest class, but watch them.
- Necromancer picks without targetable minions die sooner in Autoplay (2.0–2.4 vs 3.0). Skeletal Mage being non-targetable and Corpse Explosion consuming skeletons both trade away the tanking that keeps the Necromancer alive.
- Mage's new pick survived worse in Autoplay (2.2 vs 2.6), partly because the bot doesn't use Blink well; Arcane Missiles + Blizzard + Frost Nova did 3.0.
- Siphoning Aura heals up to 25% of 5 dps per enemy around you, capped by the 5 HP/s life steal cap; with a crowd it reaches the cap easily. Vampire Bats + Blood Pact may be strong sustain.
- Cooldown Reduction at 50% makes Second Wind a 10s heal and Bone Armor a 6s barrier (10 HP per 6s is more than Ring of Regeneration ×3).

**Unverified**

- Feel and readability seen only in Movie Maker frames: Shockwave band and stun stars, the axe's spin and return, missile darts, Blizzard and aura fields, bats (small; radius raised to 6), skeletal mage orbs, corruption lines, the bone-plate barrier and "Blocked". Not played by hand.
- Manual aiming of Shockwave, Blink, Corpse Explosion (picks skeletons nearest the cursor) and manual-cast homing (missiles seek around the cursor) wasn't tested with a mouse; Autoplay only auto-casts.
- Blink into walls and the arena edge.

**Questions**

- Should Skeletal Mage be targetable (a fragile ranged tank) or stay untouchable as now?
- Corpse Explosion: skeletons only (now), or every minion including the golem?
- Should stun have diminishing returns or an immunity window, to stop Shockwave + CDR chain-stunning?
- Is Second Wind the Fighter sustain you want, or would you rather keep healing to potions/hearts (and give the Fighter a taunt or a third attack instead)?


## 9. UI and QoL

**What was added**

- HUD weapon slot left of the spell bar (name in rarity color, bar that refills between shots, item tooltip). Hotbar tooltips now use the shop's tooltip style.
- Tab gear screen (waves only), Esc pause menu (waves and the shop), Settings (pause menu and mode select), Retry same class on both end screens and in the pause menu, HUD gold following shop purchases and rerolls.

**Judgment calls**

- **Pause menu works in the shop too**, not only mid-wave, so you can abandon or retry from there. It remembers the pause state it found, so Resume over the shop leaves the game paused. The gear screen is waves-only (the shop already shows your gear and stats).
- **Pause menu: Retry and Abandon are both there.** "Restart run" and "Retry" are the same action (same class and spells), so there's one button. Abandon = the existing restart (class select, same mode). All three run-ending buttons (Retry, Abandon, Quit) ask first; in career the prompt says the run banks nothing, keeping the existing decision. In arcade it says gold and items are lost.
- **Retry in career skips the talents screen** as well as class and spell select (it's a one-click retry). Points from a level-up wait until you go through Choose class. Alternative: route Retry through the talents screen only when there are free points.
- **Retry survives the scene reload** through `Career.PendingRetry` (class and spell `Id`s, resolved against the reloaded scene), rather than a new autoload. If an Id no longer resolves it falls back to class select with a warning.
- **The end screen's buttons**: "Retry Mage (same spells)" (focused, so Enter/Space retries) and "Choose class (R)". The career summary's "Continue (R)" became "Choose class (R)" for the same reason. R works as before. A dim backdrop now sits behind the arcade end message.
- **Damage numbers off hides enemy damage numbers only.** Your own red numbers, heals, "Dodge", "Blocked", "x2" and spell names stay, since they're feedback about you. Say if you want everything gone.
- **Screen shake scales the offset**, not the trauma, so the random draws stay the same; 0% means no movement at all.
- **Master volume** sets the Master bus (dB from a linear 0–100%, muted at 0). No audio yet, so it's untested by ear.
- **Settings and Autoplay:** Autoplay ignores `user://settings.cfg` and uses defaults in memory (seeded runs mustn't depend on your settings, e.g. damage numbers off changes random draws). `--settings=PATH` points it at a scratch file. Your real settings file is never written by tests.
- Fullscreen uses `WindowMode.Fullscreen` (borderless on macOS, its own Space). Not exclusive fullscreen.
- Gear screen shows bonuses from gear, trinkets and talents (not timed buffs, which are listed separately with their time left), plus derived character numbers (HP, armor, move speed, pickup range, dodge chance, weapon rate). The full stat list shows all 14 stats including zeros, with "(cap 60%)" / "(cap 50%)" next to Dodge and CDR.
- `GearViews` (static) replaced the shop's private equipment-list code; the shop now uses it too.

**Verified**

- `dotnet build` clean, smoke test clean.
- Seeded arcade Autoplay runs (4 classes × seeds 1–2, with and without `--god`) identical before and after for Fighter, Mage, Warlock. Necromancer differs, but it also differs between two runs of the same build (the known minion non-determinism: seed 2 `--god` gave 252, 279, 287, 286 kills).
- `--tour` passes (gear screen opens/pauses/closes, pause menu → Settings → toggle → Back → Resume, unpaused after).
- Settings persistence: launch 1 with `--settings=<scratch>.cfg --tour` wrote `damage_numbers=false`; launch 2 logged `loaded ... damage_numbers=false`.
- Retry: arcade `--runs=2 --retry` and career `--runs=2 --retry` kept class and the non-default spell picks (`retry=true` in run 2's `run_start`; career skipped the talents screen).
- HUD gold: a temporary print showed the HUD gold following every shop purchase (removed).
- Movie Maker frames: HUD weapon slot, gear screen, pause menu, settings, arcade end screen and career summary with Retry, mode select with Settings.

**Unverified**

- Anything done with a real keyboard and mouse: Esc/Tab/F11/Alt+Enter on the Mac, hover tooltips on the gear screen and the weapon slot, dragging the sliders, keyboard focus moving back to the shop's button after closing the pause menu.
- Fullscreen toggling on macOS (headless and Movie Maker runs don't touch the window), and going back to the 2560×1440 window size afterwards.
- Pausing over the shop (logic only; Autoplay's tour pauses mid-wave).

**Questions**

- Should Retry in career go through the talents screen when you have free points?
- Damage numbers toggle: enemies only (now), or every floating number?
- Do you want Esc to also work as "Back" on the select screens?
