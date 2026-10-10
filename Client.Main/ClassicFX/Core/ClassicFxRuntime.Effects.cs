// ClassicFX Effect bridge v1. Native references: ZzzEffect.cpp CreateEffect,
// MoveEffects and RenderEffects for MODEL_SWELL_OF_MAGICPOWER / MODEL_ARROWSRE06.
// MonoGame's existing ModelObject is the only BMD renderer.
using System;
using System.Threading.Tasks;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Rendering;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Models;
using Client.Main.Objects;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.ClassicFX.Core
{
    // A named model type; not a fabricated original Main numeric MODEL_* ID.
    // Future types belong here and in the model catalogue below.
    public enum ClassicFxEffectType
    {
        SwellOfMagicPower = 1,
        ArrowsRe06 = 2,
        ShockWave = 3,
        Twlight = 4,
        AirForce = 5,
        SummonerCasting1 = 6,
        SummonerCasting11 = 7,
        SummonerCasting111 = 8,
        SummonerCasting2 = 9,
        SummonerCasting22 = 10,
        SummonerCasting222 = 11,
        SummonerCasting4 = 12,
        SwordForce = 13,
        MagicCircle1 = 14,
        Magic1 = 15,
        MagicCapsule2 = 16,
        Poison = 17,
        MagicZin = 18,
        LightningGround = 19,
        MagicGround = 20,
        MagicGround2 = 21,
        MagicCircleGround = 22,
        DarkLordSkill = 23,
        AliceBuffSkillEffect = 24,
        AliceBuffSkillEffect2 = 25,
        ShockWaveGround01 = 26,
        Wave = 27
    }

    public sealed partial class ClassicFxRuntime
    {
        private struct EffectState
        {
            public ClassicFxEffectType Type;
            public int SubType;
            public ClassicFxOwner Owner;
            public Vector3 Position;
            public Vector3 Angle;
            public Vector3 Light;
            public float Scale;
            public Vector3 Direction;
            public float Velocity;
            public float Alpha;
            public float LifeTime;
            public float BlendMeshLight;
            public int BoneIndex;
            public bool FirstMove;
            // Native EyeRight and PKKey drive ShockWave(14)/Twlight(3)
            // luminous fade independently of the model's Alpha.
            public Vector3 BaseLight;
            public float Phase;
            public byte TriggerMask;
            // Last native tick emitting children for BITMAP_MAGIC+1 subtypes 6/8.
            public int LastChildNativeTick;
            public ClassicFxEffectModelObject ModelView;
        }

        private readonly EffectState[] _effects =
            new EffectState[ClassicFxPools.MaxEffects];

        public int ActiveEffectCount => Pools.Effects.ActiveCount;

        /// <summary>
        /// CreateEffect() bridge. Only accepted original type/subtype pairs
        /// are allocated. Unknown cases do NOT claim to render anything.
        /// This pool owns the state and the corresponding NeffisDev model.
        /// </summary>
        public ClassicFxHandle CreateEffect(
            ClassicFxEffectType type,
            Vector3 position,
            Vector3 angle,
            Vector3 light,
            ClassicFxOwner owner,
            int subType = 0,
            int boneIndex = -1,
            float scale = 1f)
        {
            if (_disposed || !Enabled)
                return ClassicFxHandle.Invalid;

            bool additionalModel = TryGetAdditionalModelDefinition(
                type, subType, out AdditionalEffectModelDefinition modelDefinition);
            bool batch2Model = TryGetBatch2EffectModelDefinition(
                type, subType, out Batch2EffectModelDefinition batch2Definition);
            bool season6Model = TryGetSeason6ModelDefinition(
                type, subType, out Season6ModelDefinition season6Definition);
            bool additionalTerrain = TryGetV5TerrainDefinition(type, subType,
                out V5TerrainEffectDefinition terrainDefinition);
            MagicGround2Definition magicGround2Definition = default;
            bool magicGround2 = type == ClassicFxEffectType.MagicGround2 &&
                TryGetMagicGround2Definition(subType, out magicGround2Definition);
            // Both BITMAP_MAGIC+1 and BITMAP_MAGIC+2 share native CreateEffect().
            MagicGround2Definition magicCircleDefinition = default;
            bool magicCircleGround = type == ClassicFxEffectType.MagicCircleGround &&
                TryGetMagicGround2Definition(subType, out magicCircleDefinition);
            bool nativeGroundV8 = TryGetV8GroundEffectDefinition(type, subType,
                out V8GroundEffectDefinition groundV8Definition);
            // Preserve already-ported Wizardry subtypes ShockWave 14 / Twlight 3.
            bool terrain = (type == ClassicFxEffectType.ShockWave && subType == 14) ||
                           (type == ClassicFxEffectType.Twlight && subType == 3);
            if (magicGround2 && subType == 7 && owner.WorldObject == null)
                return ClassicFxHandle.Invalid; // Native subtype 7 follows Owner.
            if (nativeGroundV8 && groundV8Definition.RequiresOwner &&
                owner.WorldObject == null)
                return ClassicFxHandle.Invalid;
            if (nativeGroundV8 || magicGround2 || magicCircleGround || additionalTerrain)
            {
                // Original terrain bitmaps can be spawned with a null Owner.
                // If present, Owner still must belong to this world.
                if (owner.WorldObject != null &&
                    !ReferenceEquals(owner.WorldObject.World, World))
                    return ClassicFxHandle.Invalid;
            }
            else if (terrain)
            {
                // Native these are Effect objects with texture terrain render,
                // NOT standalone sprites/billboards nor BMD model objects.
                if (owner.WorldObject == null ||
                    !ReferenceEquals(owner.WorldObject.World, World))
                    return ClassicFxHandle.Invalid;
            }
            else if (additionalModel || batch2Model || season6Model)
            {
                // Native model Effects can be unowned. Owner-required
                // variants are checked against their original creation rules.
                if (owner.WorldObject != null &&
                    !ReferenceEquals(owner.WorldObject.World, World))
                    return ClassicFxHandle.Invalid;
                if (type == ClassicFxEffectType.AirForce && owner.WorldObject == null)
                    return ClassicFxHandle.Invalid;
                if (batch2Model && batch2Definition.RequiresOwner &&
                    owner.WorldObject == null)
                    return ClassicFxHandle.Invalid;
                if (season6Model && season6Definition.NeedsOwner &&
                    owner.WorldObject == null)
                    return ClassicFxHandle.Invalid;
            }
            else if (owner.WorldObject is not PlayerObject player ||
                     !ReferenceEquals(player.World, World) ||
                     player.Status != GameControlStatus.Ready)
            {
                return ClassicFxHandle.Invalid;
            }

            string modelPath = null;
            float life;
            float effectScale = scale;
            float effectMeshLight = 1f;
            float effectAlpha = 1f;
            int effectBlendMesh = 0;
            int effectHiddenMesh = -1;
            Vector3 effectDirection = Vector3.Zero;
            float effectVelocity = 0f;
            Vector3 effectPosition = position;
            if (type == ClassicFxEffectType.SwellOfMagicPower && subType == 0)
            {
                modelPath = "Effect/magic_powerup.bmd";
                life = 45f;
            }
            else if (type == ClassicFxEffectType.ArrowsRe06 && subType == 1 && boneIndex >= 0)
            {
                modelPath = "Effect/arrowsre06.bmd";
                life = 40f;
            }
            else if (additionalModel)
            {
                modelPath = modelDefinition.Path;
                life = modelDefinition.LifeTime;
                effectScale = modelDefinition.Scale;
                effectMeshLight = modelDefinition.BlendMeshLight;
            }
            else if (batch2Model)
            {
                modelPath = batch2Definition.Path;
                life = batch2Definition.LifeTime;
                effectScale = batch2Definition.UseCallerScale
                    ? scale : batch2Definition.Scale;
                effectMeshLight = batch2Definition.BlendMeshLight;
                effectBlendMesh = batch2Definition.BlendMesh;
                effectHiddenMesh = batch2Definition.HiddenMesh;
                effectDirection = batch2Definition.Direction;
                effectVelocity = batch2Definition.Velocity;
                effectPosition.Z += batch2Definition.SpawnZ * Clock.FrameFactor;
                if (type == ClassicFxEffectType.DarkLordSkill)
                    angle = new Vector3(
                        MathHelper.ToRadians(45f),
                        MathHelper.ToRadians(subType == 0 ? 45f : -45f),
                        0f);
            }
            else if (season6Model)
            {
                modelPath = season6Definition.Path;
                life = season6Definition.LifeTime;
                effectScale = season6Definition.UseCallerScale ? scale : season6Definition.Scale;
                effectAlpha = season6Definition.Alpha;
                effectMeshLight = season6Definition.MeshLight;
                effectPosition.Z += season6Definition.OffsetZ * Clock.FrameFactor;
                if (season6Definition.WhiteLight) light = Vector3.One;
                // MuMain EffectTypes: ALICE rings reset the yaw to 0 on spawn.
                if (type == ClassicFxEffectType.AliceBuffSkillEffect2 ||
                    (type == ClassicFxEffectType.AliceBuffSkillEffect && subType <= 2))
                    angle.Z = 0f;
            }
            else if (additionalTerrain)
            {
                life = terrainDefinition.LifeTime;
                effectScale = terrainDefinition.UseCallerScale
                    ? scale * terrainDefinition.Scale : terrainDefinition.Scale;
                effectAlpha = terrainDefinition.Alpha;
            }
            else if (magicGround2)
            {
                life = magicGround2Definition.LifeTime;
                effectScale = InitializeMagicGround2Scale(
                    in magicGround2Definition, scale);
                if (magicGround2Definition.RandomAngle)
                    angle.Z = Random.Modulo(360); // degrees: native BITMAP_MAGIC+1:7
            }
            else if (magicCircleGround)
            {
                // Original CreateEffect(BITMAP_MAGIC+2) shares init with +1.
                life = magicCircleDefinition.LifeTime;
                effectScale = InitializeMagicGround2Scale(
                    in magicCircleDefinition, scale);
                if (magicCircleDefinition.RandomAngle)
                    angle.Z = Random.Modulo(360);
            }
            else if (nativeGroundV8)
            {
                life = groundV8Definition.LifeTime;
                effectScale = InitializeV8GroundScale(in groundV8Definition, scale);
                light *= groundV8Definition.LightMultiplier;
            }
            else if (terrain)
            {
                life = 30f; // source CreateEffect(), ShockWave 14 / Twlight 3
            }
            else
                return ClassicFxHandle.Invalid;

            if (!Pools.Effects.TryAcquire(out ClassicFxHandle handle))
                return ClassicFxHandle.Invalid;

            ClassicFxEffectModelObject view = null;
            if (modelPath != null)
            {
                view = new ClassicFxEffectModelObject(modelPath, type);
                view.Position = effectPosition;
                view.Angle = angle;
                view.Scale = effectScale;
                if (additionalModel || batch2Model || season6Model)
                    view.Color = new Color(Vector3.Clamp(light, Vector3.Zero, Vector3.One));
                if (batch2Model)
                {
                    view.BlendMesh = effectBlendMesh;
                    view.HiddenMesh = effectHiddenMesh;
                }
                // Translation from native Effect state to MonoGame BMD presentation.
                view.ApplyNativeRenderState(effectScale, effectAlpha, effectMeshLight, light);
            }
            _effects[handle.Index] = new EffectState
            {
                Type = type,
                SubType = subType,
                Owner = owner,
                Position = effectPosition,
                Angle = angle,
                Light = light,
                BaseLight = light,
                Scale = effectScale,
                Direction = effectDirection,
                Velocity = effectVelocity,
                Alpha = terrain ? 0f : effectAlpha,
                BlendMeshLight = effectMeshLight,
                LifeTime = life,
                BoneIndex = boneIndex,
                FirstMove = true,
                LastChildNativeTick = -1,
                ModelView = view
            };
            try
            {
                // BMD effects are drawn by the existing WorldControl/ModelObject.
                // Terrain Effects are drawn in ClassicFxRuntime.RenderEffects().
                if (view != null)
                {
                    World.Objects.Add(view);
                    _ = view.Load();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClassicFX] CreateEffect {type} failed: {ex.Message}");
                ReleaseEffect(handle);
                return ClassicFxHandle.Invalid;
            }
            return handle;
        }

        public bool IsEffectAlive(ClassicFxHandle handle) =>
            Pools.Effects.IsAlive(handle);

        public bool ReleaseEffect(ClassicFxHandle handle)
        {
            if (!Pools.Effects.IsAlive(handle))
                return false;
            ReleaseEffectAt(handle.Index);
            return true;
        }

        private void ReleaseEffectAt(int index)
        {
            var view = _effects[index].ModelView;
            _effects[index] = default;
            ClassicFxHandle handle = Pools.Effects.GetHandle(index);
            if (handle.IsValid)
                Pools.Effects.Release(handle);
            if (view != null)
            {
                World?.RemoveObject(view);
                view.Dispose();
            }
        }

        // Called by ClassicFxRuntime.Update() before MoveParticles/MoveJoints.
        // Original 25-FPS lifetime semantics remain FPS-independent.
        private void MoveEffects()
        {
            if (_disposed || !Enabled)
                return;
            float f = Clock.FrameFactor;
            if (f <= 0f)
                return;
            for (int i = 0; i < _effects.Length; i++)
            {
                if (!Pools.Effects.IsActive(i))
                    continue;
                ref EffectState e = ref _effects[i];
                bool terrain = e.Type == ClassicFxEffectType.ShockWave ||
                               e.Type == ClassicFxEffectType.Twlight;
                if (IsV5TerrainEffectType(e.Type))
                {
                    if (!MoveV5TerrainEffect(ref e, f))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (e.Type == ClassicFxEffectType.MagicGround2)
                {
                    MoveMagicGround2(ref e, f);
                }
                else if (e.Type == ClassicFxEffectType.MagicCircleGround)
                {
                    // MuMain +2 has no Move handler: the shared pool ages it.
                }
                else if (IsV8GroundEffectType(e.Type, e.SubType))
                {
                    if (!MoveV8GroundEffect(ref e, f))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (terrain)
                {
                    // Terrain effects can outlive their BMD owner: preserve
                    // last known ground position if owner has disappeared.
                    MoveTerrainEffect(ref e, f);
                }
                else
                {
                    if (e.ModelView == null ||
                        e.ModelView.Status == GameControlStatus.Disposed ||
                        e.ModelView.Status == GameControlStatus.Error)
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }

                    if (IsAdditionalModelEffectType(e.Type))
                    {
                        if (!MoveAdditionalModelEffect(ref e, f))
                        {
                            ReleaseEffectAt(i);
                            continue;
                        }
                    }
                    else if (IsBatch2EffectModelType(e.Type))
                    {
                        if (!MoveBatch2EffectModel(ref e, f))
                        {
                            ReleaseEffectAt(i);
                            continue;
                        }
                    }
                    else if (IsSeason6ModelType(e.Type))
                    {
                        if (!MoveSeason6Model(ref e, f))
                        {
                            ReleaseEffectAt(i);
                            continue;
                        }
                    }
                    else
                    {
                        if (e.Owner.WorldObject is not PlayerObject player ||
                            player.Status != GameControlStatus.Ready ||
                            player.IsDead ||
                            !ReferenceEquals(player.World, World))
                        {
                            ReleaseEffectAt(i);
                            continue;
                        }

                        e.Position = player.WorldPosition.Translation;
                        if (e.Type == ClassicFxEffectType.SwellOfMagicPower)
                        {
                            MoveSwellOfMagicPower(ref e, player);
                        }
                        else if (e.Type == ClassicFxEffectType.ArrowsRe06)
                        {
                            if (!TryPlayerBonePosition(player, e.BoneIndex, out Vector3 pos))
                                continue;
                            e.Position = pos;
                            if (e.LifeTime >= 15f)
                                e.Scale *= MathF.Pow(1.05f, f);
                            else
                                e.Scale *= MathF.Pow(0.95f, f);
                            ClassicFxOwner source = ClassicFxOwner.FromWorldObject(e.ModelView);
                            CreateSprite(ClassicTextureIds.BitmapLight, pos,
                                e.Scale, e.Light, source);
                            CreateSprite(ClassicTextureIds.BitmapLight, pos,
                                e.Scale * 0.8f, e.Light, source);
                            if (e.LifeTime <= 10f)
                                e.Alpha *= MathF.Pow(0.95f, f);
                        }
                    }
                    e.ModelView.Position = e.Position;
                    e.ModelView.Angle = e.Angle;
                    e.ModelView.ApplyNativeRenderState(e.Scale, e.Alpha, e.BlendMeshLight, e.Light);
                }
                e.LifeTime -= f;
                if (e.LifeTime <= 0f)
                    ReleaseEffectAt(i);
            }
        }

        private void MoveSwellOfMagicPower(ref EffectState e, PlayerObject owner)
        {
            // Native invokes these three FX pulses at LifeTime 45, 35, 25.
            // Track consumed thresholds: a 60-FPS frame can visit the same
            // integer LifeTime more than once, so avoid duplicate pulses.
            if (e.LifeTime <= 45f && (e.TriggerMask & 1) == 0)
            {
                e.TriggerMask |= 1;
                EmitWizardryGroundPulse(ref e);
            }
            if (e.LifeTime <= 35f && (e.TriggerMask & 2) == 0)
            {
                e.TriggerMask |= 2;
                EmitWizardryGroundPulse(ref e);
            }
            if (e.LifeTime <= 25f && (e.TriggerMask & 4) == 0)
            {
                e.TriggerMask |= 4;
                EmitWizardryGroundPulse(ref e);
            }

            // Main MoveEffects(MODEL_SWELL_OF_MAGICPOWER), subtype 0:
            // 45 frames; hand models at 45; purple 2LINE_GHOST during >=30;
            // body sprites in final 20 frames; mesh fade in final 20.
            if (e.FirstMove)
            {
                e.FirstMove = false;
                var source = ClassicFxOwner.FromWorldObject(owner);
                Vector3 purple = new Vector3(0.2f, 0.2f, 0.9f);
                if (TryPlayerBonePosition(owner, 28, out Vector3 right))
                    CreateEffect(ClassicFxEffectType.ArrowsRe06,
                        right, e.Angle, purple, source, 1, 28);
                if (TryPlayerBonePosition(owner, 37, out Vector3 left))
                    CreateEffect(ClassicFxEffectType.ArrowsRe06,
                        left, e.Angle, purple, source, 1, 37);
                Console.WriteLine("[ClassicFX] Effect MODEL_SWELL_OF_MAGICPOWER: 45 frames");
            }

            if (e.LifeTime >= 30f)
            {
                // Main creates two per frame, each FPS checked.
                ClassicFxOwner source = ClassicFxOwner.FromWorldObject(e.ModelView);
                Vector3 purple = new Vector3(0.3f, 0.2f, 0.9f);
                for (int j = 0; j < 2; j++)
                {
                    CreateJointFpsChecked(ClassicTextureIds.Bitmap2LineGhost,
                        e.Position, e.Position, e.Angle, 1, source,
                        20f + System.Random.Shared.Next(10), priorColor: purple);
                }
            }
            if (e.LifeTime <= 20f)
            {
                Vector3 light = new Vector3(0.7f, 0.3f, 0.9f) *
                                (e.LifeTime * 0.05f);
                Matrix[] bones = owner.GetBoneTransforms();
                if (bones != null)
                {
                    Matrix world = owner.WorldPosition;
                    ClassicFxOwner source = ClassicFxOwner.FromWorldObject(e.ModelView);
                    for (int b = 0; b < bones.Length; b++)
                        CreateSprite(ClassicTextureIds.BitmapLight,
                            (bones[b] * world).Translation, 1.5f, light, source);
                }
                e.BlendMeshLight *= MathF.Pow(0.86f, Clock.FrameFactor);
            }
        }

        private void EmitWizardryGroundPulse(ref EffectState source)
        {
            // Source ZzzEffect.cpp: 2x ShockWave 14 at Scale=5,
            // 1x Twlight 3 at Scale=6. Their native EyeRight = Light.
            if (source.ModelView == null)
                return;
            ClassicFxOwner owner = ClassicFxOwner.FromWorldObject(source.ModelView);
            Vector3 light = new Vector3(0.4f, 0.3f, 0.9f);
            // MonoGame character angles use radians; Effect terrain rotation
            // uses classic degrees.
            Vector3 angle = new Vector3(0f, 0f,
                MathHelper.ToDegrees(source.Angle.Z));
            int createdShockWaves = 0;
            for (int n = 0; n < 2; n++)
            {
                if (CreateEffect(ClassicFxEffectType.ShockWave,
                    source.Position, angle, light, owner, subType: 14, scale: 5f).IsValid)
                    createdShockWaves++;
            }
            bool createdTwlight = CreateEffect(ClassicFxEffectType.Twlight,
                source.Position, angle, light, owner, subType: 3, scale: 6f).IsValid;

            // Three messages per cast at most. Distinguishes failed creation
            // from a missing texture; avoids per-frame console spam.
            Console.WriteLine(
                $"[ClassicFX] Wizardry pulse: ShockWave {createdShockWaves}/2 " +
                $"(texture={Textures.TryGet(ClassicTextureIds.BitmapShockWave, out _)}), " +
                $"Twlight {(createdTwlight ? 1 : 0)}/1 " +
                $"(texture={Textures.TryGet(ClassicTextureIds.BitmapTwlight, out _)}).");
        }

        private void MoveTerrainEffect(ref EffectState e, float f)
        {
            // Source MoveHandlers.cpp: Move_BITMAP_SHOCK_WAVE/Move_BITMAP_TWLIGHT.
            // Both of Wizardry's subtypes use the same scale/fade logic.
            WorldObject owner = e.Owner.WorldObject;
            if (owner != null && ReferenceEquals(owner.World, World) &&
                owner.Status != GameControlStatus.Disposed)
                e.Position = owner.WorldPosition.Translation;

            e.Scale = MathF.Max(0f, e.Scale - 0.15f * f);
            if (e.Type == ClassicFxEffectType.Twlight)
                e.Angle.Z += 10f * f;

            if (e.LifeTime >= 20f)
            {
                e.Alpha += 0.1f * f;
                e.Phase += f;
                e.Light = e.BaseLight * (e.Phase * 0.1f);
            }
            else if (e.LifeTime <= 10f)
            {
                e.Phase -= f;
                e.Alpha -= 0.1f * f;
                e.Light = e.BaseLight * (e.Phase * 0.1f);
            }
        }

        /// <summary>
        /// Native RenderTerrainAlphaBitmap for ShockWave 14 and Twlight 3.
        /// Uses one shared GPU-batched billboard pipeline; geometry is a
        /// tessellated XY plane following the existing MonoGame terrain.
        /// Mesh BMD effects continue through WorldControl.RenderObjects().
        /// </summary>
        public void RenderEffects()
        {
            if (_disposed || !Enabled || _billboardRenderer == null ||
                World?.Terrain == null)
                return;

            _billboardRenderer.Begin();
            for (int i = 0; i < _effects.Length; i++)
            {
                if (!Pools.Effects.IsActive(i)) continue;
                ref EffectState e = ref _effects[i];
                if (e.Type == ClassicFxEffectType.MagicGround2)
                {
                    RenderMagicGround2(ref e);
                    continue;
                }
                if (e.Type == ClassicFxEffectType.MagicCircleGround)
                {
                    RenderMagicCircleGround(ref e);
                    continue;
                }
                if (IsV5TerrainEffectType(e.Type))
                {
                    RenderV5TerrainEffect(ref e);
                    continue;
                }
                int textureId = e.Type switch
                {
                    ClassicFxEffectType.ShockWave => ClassicTextureIds.BitmapShockWave,
                    ClassicFxEffectType.Twlight => ClassicTextureIds.BitmapTwlight,
                    _ => -1
                };
                if (textureId < 0 || e.Scale <= 0f ||
                    !Textures.TryGet(textureId, out ClassicTextureResource tex))
                    continue;

                QueueTerrainEffect(ref e, tex);
            }
            _billboardRenderer.End();
        }

        private void QueueTerrainEffect(ref EffectState e, ClassicTextureResource texture,
            float? scaleOverride = null, Vector3? lightOverride = null,
            float? angleZOverride = null,
            ClassicBlendMode blendOverride = ClassicBlendMode.Glow)
        {
            // Main: RenderTerrainAlphaBitmap(), ZzzLodTerrain.cpp.
            // Each quad sits on an ACTUAL 100-unit tile; its four Z values
            // match TerrainRenderer's visual mesh, including TWFlags.Height.
            // Reuses the existing batched ClassicBillboardRenderer on Android.
            float tileScale = Constants.TERRAIN_SCALE;
            float fx = e.Position.X / tileScale;
            float fy = e.Position.Y / tileScale;
            int cellX = (int)fx;
            int cellY = (int)fy;
            float size = scaleOverride ?? e.Scale;
            if (size <= 0f) return;

            // Faithful original tile bounds and texcoord derivation.
            int extent = (int)size + 1;
            float texU = (cellX - fx) + 0.5f * size;
            float texV = (cellY - fy) + 0.5f * size;
            float invSize = 1f / size;
            float radians = MathHelper.ToRadians(-(angleZOverride ?? e.Angle.Z));
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);
            int lastTile = Constants.TERRAIN_SIZE - 1;

            for (int dy = -extent; dy <= extent; dy++)
            {
                int tileY = cellY + dy;
                if (tileY < 0 || tileY >= lastTile) continue;

                for (int dx = -extent; dx <= extent; dx++)
                {
                    int tileX = cellX + dx;
                    if (tileX < 0 || tileX >= lastTile) continue;

                    Vector3 p0 = EffectTerrainTilePoint(tileX, tileY);
                    Vector3 p1 = EffectTerrainTilePoint(tileX + 1, tileY);
                    Vector3 p2 = EffectTerrainTilePoint(tileX + 1, tileY + 1);
                    Vector3 p3 = EffectTerrainTilePoint(tileX, tileY + 1);

                    float u0 = texU + dx;
                    float v0 = texV + dy;
                    Vector2 uv0 = EffectTerrainTileUv(u0, v0, invSize, cos, sin);
                    Vector2 uv1 = EffectTerrainTileUv(u0 + 1f, v0, invSize, cos, sin);
                    Vector2 uv2 = EffectTerrainTileUv(u0 + 1f, v0 + 1f, invSize, cos, sin);
                    Vector2 uv3 = EffectTerrainTileUv(u0, v0 + 1f, invSize, cos, sin);

                    _billboardRenderer.QueueWorldQuad(texture,
                        p0, p1, p2, p3, uv0, uv1, uv2, uv3,
                        lightOverride ?? e.Light, blendOverride, ClassicDepthMode.ReadOnly);
                }
            }
        }

        private Vector3 EffectTerrainTilePoint(int tileX, int tileY)
        {
            float x = tileX * Constants.TERRAIN_SCALE;
            float y = tileY * Constants.TERRAIN_SCALE;
            // DepthRead + 2 world units prevents z fighting on flat tiles.
            float z = World.Terrain.RequestTerrainRenderHeight(x, y) + 2f;
            return new Vector3(x, y, z);
        }

        private static Vector2 EffectTerrainTileUv(
            float u, float v, float invSize, float cos, float sin)
        {
            // Original: rotate texcoords around center (0.5, 0.5).
            float x = u * invSize - 0.5f;
            float y = v * invSize - 0.5f;
            return new Vector2(
                x * cos - y * sin + 0.5f,
                x * sin + y * cos + 0.5f);
        }

        private static bool TryPlayerBonePosition(PlayerObject owner,
            int boneIndex, out Vector3 position)
        {
            Matrix[] bones = owner.GetBoneTransforms();
            if (bones == null || (uint)boneIndex >= (uint)bones.Length)
            {
                position = Vector3.Zero;
                return false;
            }
            position = (bones[boneIndex] * owner.WorldPosition).Translation;
            return true;
        }

        private void ClearEffectStorage()
        {
            for (int i = 0; i < _effects.Length; i++)
            {
                var view = _effects[i].ModelView;
                _effects[i] = default;
                if (view == null)
                    continue;
                World?.RemoveObject(view);
                view.Dispose();
            }
        }
    }

    /// <summary>
    /// No second BMD renderer: meshes, animation and blend go through
    /// NeffisDev's existing ModelObject and WorldControl passes.
    /// </summary>
    internal sealed class ClassicFxEffectModelObject : ModelObject
    {
        private readonly string _bmdPath;
        // This is a model-asset renderer profile, not skill-logic scale tuning.
        // Values are grounded in the previous DarkLordCriticalHandEffect renderer.
        // Disable for a side-by-side A/B comparison of the generic model bridge.
        private const bool EnableDarkLordModelProfile = true;
        private readonly bool _useDarkLordModelProfile;
        private const float DarkLordRenderMaxScale = 0.65f;
        private const float DarkLordRenderOpacity = 0.48f;
        private const float DarkLordRenderBlendIntensity = 0.70f;

        public ClassicFxEffectModelObject(string bmdPath, ClassicFxEffectType type)
        {
            _bmdPath = bmdPath;
            _useDarkLordModelProfile = EnableDarkLordModelProfile &&
                type == ClassicFxEffectType.DarkLordSkill;
            IsTransparent = true;
            AffectedByTransparency = true;
            BlendState = BlendState.Additive;
            BlendMesh = 0;
            BlendMeshLight = 1f;
            DepthState = DepthStencilState.DepthRead;
            RenderShadow = false;
            LightEnabled = false;
            UseSunLight = false;
            ContinuousAnimation = !_useDarkLordModelProfile;
            AnimationSpeed = 25f;
            Color = type == ClassicFxEffectType.SwellOfMagicPower
                ? new Color(0.7f, 0.4f, 0.9f)
                : new Color(0.2f, 0.2f, 0.9f);
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-250f, -250f, -200f),
                new Vector3(250f, 250f, 350f));
            Interactive = false;
        }

        // Classic spell BMDs may require a different shader/material path
        // than world actors. The first measured profile concerns DarkLordSkill.
        protected override bool AllowDynamicLightingShader =>
            !_useDarkLordModelProfile;

        // Keep original EffectState math unmodified. Translation happens only
        // when state enters the MonoGame renderer. All non-profiled models
        // retain the previous ClassicFX presentation, exactly as before.
        public void ApplyNativeRenderState(float nativeScale,
            float nativeAlpha, float nativeBlendMeshLight, Vector3 nativeLight)
        {
            if (_useDarkLordModelProfile)
            {
                // The prior ModelObject cast uses Light=(1,.6,.3), NOT just Color.
                // When the dynamic shader is disabled, CPU skinning obtains
                // vertex illumination from ModelObject.Light. Zero = black.
                // Keep the native source light and do not double-tint it.
                Light = nativeLight;
                Color = Microsoft.Xna.Framework.Color.White;
                // Previous functioning DarkLordCriticalHandEffect parameters.
                // This is a temporary renderer calibration for this BMD asset,
                // not a change to native lifespan or growth physics.
                Scale = MathF.Min(nativeScale, DarkLordRenderMaxScale);
                Alpha = MathHelper.Clamp(
                    nativeAlpha * DarkLordRenderOpacity, 0f, 1f);
                BlendMeshLight = nativeBlendMeshLight * DarkLordRenderBlendIntensity;
                BlendMesh = -1; // Previous ModelObject default, not mesh 0.
            }
            else
            {
                Scale = nativeScale;
                Alpha = MathHelper.Clamp(nativeAlpha, 0f, 1f);
                BlendMeshLight = nativeBlendMeshLight;
            }
        }

        public override async Task Load()
        {
            try
            {
                Model = await BMDLoader.Instance.Prepare(_bmdPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClassicFX] BMD {_bmdPath}: {ex.Message}");
                Status = GameControlStatus.Error;
                return;
            }
            await base.Load();
        }
    }
}
