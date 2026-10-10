# ClassicFX S6 Batch 25 — models not yet registered

Base: `1d660677136c40e3a2a0a1b5c06e3d267427a387`. Native SHA: `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

## Scope: 10 unique IDs 273–282

- Cursed Temple Holy Item, Protection Skill, Restraint Skill (3 BMDs)
- Doppelganger slime chip (1)
- Raklion boss magic (1)
- Shockwave01, Shockwave02, ShockwaveSpin01 (3)
- WindForce (1)
- SD Aura / shield_up (1)

BMD paths verified against `Data_Broyal/Data/Skill` and `Data/Effect`. No duplicates: `BrokenIce0–3`, `CursedStatue1–2`, `RaklionBossCrack`, `KnightPlancrackA/B` and Summoner casting BMDs already existed.

Original move routines translated into existing ClassicFX Effect pool: owner-bone following (CursedTemple + SD), angle/light/alpha for shockwaves, gravity/bounce for slime, Raklion fade, WindForce rotating and creating controlled child model instances. No gameplay damage or server code touched.

## Explicit native dependencies not yet implemented

- Cursed HolyItem has native bone-20 relative offset (70,0,0) not representable by the current zero-offset bone bridge. Protection uses bone2 but skips attached `BITMAP_SHOCK_WAVE:10` emission; Restraint omits peripheral particle/joint features. Buff removal hooks are not yet wired.
- Shockwave01 subtypes 1,3,4 and Shockwave02/Spin require `nativeAnimationSpeed>0` because MuMain obtains the specific owner BMD action PlaySpeed; the caller must pass it to the new trailing optional `CreateEffect(..., nativeAnimationSpeed: speed)`. Shockwave01 subtype2 uses original fixed velocity 0.3. This prevents fabricated animation speeds.
- Shockwave01 subtype3/4 uses owner world-position fallback for native `Owner->StartPosition`. Native interpolation is therefore partial.
- WindForce subtype1 requires original g_isCharacterBuff status and is **rejected** until buff bridge. The supported nonpersistent subtypes are 0,2,3,4,5.
- Cursed Temple skills are 3D models only. Event ownership/equip triggers are not hooked up. No end-to-end testing or FPS profiling.
- New types are registered for explicit ClassicFX creation; none of these models automatically replaces a legacy callsite until dispatch is migrated.
- No local .NET build or CI for this branch; run guarded PowerShell before push.
