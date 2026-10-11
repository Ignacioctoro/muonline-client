# ClassicFX S6 Batch 43 — Native effects and gaps

Reference: `MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.
Base: `5dc6f005d8be34b3e5b381c667ba885755305b7c`.

## New IDs (437–439)

- **437 FirePlusOneEmitter** — native `BITMAP_FIRE+1`, 10 ticks, rotated -60 / elevated +130 initialization; emits the original `BITMAP_FIRE+1` particle subtype 1 on reference ticks. Existing `Effect/Fire02.OZJ` texture.
- **438 DragonLoreLava** — native `BITMAP_LAVA`; action-bound lifetime, bones 36/28, original `BITMAP_LAVA` joints, paired shockwaves, FireHik / CursedLich / Spark particles, owner/action validation. Existing `Effect/lava.OZJ` and the pooled lava joint family.
- **439 HolyArrowJointCarrier** — native `MODEL_ARROW_HOLY`: invisible carrier, three/four `BITMAP_FLARE` joints (subtype 25/11), source offset, 30-tick life, angle progression and one-shot Piercing phase at tick 13.

## Limitations

- **No fabricated history**: the C++ Dragon Lore emitter samples previous skeleton poses between frames. MonoGame exposes the current pose, so the bridge emits only the two real bone anchors each reference tick. Original density/continuity will differ without a historical bone-pose sampler.
- **No hit detection from the visuals**: `CheckClientArrow`, collision, damage, audio and associated server packet flow remain the gameplay layer's responsibility. This batch only ports the visible Holy Arrow carrier/children.
- `BITMAP_FIRE_RED`, unavailable BMD-only roots (Big Meteo, Boss Attack, Staff of Destruction, etc.) and renamed entries already represented by IDs 1–436 are **not** added as placeholder visual types.
- Does not replace existing gameplay call sites automatically. No parallel renderer, no per-frame allocated arrays.
- **Compilation is not certified**: GitHub connector cannot run the user's .NET 10/MonoGame Windows build; local compilation is mandatory before pushing `classicfx-nova-pilot`.
