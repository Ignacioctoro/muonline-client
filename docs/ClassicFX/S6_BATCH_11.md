# ClassicFX — Season 6 Batch 11

Parent: `classicfx-nova-pilot@fae0a2d291eda5f7fdc5cb96faf8430a0fc94906`.

Reference: MuMain `21728b1e5b03e0763b38ef9e23f79645e0df7ad2`, `ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp`, `ZzzOpenData.cpp`, `EffectTypes.json`. BMD names verified in `Data_Broyal/Data/Effect`.

## Implemented
- **Blow of Destruction (MODEL_BLOW_OF_DESTRUCTION) subtypes 0 and 1**: native 40-tick life, owner offset, companion phase, impact at tick 23, floor glow (`BITMAP_FLARE_BLUE`), sprite emissions (`BITMAP_SWORD_EFFECT_MONO` and `BITMAP_LIGHT`), waterfall/smoke particles and child BMD impacts. Uses existing ClassicFX queues and MonoGame renderer.
- **NightWater01**: BMD `Effect/nightwater01.bmd`; 25-tick life, random yaw, alpha decay.
- **KnightPlancrackB**: BMD `Effect/knight_plancrack_b.bmd`; 25-tick life, +15 Z, +90-degree yaw, alpha decay.
- Existing `KnightPlancrackA` / `Stone1/Stone2` families reused.
- Child emissions gated at 25Hz; no repeated render-frame spawning.

## Deliberately unported dependencies
- BOD subtype 2 relies on `m_sTargetIndex` / explicit target-position data and `MODEL_DRAGON_LOWER_DUMMY` / `BITMAP_LIGHT_RED`. The caller bridge does not provide these; subtype 2 is rejected, not misrepresented.
- BOD subtype 1 additionally emits `MODEL_RAKLION_BOSS_CRACKEFFECT`, not yet available as a ClassicFX effect.
- EarthQuake camera shaking is excluded from this visual engine; it must be handled by the scene camera.
- The native `CreateEffect` contract uses incoming Light as a position for subtype 0. This is preserved; adapters must pass the correct original payload.
- No unverified skill adapter. No combat, damage or network packets copied.
- Remote authoring only; Windows build / visual parity not claimed.

## Install
Branch `classicfx-s6-batch11-20261010` has one new commit based on the confirmed Batch 10 main. Cherry-pick or fast-forward after checking clean worktree and HEAD, compile once, then push.
