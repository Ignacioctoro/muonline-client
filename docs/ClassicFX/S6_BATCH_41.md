# ClassicFX Season 6 — Batch 41

MuMain C++ pinned: `21728b1e5b03e0763b38ef9e23f79645e0df7ad2`; Broyal base `0c9d2844de941db732b1f74bccdea538e6fbc416`.
Source-only focused changes based on `ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp`, `EffectTypes.json`, and `ZzzOpenData.cpp`. No general re-audit.

| Broyal ID | Type | Native original |
|---:|---|---|
|425|TraceEnergyJointCarrier|MODEL_EFFECT_TRACE, subtype 0|
|426|UmbrellaDeathRingCarrier|MODEL_EFFECT_UMBRELLA_DIE, subtype 0|
|427|GuardianDefenderAttackCarrier|MODEL_EFFECT_EG_GUARDIANDEFENDER_ATTACK2, subtype 0|
|428|StreamBreathFireCarrier|MODEL_1_STREAMBREATHFIRE, subtype 0|
|429|ThunderNapinCore|MODEL_EFFECT_THUNDER_NAPIN_ATTACK_1, subtype 0|
|430|ThunderNapinScatter|MODEL_EFFECT_THUNDER_NAPIN_ATTACK_1, subtype 1|
|431|SkillFissureCarrier|MODEL_SKILL_FISSURE, subtype 0|
|432|SakuraItemEffectModel|MODEL_EFFECT_SKURA_ITEM, subtypes 0/1|
|433|FenrirDamageRed|MODEL_FENRIR_SKILL_DAMAGE, subtype 1|
|434|FenrirDamageBlue|MODEL_FENRIR_SKILL_DAMAGE, subtype 2|
|435|FenrirDamageGreen|MODEL_FENRIR_SKILL_DAMAGE, subtype 3|

All logical variant types accept Broyal caller subtype 0; the original subtype is encoded by the type for Thunder and Fenrir. Sakura retains both original subtypes 0/1.

## Runtime

- `ClassicFxRuntime.Effects.cs`: shared allocation/initialization, post-acquire children, pool movement dispatch, owner validation. No additional renderer.
- Actual Sakura BMD `Effect/cherryblossom/Skura_iteam_event.bmd`, verified in `Data_Broyal`, uses existing Season6ModelDefinition and ModelObject; sampled bones 1/2 provide particle and sprite emitters.
- Trace joint carries `ClassicFxOwner.FromClassicFx(handle)`; Umbrella and Fissure apply one-shot threshold masks; Thunder emits existing `BITMAP_LIGHTNING_MEGA1..3` particles and `BITMAP_LIGHT` sprites; Guardian uses shockwave sprite and native fire particle; Fenrir emits ten energy particles from randomized real owner bones.

## Boundaries

- Original sound hooks, render-per-display-frame (now reference-tick child emissions), nonvisual combat checks and some animation state gates remain unported.
- Sakura depends on the existing asynchronous model loader. If its bones are not ready, emissions at those bones are skipped, not fabricated. Render action/animation details require in-game check.
- Fissure child BMDs use the stable world owner because the logical emitter is released immediately after the final spawn; exact animation phases and terrain interactions remain to verify.
- Source's random FPS checks are approximated by reference-clock emissions; Android draw budgets must be validated in gameplay.
- No claim of completed parity or compilation. Build and push require local Windows `dotnet build`.


Static tick threshold arrays are allocated once, not per effect update (Windows/Android).
