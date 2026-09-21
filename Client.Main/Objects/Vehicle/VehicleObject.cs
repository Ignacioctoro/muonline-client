using Client.Main.Content;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Client.Main.Objects.Effects;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;

namespace Client.Main.Objects.Vehicle;

public class VehicleObject : ModelObject
{
    // Default vehicle animation indices (some vehicles override via VehicleDefinition)
    public const int DefaultAnimationIdle = 0;
    public const int DefaultAnimationRun = 2;
    public const int DefaultAnimationSkill = 4;
    private const int FenrirBoneFront1 = 22;
    private const int FenrirBoneFront2 = 28;
    private const int FenrirBoneRear1 = 36;
    private const int FenrirBoneRear2 = 44;

    private readonly FenrirFootThunderEffect[] _fenrirFootEffects =
        new FenrirFootThunderEffect[4];
    private const int FenrirThunderPoolSize = 8;

    private const float FenrirThunderSpawnInterval = 0.07f;

    private readonly FenrirThunderEffect[] _fenrirThunderEffects =
        new FenrirThunderEffect[FenrirThunderPoolSize];
    // Fenrir face lights:
    // 4 sprites for the eyes (2 overlapping sprites per eye)
    // 2 sprites for the jaw.
    private readonly LightEffect[] _fenrirEyeLights =
        new LightEffect[4];

    private readonly LightEffect[] _fenrirJawLights =
        new LightEffect[2];
    private readonly Spark03Effect _fenrirSkillSpark;

    private float _fenrirSkillSparkTimer;

    private const float FenrirSkillSparkInterval = 0.06f;
    private float _fenrirThunderSpawnTimer;

    private int _fenrirThunderPoolCursor;

    private int _lastFenrirAction = -1;
    private float _lastFenrirAnimationFrame = -1f;

    private bool _fenrirWalkTriggered;
    private bool _fenrirFrontTriggered;
    private bool _fenrirRearTriggered;

    // Vehicle IDs that have root motion in their run animation and need position locking
    private static readonly HashSet<int> VehiclesWithRootMotion = new()
    {
        7,  // Rider 01 (Uniria/Dinorant)
        8,  // Rider 02
        27, // Pon Up Ride
        28, // Pon Ride
        22, // Griffs Up Ride
        23, // Griffs Ride
        30, // Rippen Up Ride
        31, // Rippen Ride
    };

    private short itemIndex = -1;
    private int _itemIndexChangeVersion;

    public short ItemIndex
    {
        get => itemIndex;
        set
        {
            if (itemIndex == value)
                return;

            itemIndex = value;

            // Cada cambio de ItemIndex obtiene una nueva versión.
            // Una carga anterior no podrá sobrescribir una más reciente.
            int version = Interlocked.Increment(ref _itemIndexChangeVersion);

            _ = OnChangeIndexAsync(value, version);
        }
    }

    /// <summary>
    /// Checks whether an asynchronous vehicle load still corresponds
    /// to the currently requested ItemIndex.
    /// </summary>
    private bool IsCurrentItemIndex(short requestedIndex, int version)
    {
        return requestedIndex == itemIndex
            && version == Volatile.Read(ref _itemIndexChangeVersion);
    }

    /// <summary>
    /// The vertical offset to apply to the rider when mounted on this vehicle.
    /// Retrieved from VehicleDefinition when the vehicle is loaded.
    /// </summary>
    public float RiderHeightOffset { get; private set; } = 0f;

    /// <summary>
    /// The animation speed multiplier for this vehicle.
    /// Retrieved from VehicleDefinition when the vehicle is loaded.
    /// </summary>
    public float AnimationSpeedMultiplier { get; private set; } = 1.0f;

    private float idleAnimationSpeedMultiplier = 1.0f;
    private float runAnimationSpeedMultiplier = 1.0f;
    private float skillAnimationSpeedMultiplier = 1.0f;

