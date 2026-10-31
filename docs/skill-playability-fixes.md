# Season 6: first playability fixes

No build or in-game test was run; the user compiles locally. C# syntax was
checked separately. The Season6SkillRoutes smoke cases were extended but not run.

## Implemented client changes

- Force 60, Force Wave 66 and Strengthener 509 use the Dark Lord strike,
  including Dark Horse/Fenrir variants. Their 0x19 echo spawns two native
  Waves/1 and one Piercing2/0 at strike frame 3.
- F3:11 skill updates are copied out of the network buffer and applied on the
  main thread. A skill-list revision refreshes both palettes, clears a removed
  selected skill and prunes its PC/mobile shortcuts. Pending casts recheck the
  authoritative list before sending.
- Ephemeral ClassicFX sprites expire at the next Update even when Draw was
  skipped. Persistent Elf/Soul Barrier joints and Wizardry carriers stop while
  unfocused and recreate on return. Aura trail capacity can shrink again;
  background FPS cannot leave it permanently enlarged.
- Fire Slash 55 uses the native wheel animation, Gathering/1 at frames 1/2 and
  JOINT_FORCE/0 at frame 3 (the child of native BITMAP_SWORD_FORCE), using the
  existing ClassicFX movement/rendering. Its approximate screen-space arc is
  no longer used. Only the area echo creates the visual.
- Equipped arrows (4,15) and bolts (4,7) show the server-reported quantity,
  stored in durability even with base durability 255.

## Server/data audit

Audited OpenMU-server ba45b19245acf5c9e02d17700f0efeef1d37db06 and the supplied
Data/Local/skill.bmd. Force 60/66 have range 4, MP 10, AG 0 and delay 0 in the
Data, matching the S6 initializer. ForceSkillAction accepts 60 and delegates to
66 when learned. Strengthener requires Force Wave. Actual rejection or damage
on the running server has not been reproduced and remains to be verified.

Fire Slash is an AreaSkillAutomaticHits skill in the S6 initializer, with its
own frustum and defense-reduction effect. This patch sends no extra hits from
visuals and changes no server damage or target selection.

Arrow consumption exists in AttackableExtensions.ApplyAmmunitionConsumption
and applies to successful and missed hits; Player.OnAmmunitionAmountChanged
updates item durability and sends 0x2A (or removes an empty stack). Normal bow
configuration adds AmmunitionConsumptionRate=1. Infinity Arrow multiplies it
by zero for 600 seconds. The running database/buff state was not accessible;
no speculative server configuration or local fake decrement was applied.

## Local acceptance checks

1. DL outside safe zone: Force against a nearby monster, on foot, on Dark Horse
   and on Fenrir. Confirm repeated strike, MP loss, server animation and actual
   HP loss. Repeat with/without Force Wave on the scepter. If HP does not fall,
   capture client Sending/Skill request sent and server validation logs.
2. Summoner: assign a book skill to current selection and PC/mobile shortcuts;
   replace/remove the book while idle and while approaching a target. Its icon,
   binding and pending cast must disappear; reopening palettes must not restore it.
3. Elf/SM: activate Greater Defense/Damage, Soul Barrier and Wizardry; minimize
   or switch apps for 30-60 seconds. Return repeatedly: no burst of old sprites,
   giant trails or multiplication of auras; active buffs must remain visible.
4. Elf: wait until Infinity Arrow expires (or clear it server-side); start with
   a known arrow/bolt count. Attack several monsters, including misses. Compare
   equipped counter, 0x2A durability logs, and the server editor. With Infinity
   Arrow active the quantity should remain unchanged. If no consumption occurs
   without it, inspect the live bow's rate and active attribute modifiers; this
   case remains open until the running server is tested.
5. MG: Fire Slash near one monster and a group, facing several directions.
   Confirm one correctly oriented world-space arc per cast, MP/AG consumption,
   actual damage and defense-reduction state; compare server hit counts before
   and after to rule out duplicate hits. Power Slash/Twisting Slash are unchanged.

Native visual reference: sven-n/MuMain at
21728b1e5b03e0763b38ef9e23f79645e0df7ad2, ZzzCharacter.cpp,
Render/Effects/ZzzEffect.cpp and Behaviors/MoveHandlers.cpp.
