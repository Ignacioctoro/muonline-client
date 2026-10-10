# ClassicFX S6 Batch 14 — native terrain effects (Fenrir / TwinTail / Cloud)

Base: `classicfx-nova-pilot@6eb600d5bb27253a086fbc71e2cdf3515bce1ca5`
Pinned native: `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`, `ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp`, `ZzzOpenData.cpp`, `_TextureIndex.h`.
Data: `Ignacioctoro/Data_Broyal/Data/Effect/eff_lightinga01..05.OZJ`.

## Native types added

- `FenrirFootThunder` (5 subtypes 0–4): initial 200 native ticks, timed five-texture native bitmap sequence (`32251..32255`), 200ms frame steps, 0.05 alpha decrement/tick and subtype-specific fading RGB channels. Rendered with fixed terrain scale 0.6, not a BMD.
- `TwinTail` (3 subtypes 0–2): original 200/50 tick lifetimes, subtype 0 pulse alternating every 1000ms and scaled Spark+1 terrain; subtypes 1/2 scale 3.5, animated cloud terrain rotation using actual WorldTimeMilliseconds, native alpha/RGB fades.
- `CloudGround` (BITMAP_CLOUD, native subtype 0): 60-tick lifetime, random ground yaw, native 0.03/tick growth and exponential light fade.

The five native Fenrir frame IDs are explicitly mapped to the original Effect/eff_lightinga01..05.jpg virtual filenames with OZJ files present in Data_Broyal. All use existing ClassicFX Effect pool and batched terrain renderer; no extra BMD renderer, no additional SpriteBatch or gameplay actions.

## Scope / limitations

- These effect-entry APIs can be called by future Fenrir/scene adapters; existing mount movement code is not replaced in this batch. Rendering both old and new copies must be prevented when the scene adapter is wired.
- The native shared Luminosity multiplier is external to these effects. CloudGround currently uses its own native light decay but not an invented scene luminosity value.
- Timed bitmap steps use accumulated 25Hz reference ticks (5 ticks ~= 200 ms, 25 ticks ~= 1 second), independent of Android render framerate.
- Batch14 does not claim to port MODEL_FALL_STONE_EFFECT: its original BMD load path was not established in the pinned asset catalog; substituting an arbitrary stone would break parity.
- Remote authoring: GitHub commit and structural checks only. Windows .NET build and visual verification must be performed on the user's machine.

## Install

Fast-forward `classicfx-nova-pilot` with `classicfx-s6-batch14-20261010` only when the current HEAD is the base SHA above; build `Client.Main/Client.Main.csproj`, then push on success.
