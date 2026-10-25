I want to create a video game that is in the arena-shooter adjacent genre. A few games of influence would be Brotato, Pickle Pete, Vampire Survivors.

I want to make mine a fantasy theme, and I think I want to do a few things a little different than other games like this, by integrating RPG types of features. For example, armor slots, trinkets, rings, etc.

1. I want the player to be able to choose different classes to play as, with very different play styles. My thoughts on these are fantasy/dnd type classes.
   - Fighter - A class focused on weapons. Decent armor/health
   - Mage - Focused on magic. low armor/health
   - Warlock - Magic and focus is health draining/life stealing
   - Necromancer - Minions and magic

I think I also like the idea of adding RPG elements like a spell list/skill tree, and at the start of each game, the player can select 3-4 options from their spell list. For example maybe the mage wants to focus on a frost/cold build, or a fire/burning build.

I think I also want to add potions/buffs. So maybe health potions are purchased, or dropped during games, and they can choose to use them during the rounds.

I think game play is similar to Brotato. There are maybe rounds that last maybe a minute or two. Enemies spawn from around the map. In between each round the player get's to manage inventory/rewards. I'm not sure yet how I want to do that piece. I'm thinking either currency is gained throughout each round and options are availalbe for purchase, or items drop during the round and the user can choose which to equip. Or maybe even a combination of the two, and items they don't want can be sold and used to purchase other items.

Other decisions I haven't decided on yet.

1. ~~should every single new game be a start from scratch, or should the character be improved incrementally over time so they get stronger and stronger. I like both options, and maybe even would provide both options. Career mode vs arcade or something like that.~~ Decided: both (see "Progression modes" below).
2. What the currency is
3. Whether currency has to be picked up, or if it is auto-accrued.

## Decisions so far (prototype — revisit after playtesting)

- **Currency:** gold. Enemies drop it, and the player has to walk over it to collect it (Pickup Range is an upgradable stat). Gold left on the floor when a wave ends goes into a **bag** (same as Brotato's bag and Pickle Pete's jar, for familiarity): each pickup in later waves pays double, drawing the extra from the bag, until it's empty. The bag can grow across waves and never expires.
- **Between waves:** a shop offering 4 random items from a catalog, with rerolls that get more expensive each time and prices that rise slightly each wave. Items are permanent stat modifiers for the rest of the run. Item drops and selling aren't built yet (replacing gear refunds half its price, see below).
- **Run structure:** 5 waves of increasing length (30s up to 60s). Each wave increases the spawn rate and enemy health, damage and speed. HP refills at the start of each wave.
- **Weapons and spells:** every class's equipped weapon is its basic attack and fires constantly on its own (aiming at the nearest enemy). Spells and abilities are stronger options on longer cooldowns, kept in hotkey slots 1–4. Each slot can auto-cast or be cast on demand (manual casts aim at the mouse). Keys after 4 are for consumables (5, 6 so far).
- **Classes so far:** Fighter (Sword; spells Whirlwind, Ground Slam, Charge, Shield Wall, War Cry), Mage (Wand; spells Fireball, Frost Nova, Ice Bolt, Chain Lightning, Meteor), Warlock (Hex Rod; Drain Life, Curse of Agony, Shadow Bolt, Crimson Nova, Blood Pact) and Necromancer (Bone Wand; Raise Skeletons, Bone Golem, Grave Curse, Bone Spear, Bone Nova).
- **Life steal:** a percent of damage dealt that heals you. Each ability has a base (Hex Rod 8%, Drain Life 40%…) and the **Life Steal** stat adds to all of them, like Crit Chance. Overkill doesn't count, DoT ticks do. Capped at 5 HP per second from all sources (Brotato caps it too), so area spells can't full-heal in one hit. Any class can buy life steal items.
- **Damage over time:** any ability can apply a DoT (curses, Shadow Bolt; fire burns later). One DoT per enemy: the strongest wins, and a weaker one doesn't extend it.
- **Minions (Necromancer):** allied units with simple AI (attack the nearest enemy near the player, otherwise follow). They expire on a timer and lose HP touching enemies, but enemies don't target them and enemy shots pass through them, so enemy AI stays simple and the player stays the focus. Each type has a cap (6 skeletons, 1 golem); summoning past it crumbles the oldest. They come from summon spells and from enemies killed while marked (Grave Curse, Reaper's Sickle, Skull Staff). Minion damage scales with Damage% plus a **Minion Damage** stat. Minions don't life steal.
- **Spell selection:** at run start, after picking a class, the player picks 3 spells from the class's list (`ClassData.SpellPicks`). Pick order sets hotkeys 1–3. Slot 4 is left open for now, so spells could also be gained during the run later (the "mix" option). The list grows toward 6–8 per class.
- **Buffs:** temporary stat bonuses (War Cry, Shield Wall; potions later) sit on top of the run's stats and expire on a timer. Recasting refreshes the timer instead of stacking. All buffs clear at the start of each wave.
- **Healing:** potions, plus life steal. A health potion (+10 HP) on Space, one free charge per wave, refilled at each wave start. HP still refills fully between waves.
  - **Hearts fill a reserve potion** instead of healing directly. Enemies sometimes drop a heart (+2); walking over it pours that into a second, slowly-filled potion. The reserve holds one potion's heal (10, grows with Potion Heal), keeps between waves, and doesn't become a charge. While it's full, hearts stay on the floor; a heart worth more than the room left tops it up and the rest is lost.
  - **Space drinks in this order:** a full potion (the free charge, then bought ones) for the full heal; only when none are left, the whole reserve, whatever its fill. Nothing happens at full HP, so a misclick doesn't waste a potion.
