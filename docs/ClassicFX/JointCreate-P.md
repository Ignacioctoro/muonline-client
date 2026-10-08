# ClassicFX CreateJoint P (MuMain)

Base: `classicfx-engine-2026-10`, commit `7ac945a966bf645062cef62bfe0bc0ea3eef069a`.

## Included

- Native initial four tail vertices (or disabled first tail for `SCOLPION_TAIL` and specified `JOINT_ENERGY` subtypes).
- All native `CreateJoint` branches for `BITMAP_SCOLPION_TAIL`, `BITMAP_JOINT_ENERGY`, `BITMAP_JOINT_HEALING` (source lines ~208–602).
- Lifetimes, colors, alpha blend minus, geometry parameters, random number calls, TexType replacements, variable MaxTails and target references.
- Optional `JointEyeResolver` to supply *actual* six OBJECT Eye positions. Eye values are not guessed from bone indices. `UpdateJointTargetPosition` falls back to owner Position as it does in the native Main; direct Eye copies remain at call position if there is no eye resolver.
- Requests for *unported* joint types/subtypes are safely rejected and their slots immediately released (until subsequent constructor blocks are added).

## Not included

- `CreateJoint` for later families `BITMAP_2LINE_GHOST`, `BITMAP_JOINT_SPIRIT` etc. (next block Q).
- `MoveJoints` and `RenderJoints` — do not test this block as a visible effect.
- Integration for OWNER Eye positions. Provide real model data when available; do not assign guessed bones.
- `CreateEffect` called by ghost joints, blocked until Effect port.

## Install

From the root `C:\muonline-client-main`:

```powershell
git apply --check .\classicfx_joint_create_p.patch
git apply .\classicfx_joint_create_p.patch
dotnet build .\Client.Main\Client.Main.csproj
```

Then:

```powershell
git add Client.Main/ClassicFX/Data/ClassicTextureIds.cs
git add Client.Main/ClassicFX/Core/ClassicFxRuntime.Joints.cs
git add Client.Main/ClassicFX/Core/ClassicFxRuntime.JointCreate.P.cs
git add docs/ClassicFX/JointCreate-P.md
git commit -m "classicfx_joint_create_P_energy_healing"
git push
```