    private int idleActionIndex = DefaultAnimationIdle;
    private int runActionIndex = DefaultAnimationRun;
    private int skillActionIndex = DefaultAnimationSkill;

    private Dictionary<int, float> actionPlaySpeedOverrides;

    public VehicleObject()
    {
        RenderShadow = true;
        IsTransparent = true;
        AffectedByTransparency = true;
        BlendState = BlendState.AlphaBlend;
        BlendMesh = -1;
        BlendMeshState = BlendState.Additive;
        Alpha = 1f;
        LinkParentAnimation = false;
        AnimationSpeed = 25f;

        for (int i = 0; i < _fenrirFootEffects.Length; i++)
        {
            var effect = new FenrirFootThunderEffect();

            _fenrirFootEffects[i] = effect;

            Children.Add(effect);
        }

        for (int i = 0; i < _fenrirThunderEffects.Length; i++)
        {
            var effect = new FenrirThunderEffect();

            _fenrirThunderEffects[i] = effect;

            Children.Add(effect);
        }

        // Fenrir eyes.
        // MU renders two identical BITMAP_LIGHT sprites
        // on each eye to intensify the glow.
        for (int i = 0; i < _fenrirEyeLights.Length; i++)
        {
            var light = new LightEffect
            {
                Hidden = true,
                Alpha = 1f
            };

            _fenrirEyeLights[i] = light;

            Children.Add(light);
        }

        // Fenrir jaw.
        // Original MU renders two different-sized lights
        // at the same jaw position.
        for (int i = 0; i < _fenrirJawLights.Length; i++)
        {
            var light = new LightEffect
            {
                Hidden = true,
                Alpha = 1f
            };

            _fenrirJawLights[i] = light;

            Children.Add(light);
        }

        // Fenrir skill spark.
        _fenrirSkillSpark =
            new Spark03Effect
            {
                Hidden = true,
                Alpha = 1f,
                Scale = 0.84f,
                Light =
                    new Vector3(
                        1f,
                        0f,
                        0f)
            };

        Children.Add(
            _fenrirSkillSpark);
    }
    /// <summary>
    /// Loads the vehicle corresponding to a specific ItemIndex.
    ///
    /// requestedIndex and version are captured when ItemIndex changes.
    /// If ItemIndex changes again while the model is loading,
    /// this operation is discarded and cannot overwrite the newer vehicle.
    /// </summary>
    private async Task OnChangeIndexAsync(short requestedIndex, int version)
    {
        // This request may already have been replaced before execution starts.
        if (!IsCurrentItemIndex(requestedIndex, version))
            return;

        // No vehicle.
        if (requestedIndex < 0)
        {
            Model = null;

            RiderHeightOffset = 0f;

            AnimationSpeedMultiplier = 1.0f;
            idleAnimationSpeedMultiplier = 1.0f;
            runAnimationSpeedMultiplier = 1.0f;
            skillAnimationSpeedMultiplier = 1.0f;

            idleActionIndex = DefaultAnimationIdle;
            runActionIndex = DefaultAnimationRun;
            skillActionIndex = DefaultAnimationSkill;

            actionPlaySpeedOverrides = null;

            return;
        }

        // IMPORTANT:
        // Use requestedIndex instead of the mutable itemIndex field.
        VehicleDefinition riderDefinition =
            VehicleDatabase.GetVehicleDefinition(requestedIndex);

        if (riderDefinition == null)
            return;

        string modelPath = riderDefinition.TexturePath;

        // Load into a local variable first.
        // Do not modify Model until we know this request is still current.
        var newModel = await BMDLoader.Instance.Prepare(
            Path.Combine("Skill", modelPath));

        // ItemIndex could have changed while BMDLoader was awaiting.
        // If so, this model is obsolete and must not be applied.
        if (!IsCurrentItemIndex(requestedIndex, version))
            return;

        // From this point onward this request is still the current vehicle.
        RiderHeightOffset = riderDefinition.RiderHeightOffset;

        AnimationSpeedMultiplier =
            riderDefinition.AnimationSpeedMultiplier;

        AnimationSpeed =
            riderDefinition.AnimationSpeed;

        idleAnimationSpeedMultiplier =
            riderDefinition.IdleAnimationSpeedMultiplier;

        runAnimationSpeedMultiplier =
            riderDefinition.RunAnimationSpeedMultiplier;

        skillAnimationSpeedMultiplier =
            riderDefinition.SkillAnimationSpeedMultiplier;

        idleActionIndex =
            riderDefinition.IdleActionIndex;

        runActionIndex =
            riderDefinition.RunActionIndex;

        skillActionIndex =
            riderDefinition.SkillActionIndex;

        actionPlaySpeedOverrides =
            riderDefinition.ActionPlaySpeedOverrides;

        // Only the most recent ItemIndex request is allowed
        // to replace the current model.
        Model = newModel;

        if (Model == null)
        {
            Status = GameControlStatus.Error;
            return;
        }

        if (Status == GameControlStatus.Error)
        {
            Status = GameControlStatus.Ready;
        }

        // Apply animation speed multiplier to all actions.
        ApplyAnimationSpeedMultiplier();

        // For vehicles with root motion in their animations,
        // lock positions to prevent drifting.
        if (VehiclesWithRootMotion.Contains(requestedIndex))
        {
            ApplyPositionLockToAnimations();
        }
    }