- **Consumables:** bought on a potion shelf under the shop's offers, always in stock (not rerolled, not taking an offer slot), price rising with the wave like items. They're kept until drunk, with a carry limit each. Health Potion (extra full charge, carry 2), Potion of Fury (+30% Damage for 10s, key 5), Potion of Swiftness (+35% Move Speed, +60 Pickup Range for 10s, key 6). Buff potions are timed buffs: drinking one that's active refreshes it, and they clear at wave start like other buffs. A bigger heal is an item, not a consumable: the **Potion Heal** stat (Alchemist's Flask trinket, +4) adds to the potion and the reserve.
- **Equipment (two kinds of item, Brotato-style split):** *gear* goes in 6 slots (Main Hand, Off Hand, Armor, Ring ×2, Amulet), one item each. *Trinkets* have no slot and stack without limit (Whetstone, Rabbit's Foot…). Gear is limited, so it gets bigger numbers per item than trinkets.
  - Buying gear for a full slot replaces the old piece, which refunds half its base price. No inventory or stash. When both ring slots are full, the player picks which ring to replace.
  - The main-hand item is the weapon, i.e. the basic attack. Each class starts with its weapon (Sword, Wand) and can buy its own others (`ClassData.Weapons`). Weapons are class-locked for now so classes keep their identity. Shared weapons could go into the catalog later.
  - Two-handed weapons (Greatsword, Staff) also fill the off hand. Buying an off-hand item while holding one puts the starting weapon back in the main hand.
  - The shop doesn't offer gear you're already wearing (except rings).
- **Armor** is a flat reduction to each hit (minimum 1 damage).
- **Crits (Brotato-style):** each weapon and spell has its own base crit chance (5% default) and crit multiplier (×1.5 default; heavy hits like Sword and Ground Slam ×2). The player has a global **Crit Chance** stat only, added to every ability's base and capped at 100%. No global crit damage stat: fast and slow weapons need different multipliers, and chance × damage both being buyable runs away in a shop where items stack. Revisit a D3-style additive Crit Damage stat if equipment slots add a gear budget. Crit is rolled per enemy on impact; damage reservations use the non-crit damage.
- **Progression modes:** a mode select screen comes first. **Arcade** is a single fresh run with nothing saved. **Career** gives each class one persistent character (one save file, `user://career.json`, slot-less for now). Career runs keep the same 5-wave structure and difficulty.
  - **XP and levels:** every career run banks XP, win or lose: 1 per kill, 20 per wave cleared, +50 for a victory (a won run is ~350–450 XP, an early death ~70–110). Level 2 costs 60 XP and each level 40 more than the last; max level 25. Each level after the first is one **talent point**.
  - **Talents:** permanent stat bonuses for that class's career runs, 1 point per rank: Toughness +2 Max HP, Might +3% Damage, Haste +3% Attack Speed, Fleet Foot +3% Move Speed (5 ranks each), Iron Skin +1 Armor (3), Precision +2% Crit Chance, Magnetism +12 Pickup Range (5 each). 33 ranks but only 24 points at the cap, so a maxed character still chooses. Reset is free at any time (no respec cost), so experimenting is safe.
  - **Heirloom:** after a career victory the player may keep one worn piece of gear (not the starting weapon); it starts equipped in every later run of that class until replaced. One heirloom per character. Defeat keeps XP and record but never changes the heirloom.
  - **Save IDs:** the save references classes, items and talents by an explicit `Id` export, never by file name. An `Id` must never change once shipped; files can be renamed freely.
  - Abandoning a run (quitting the app) banks nothing. Gold, trinkets and potions never carry over.
- **Enemy roster so far:** Grunt (basic chaser), Runner (fast and fragile, from wave 2), Archer (keeps its distance and shoots dodgeable bolts, from wave 3), Brute (slow, tanky, resists knockback, hits hard, from wave 4). Each spawn is a weighted random pick from the types unlocked so far.
