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

1. should every single new game be a start from scratch, or should the character be improved incrementally over time so they get stronger and stronger. I like both options, and maybe even would provide both options. Career mode vs arcade or something like that.
2. What the currency is
3. Whether currency has to be picked up, or if it is auto-accrued.

## Decisions so far (prototype — revisit after playtesting)

- **Currency:** gold. Enemies drop it, and the player has to walk over it to collect it (Pickup Range is an upgradable stat). Gold left on the floor when a wave ends goes into a **bag** (same as Brotato's bag and Pickle Pete's jar, for familiarity): each pickup in later waves pays double, drawing the extra from the bag, until it's empty. The bag can grow across waves and never expires.
- **Between waves:** a shop offering 4 random items from a catalog, with rerolls that get more expensive each time and prices that rise slightly each wave. Items are permanent stat modifiers for the rest of the run. Item drops and selling aren't built yet.
- **Run structure:** 5 waves of increasing length (30s up to 60s). Each wave increases the spawn rate and enemy health, damage and speed. HP refills at the start of each wave.
- **Weapons and spells:** every class's equipped weapon is its basic attack and fires constantly on its own (aiming at the nearest enemy). Spells and abilities are stronger options on longer cooldowns, kept in hotkey slots 1–4. Each slot can auto-cast or be cast on demand (manual casts aim at the mouse). Slots after 4 are reserved for consumables later.
- **Classes so far:** Fighter (Sword; spells Whirlwind, Ground Slam, Charge, Shield Wall, War Cry) and Mage (Wand; spells Fireball, Frost Nova, Ice Bolt, Chain Lightning, Meteor).
- **Spell selection:** at run start, after picking a class, the player picks 3 spells from the class's list (`ClassData.SpellPicks`). Pick order sets hotkeys 1–3. Slot 4 is left open for now, so spells could also be gained during the run later (the "mix" option). The list grows toward 6–8 per class.
- **Buffs:** temporary stat bonuses (War Cry, Shield Wall; potions later) sit on top of the run's stats and expire on a timer. Recasting refreshes the timer instead of stacking. All buffs clear at the start of each wave.
- **Healing:** two sources. Enemies have a small chance to drop a heart (+2 HP), which only gets picked up while you're hurt. And a health potion (+10 HP) on Space, usable once per wave, refilled at each wave start. HP still refills fully between waves.
- **Armor** is a flat reduction to each hit (minimum 1 damage).
- **Crits (Brotato-style):** each weapon and spell has its own base crit chance (5% default) and crit multiplier (×1.5 default; heavy hits like Sword and Ground Slam ×2). The player has a global **Crit Chance** stat only, added to every ability's base and capped at 100%. No global crit damage stat: fast and slow weapons need different multipliers, and chance × damage both being buyable runs away in a shop where items stack. Revisit a D3-style additive Crit Damage stat if equipment slots add a gear budget. Crit is rolled per enemy on impact; damage reservations use the non-crit damage.
- **Enemy roster so far:** Grunt (basic chaser), Runner (fast and fragile, from wave 2), Archer (keeps its distance and shoots dodgeable bolts, from wave 3), Brute (slow, tanky, resists knockback, hits hard, from wave 4). Each spawn is a weighted random pick from the types unlocked so far.