    /// <summary>
    /// Applies the AnimationSpeedMultiplier to all animations for this vehicle.
    /// </summary>
    private void ApplyAnimationSpeedMultiplier()
    {
        if (Model?.Actions == null)
            return;

        if (actionPlaySpeedOverrides != null &&
            actionPlaySpeedOverrides.Count > 0)
        {
            foreach (var kvp in actionPlaySpeedOverrides)
            {
                int index = kvp.Key;

                if (index < 0 || index >= Model.Actions.Length)
                {
                    continue;
                }

                var action = Model.Actions[index];

                if (action == null)
                {
                    continue;
                }

                action.PlaySpeed = kvp.Value;
            }

            return;
        }

        bool hasCustomMultiplier =
            AnimationSpeedMultiplier != 1.0f
            || idleAnimationSpeedMultiplier != 1.0f
            || runAnimationSpeedMultiplier != 1.0f
            || skillAnimationSpeedMultiplier != 1.0f;

        if (!hasCustomMultiplier)
            return;

        for (int i = 0; i < Model.Actions.Length; i++)
        {
            var action = Model.Actions[i];

            if (action == null)
            {
                continue;
            }

            float multiplier = AnimationSpeedMultiplier;

            if (i == idleActionIndex)
            {
                multiplier *= idleAnimationSpeedMultiplier;
            }
            else if (i == runActionIndex)
            {
                multiplier *= runAnimationSpeedMultiplier;
            }
            else if (i == skillActionIndex)
            {
                multiplier *= skillAnimationSpeedMultiplier;
            }

            if (multiplier != 1.0f)
            {
                action.PlaySpeed *= multiplier;
            }
        }
    }

    /// <summary>
    /// Forces LockPositions on all animations for vehicles that have root motion baked in.
    /// This prevents the vehicle from drifting ahead during run animations.
    /// </summary>
    private void ApplyPositionLockToAnimations()
    {
        if (Model?.Actions == null)
            return;

        foreach (var action in Model.Actions)
        {
            if (action != null && !action.LockPositions)
            {
                action.LockPositions = true;
            }
        }
    }

    public override async Task Load()
    {
        await base.Load();
    }

