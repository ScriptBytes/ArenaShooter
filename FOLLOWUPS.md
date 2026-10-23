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
