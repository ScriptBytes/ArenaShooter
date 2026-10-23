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
- Should enemies attack minions (taunt/tank gameplay), or keep minions as pure damage?
- Should life steal stay available to every class, or be Warlock-flavored (class-specific items)?
- Necromancer pet count: is 6 skeletons + 1 golem the right amount of screen clutter once real sprites arrive?

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
- Should a full reserve become a regular charge (freeing room for more hearts), or should excess hearts heal directly when the reserve is full?
- Should consumables also drop during waves (rare potion drop), or stay shop-only?
- Should Space drink the reserve first when it's full, to make room for more hearts before using the free charge?

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
- IDs are file names (`fighter`, `spear`) and talent `Id` strings, so renaming a class/item `.tres` orphans saved data (an unknown heirloom is ignored with a warning, not deleted). Consider explicit `Id` exports on `ClassData`/`ItemData` before renaming anything.
- System.Text.Json uses reflection; fine for desktop. A trimmed/AOT export (iOS) would need a source-generated `JsonSerializerContext`.

**Unverified**
- New screens (mode select, talents, career summary, the class select Back button and level badges) checked in Movie Maker frames only, not with mouse/keyboard. Keyboard focus between the screens, and the R key on the career summary, untested by hand.
- Restart (R) after an arcade run was tested through Autoplay's input action only.
- The heirloom choice row could overflow the panel with 6 gear pieces (buttons are in one row).