    public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            UpdateFenrirFootThunder();
            UpdateFenrirBodyThunder(gameTime);
            UpdateFenrirFaceLights(gameTime);
            UpdateFenrirSkillSpark(gameTime);
        }
    private bool IsFenrir()
                {
                    // VehicleDatabase:
                    // 11-14 = fenril_*
                    // 15-18 = fenrir_*
                    return itemIndex >= 11 && itemIndex <= 18;
                }
                private Vector3 GetFenrirThunderColor()
        {
            //
            // VehicleDatabase:
            //
            // 11 / 15 = Black
            // 12 / 16 = Blue
            // 13 / 17 = Gold
            // 14 / 18 = Red
            //

            return itemIndex switch
            {
                12 or 16 =>
                    new Vector3(
                        0.1f,
                        0.1f,
                        0.8f),

                13 or 17 =>
                    new Vector3(
                        0.8f,
                        0.8f,
                        0.1f),

                14 or 18 =>
                    new Vector3(
                        0.8f,
                        0.0f,
                        0.0f),

                _ =>
                    new Vector3(
                        1.0f,
                        1.0f,
                        0.2f)
            };
        }

        private float GetFenrirAnimationFrame()
        {
            if (Model?.Actions == null)
                return 0f;

            if (CurrentAction < 0 ||
                CurrentAction >= Model.Actions.Length)
                return 0f;

            var action = Model.Actions[CurrentAction];

            if (action == null)
                return 0f;

            int totalFrames = Math.Max(
                action.LockPositions
                    ? action.NumAnimationKeys - 1
                    : action.NumAnimationKeys,
                1);

            return (float)(_animTime % totalFrames);
        }

        private bool TryGetFenrirBoneWorldPosition(
            int boneIndex,
            out Vector3 worldPosition)
        {
            worldPosition = Vector3.Zero;

            Matrix[] bones = GetBoneTransforms();

            if (bones == null)
                return false;

            if (boneIndex < 0 ||
                boneIndex >= bones.Length)
                return false;

            Matrix boneWorld =
                bones[boneIndex] * WorldPosition;

            worldPosition =
                Vector3.Transform(
                    Vector3.Zero,
                    boneWorld);

            return true;
        }
        private bool TryGetFenrirBoneLocalPosition(
            int boneIndex,
            out Vector3 localPosition)
        {
            localPosition = Vector3.Zero;

            Matrix[] bones =
                GetBoneTransforms();

            if (bones == null)
                return false;

            if (boneIndex < 0 ||
                boneIndex >= bones.Length)
                return false;

            //
            // TransformPosition(BoneTransform[x], Vector3.Zero)
            // del cliente clásico equivale a tomar la
            // Translation de la matriz.
            //
            localPosition =
                bones[boneIndex].Translation;

            return true;
        }
        private bool TryGetFenrirBoneLocalPosition(
            int boneIndex,
            Vector3 offset,
            out Vector3 localPosition)
        {
            localPosition = Vector3.Zero;

            Matrix[] bones =
                GetBoneTransforms();

            if (bones == null)
                return false;

            if (boneIndex < 0 ||
                boneIndex >= bones.Length)
                return false;

            //
            // Equivalent to the original MU:
            //
            // TransformPosition(
            //     BoneTransform[bone],
            //     offset,
            //     output,
            //     false);
            //
            localPosition =
                Vector3.Transform(
                    offset,
                    bones[boneIndex]);

            return true;
        }
        private void HideFenrirFaceLights()
        {
            for (int i = 0;
                i < _fenrirEyeLights.Length;
                i++)
            {
                _fenrirEyeLights[i].Hidden = true;
            }

            for (int i = 0;
                i < _fenrirJawLights.Length;
                i++)
            {
                _fenrirJawLights[i].Hidden = true;
            }
        }
        private void UpdateFenrirFaceLights(
            GameTime gameTime)
        {
            if (!IsFenrir() ||
                Model == null ||
                Hidden)
            {
                HideFenrirFaceLights();
                return;
            }

            //
            // Original MU:
            //
            // fLuminosity =
            // sin(WorldTime * 0.002) * 0.2
            //
            float worldTime =
                (float)gameTime.TotalGameTime.TotalMilliseconds;

            float luminosity =
                MathF.Sin(worldTime * 0.002f)
                * 0.2f;


            // =========================================================
            // EYES
            // =========================================================
            //
            // Original:
            //
            // Bone 11
            //
            // Right/left eye:
            //
            // (50, 2, 11)
            // (50, 2, -11)
            //

            Vector3 eyePositionA;
            Vector3 eyePositionB;

            bool eyeAFound =
                TryGetFenrirBoneLocalPosition(
                    11,
                    new Vector3(
                        50f,
                        2f,
                        11f),
                    out eyePositionA);

            bool eyeBFound =
                TryGetFenrirBoneLocalPosition(
                    11,
                    new Vector3(
                        50f,
                        2f,
                        -11f),
                    out eyePositionB);


            if (eyeAFound && eyeBFound)
            {
                Vector3 eyeColor =
                    new Vector3(
                        MathHelper.Clamp(
                            0.9f + luminosity,
                            0f,
                            1f),

                        MathHelper.Clamp(
                            0.2f +
                            luminosity * 0.5f,
                            0f,
                            1f),

                        MathHelper.Clamp(
                            0.1f +
                            luminosity * 0.5f,
                            0f,
                            1f));


                //
                // Original CreateSprite scale:
                //
                // 0.5 + luminosity * 0.1
                //
                float originalEyeScale =
                    0.5f +
                    luminosity * 0.1f;

                //
                // SpriteObject currently applies Scale during
                // the world transform and again during screen
                // projection, so sqrt gives us approximately
                // the intended MU scale.
                //
                float eyeScale =
                    MathF.Sqrt(
                        MathF.Max(
                            0.01f,
                            originalEyeScale));


                // Eye A - two overlapping lights.
                _fenrirEyeLights[0].Position =
                    eyePositionA;

                _fenrirEyeLights[1].Position =
                    eyePositionA;


                // Eye B - two overlapping lights.
                _fenrirEyeLights[2].Position =
                    eyePositionB;

                _fenrirEyeLights[3].Position =
                    eyePositionB;


                for (int i = 0;
                    i < _fenrirEyeLights.Length;
                    i++)
                {
                    LightEffect light =
                        _fenrirEyeLights[i];

                    light.Light =
                        eyeColor;

                    light.Scale =
                        eyeScale;

                    light.Alpha =
                        1f;

                    light.Hidden =
                        false;
                }
            }
            else
            {
                for (int i = 0;
                    i < _fenrirEyeLights.Length;
                    i++)
                {
                    _fenrirEyeLights[i].Hidden =
                        true;
                }
            }


            // =========================================================
            // JAW
            // =========================================================
            //
            // Original:
            //
            // Bone 13
            // Offset (40, 15, 0)
            //
            // CreateSprite(... 1.5 ...)
            // CreateSprite(... 1.0 ...)
            //

            Vector3 jawPosition;

            if (TryGetFenrirBoneLocalPosition(
                13,
                new Vector3(
                    40f,
                    15f,
                    0f),
                out jawPosition))
            {
                Vector3 jawColor =
                    new Vector3(
                        1.0f,
                        0.3f,
                        0.2f);


                _fenrirJawLights[0].Position =
                    jawPosition;

                _fenrirJawLights[1].Position =
                    jawPosition;


                _fenrirJawLights[0].Light =
                    jawColor;

                _fenrirJawLights[1].Light =
                    jawColor;


                //
                // Original scales:
                //
                // 1.5
                // 1.0
                //
                // Again, compensate for SpriteObject scale
                // being applied twice.
                //
                _fenrirJawLights[0].Scale =
                    MathF.Sqrt(1.5f);

                _fenrirJawLights[1].Scale =
                    1.0f;


                _fenrirJawLights[0].Alpha =
                    1f;

                _fenrirJawLights[1].Alpha =
                    1f;


                _fenrirJawLights[0].Hidden =
                    false;

                _fenrirJawLights[1].Hidden =
                    false;
            }
            else
            {
                _fenrirJawLights[0].Hidden =
                    true;

                _fenrirJawLights[1].Hidden =
                    true;
            }
        }
        private void UpdateFenrirSkillSpark(
            GameTime gameTime)
        {
            if (!IsFenrir() ||
                Model == null ||
                Hidden)
            {
                _fenrirSkillSpark.Hidden =
                    true;

                _fenrirSkillSparkTimer =
                    0f;

                return;
            }

            //
            // Original Fenrir skill action = action 4.
            //
            bool usingSkill =
                CurrentAction ==
                skillActionIndex;

            if (!usingSkill)
            {
                _fenrirSkillSpark.Hidden =
                    true;

                _fenrirSkillSparkTimer =
                    0f;

                return;
            }

            float dt =
                (float)gameTime
                    .ElapsedGameTime
                    .TotalSeconds;

            _fenrirSkillSparkTimer += dt;

            if (_fenrirSkillSparkTimer <
                FenrirSkillSparkInterval)
            {
                return;
            }

            _fenrirSkillSparkTimer = 0f;


            //
            // Original:
            //
            // Bone 14
            //
            // Vector(
            //   (rand()%10 - 10) * 0.5,
            //   0,
            //   (rand()%40 - 20) * 0.5
            // )
            //

            Vector3 randomOffset =
                new Vector3(
                    (Random.Shared.Next(10) - 10)
                        * 0.5f,

                    0f,

                    (Random.Shared.Next(40) - 20)
                        * 0.5f);


            if (!TryGetFenrirBoneLocalPosition(
                14,
                randomOffset,
                out Vector3 sparkPosition))
            {
                _fenrirSkillSpark.Hidden =
                    true;

                return;
            }


            float worldTime =
                (float)gameTime
                    .TotalGameTime
                    .TotalMilliseconds;

            float luminosity =
                MathF.Sin(
                    worldTime * 0.002f)
                * 0.2f;


            //
            // Original scale:
            //
            // 0.7 + luminosity * 0.05
            //
            float originalScale =
                0.7f +
                luminosity * 0.05f;


            _fenrirSkillSpark.Position =
                sparkPosition;

            _fenrirSkillSpark.Light =
                new Vector3(
                    1f,
                    0f,
                    0f);

            _fenrirSkillSpark.Scale =
                MathF.Sqrt(
                    MathF.Max(
                        0.01f,
                        originalScale));

            _fenrirSkillSpark.Alpha =
                1f;

            _fenrirSkillSpark.Hidden =
                false;
        }
        private FenrirThunderEffect GetNextFenrirThunderEffect()
        {
            //
            // Primero buscamos un efecto que haya terminado.
            //
            for (int i = 0;
                i < _fenrirThunderEffects.Length;
                i++)
            {
                int index =
                    (_fenrirThunderPoolCursor + i)
                    % _fenrirThunderEffects.Length;

                FenrirThunderEffect effect =
                    _fenrirThunderEffects[index];

                if (!effect.IsActive)
                {
                    _fenrirThunderPoolCursor =
                        (index + 1)
                        % _fenrirThunderEffects.Length;

                    return effect;
                }
            }

            //
            // Si los ocho siguen vivos, reutilizamos el siguiente.
            //
            FenrirThunderEffect fallback =
                _fenrirThunderEffects[
                    _fenrirThunderPoolCursor];

            _fenrirThunderPoolCursor =
                (_fenrirThunderPoolCursor + 1)
                % _fenrirThunderEffects.Length;

            return fallback;
        }
        private void SpawnFenrirBodyThunder()
        {
            if (!IsFenrir())
                return;

            //
            // Original:
            //
            // Scale = 0.3 + rand(0..99) * 0.002
            //
            float scale =
                0.3f
                + Random.Shared.Next(100) * 0.002f;

            int roll =
                Random.Shared.Next(30);

            Vector3 localPosition;

            //
            // Original ZzzEffect.cpp:
            //
            // 1-2  -> bone 10
            // 3    -> bone 14
            // 4-5  -> bone 2
            // 6-7  -> bone 50 / 51
            // 8    -> bone 53
            // 9-10 -> no effect
            // rest -> random around body
            //

            if (roll >= 1 && roll <= 2)
            {
                if (!TryGetFenrirBoneLocalPosition(
                        10,
                        out localPosition))
                    return;
            }
            else if (roll == 3)
            {
                scale -= 0.2f;

                if (!TryGetFenrirBoneLocalPosition(
                        14,
                        out localPosition))
                    return;
            }
            else if (roll >= 4 && roll <= 5)
            {
                if (!TryGetFenrirBoneLocalPosition(
                        2,
                        out localPosition))
                    return;
            }
            else if (roll >= 6 && roll <= 7)
            {
                scale -= 0.2f;

                int bone =
                    Random.Shared.Next(2) == 0
                        ? 50
                        : 51;

                if (!TryGetFenrirBoneLocalPosition(
                        bone,
                        out localPosition))
                    return;
            }
            else if (roll == 8)
            {
                scale -= 0.2f;

                if (!TryGetFenrirBoneLocalPosition(
                        53,
                        out localPosition))
                    return;
            }
            else if (roll >= 9 && roll <= 10)
            {
                //
                // Original MU simply kills the effect for
                // these random values.
                //
                return;
            }
            else
            {
                //
                // Most rays appear scattered around the body.
                //
                localPosition =
                    new Vector3(
                        Random.Shared.Next(240) - 120,
                        Random.Shared.Next(10) - 5,
                        110f);

                scale += 0.1f;
            }

            FenrirThunderEffect effect =
                GetNextFenrirThunderEffect();

            effect.Trigger(
                localPosition,
                GetFenrirThunderColor(),
                scale);
        }
        private void UpdateFenrirBodyThunder(
            GameTime gameTime)
        {
            if (!IsFenrir() ||
                Model == null ||
                Hidden)
            {
                _fenrirThunderSpawnTimer = 0f;

                for (int i = 0;
                    i < _fenrirThunderEffects.Length;
                    i++)
                {
                    _fenrirThunderEffects[i]
                        .Stop();
                }

                return;
            }

            float dt =
                (float)gameTime.ElapsedGameTime.TotalSeconds;

            _fenrirThunderSpawnTimer += dt;

            while (_fenrirThunderSpawnTimer
                >= FenrirThunderSpawnInterval)
            {
                _fenrirThunderSpawnTimer -=
                    FenrirThunderSpawnInterval;

                //
                // Original Fenrir creates two instances
                // of MODEL_FENRIR_THUNDER.
                //
                SpawnFenrirBodyThunder();
                SpawnFenrirBodyThunder();
            }
        }

        private void TriggerFenrirFootEffect(
            int effectIndex,
            int boneIndex)
        {
            if (effectIndex < 0 ||
                effectIndex >= _fenrirFootEffects.Length)
                return;

            if (!TryGetFenrirBoneWorldPosition(
                    boneIndex,
                    out Vector3 position))
                return;

            _fenrirFootEffects[effectIndex]
                .Trigger(position);
        }

        private void TriggerAllFenrirFeet()
        {
            TriggerFenrirFootEffect(
                0,
                FenrirBoneFront1);

            TriggerFenrirFootEffect(
                1,
                FenrirBoneFront2);

            TriggerFenrirFootEffect(
                2,
                FenrirBoneRear1);

            TriggerFenrirFootEffect(
                3,
                FenrirBoneRear2);
        }

        private void ResetFenrirEffectCycle()
        {
            _fenrirWalkTriggered = false;
            _fenrirFrontTriggered = false;
            _fenrirRearTriggered = false;
        }

        private void UpdateFenrirFootThunder()
        {
            if (!IsFenrir() ||
                Model == null ||
                Hidden)
            {
                _lastFenrirAction = -1;
                _lastFenrirAnimationFrame = -1f;

                ResetFenrirEffectCycle();

                return;
            }

            float frame =
                GetFenrirAnimationFrame();

            bool actionChanged =
                CurrentAction != _lastFenrirAction;

            bool animationLooped =
                !actionChanged
                && _lastFenrirAnimationFrame >= 0f
                && frame < _lastFenrirAnimationFrame;

            if (actionChanged || animationLooped)
            {
                ResetFenrirEffectCycle();
            }

            //
            // FENRIR WALK
            // Original action index = 1
            //
            if (CurrentAction == 1)
            {
                if (!_fenrirWalkTriggered)
                {
                    TriggerAllFenrirFeet();

                    _fenrirWalkTriggered = true;
                }
            }

            //
            // FENRIR RUN
            // Original action index = 2
            //
            else if (CurrentAction == 2)
            {
                //
                // Original MU:
                // frame > 1.0 && <= 1.4
                // bones 22 and 28
                //
                if (!_fenrirFrontTriggered &&
                    frame >= 1.0f)
                {
                    TriggerFenrirFootEffect(
                        0,
                        FenrirBoneFront1);

                    TriggerFenrirFootEffect(
                        1,
                        FenrirBoneFront2);

                    _fenrirFrontTriggered = true;
                }

                //
                // Original MU:
                // frame > 4.8 && <= 5.2
                // bones 36 and 44
                //
                if (!_fenrirRearTriggered &&
                    frame >= 4.8f)
                {
                    TriggerFenrirFootEffect(
                        2,
                        FenrirBoneRear1);

                    TriggerFenrirFootEffect(
                        3,
                        FenrirBoneRear2);

                    _fenrirRearTriggered = true;
                }
            }

            _lastFenrirAction =
                CurrentAction;

            _lastFenrirAnimationFrame =
                frame;
        }

    /// <summary>
    /// Sets the vehicle animation based on rider state.
    /// </summary>
    public void SetRiderAnimation(
        bool isMoving,
        bool isUsingSkill = false)
    {
        if (Model == null || Hidden)
            return;

        int targetAnim;

        if (isUsingSkill)
        {
            targetAnim = skillActionIndex;

            // Skill animation should play once
            // and hold on last frame.
            HoldOnLastFrame = true;
        }
        else
        {
            if (isMoving)
            {
                targetAnim = runActionIndex;
            }
            else
            {
                targetAnim = idleActionIndex;
            }

            // Normal animations should loop.
            HoldOnLastFrame = false;
        }

        if (CurrentAction != targetAnim)
        {
            CurrentAction = targetAnim;

            // Reset animation time when changing actions.
            _animTime = 0.0;
        }
    }

    public override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
    }
    private void DrawFenrirBodyGlow()
    {
        if (!IsFenrir())
            return;

        if (Model == null ||
            Hidden ||
            Model.Meshes == null ||
            Model.Meshes.Length <= 1)
        {
            return;
        }

        //
        // Classic MU:
        //
        // Fenrir normal:
        //
        // RenderMesh(
        //     1,
        //     RENDER_TEXTURE |
        //     RENDER_BRIGHT |
        //     RENDER_CHROME);
        //
        //
        // Our client does not yet have RENDER_CHROME,
        // so we reproduce the bright additive second pass.
        //

        Vector3 glowColor =
            GetFenrirThunderColor();

        //
        // Make the overlay softer than the lightning.
        //
        glowColor =
            Vector3.Lerp(
                glowColor,
                Vector3.One,
                0.35f);

        Matrix glowMatrix =
            WorldPosition;

        DrawMeshHighlight(
            1,
            glowMatrix,
            glowColor);
    }

    public override void DrawAfter(GameTime gameTime)
    {
        base.DrawAfter(gameTime);

        DrawFenrirBodyGlow();
    }
}