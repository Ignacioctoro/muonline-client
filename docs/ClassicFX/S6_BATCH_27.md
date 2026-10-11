# ClassicFX Season 6 — Batch 27

Base: `404f5d9e496e10fd460c14755cf6e52cb93ea2d0`. Original MuMain fixed reference: `21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

## New IDs 295–305 (11 unique IDs)

- InfinityArrowCore (visible subtype 1 only), InfinityArrow1–4: original `Skill/arrowsre01.bmd` through `arrowsre05.bmd`. Owner bone 29 follows the core; 1–3 follow owner position with subtype-specific fading. Type 4 is native model-only. For subtype 1 on 1–3, original linked children of type 3 (subtypes 2/3) emit at ticks 40/20.
- BladeSkillModel: `Effect/bladetonedo.bmd`; 10/14 frames, subtype-0 scale decrease and subtype-1 alpha/mesh light driven by remaining lifespan.
- WolfHeadEffect/Effect2: the two native Rage Fighter wolf models, owner bones 27 and 5, native action PlaySpeed required for source cast subtypes; native child model emissions reproduced at reference-tick cadence.
- DownAttackDummyL/R: native L/R BMD path inversion preserved (`down_right_punch` for L, `down_left_punch` for R). Requires caller-supplied native action speed. Owner follow, model bone-0 force-pillar joint and flare on reference ticks.
- DragonKickDummy: `Effect/dragon_kick_dummy.bmd`, 200-frame native maximum lifespan, tracks owner, requires caller-supplied native action speed.

All BMD paths verified in `Data_Broyal`; original identity and movement cross-checked against `ZzzOpenData.cpp`, `ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp`, and `EffectTypes.json`. This is one shared ClassicFX pool/renderer.

## Explicit remaining work

- InfinityArrowCore subtype 0 is a native emitter spawning subtype 1 plus all four BMD variants; not registered as a fake visible BMD. Connect emitter at actual caster callsite.
- WolfHead sources modify player action angles and native movement, and emit pin-light joints; these actor-logic dependencies are not replicated here. The native action PlaySpeed must be supplied by callers.
- DownAttack native frames 3/5/6/8, target-hit bomb and sounds, and automatic R->L chaining require animation frame, target and action-state bridge. The 100-frame life is a native ceiling, NOT a claim of full stamp behavior.
- DragonKick frame 4–5 bone-based ground-wind joints, target particles, and precise cancellation on owner action exit need player action/frame bridge.
- Blade subtype 1 caster angle changes and spark/shiny secondary effects need caller/owner integration.
- Merely registering an ID does not wire any existing skill caller or remove old renderers. Visual/Android verification and real `dotnet build` are pending local integration.

Avoid duplicate live legacy + ClassicFX execution when callers are migrated.
