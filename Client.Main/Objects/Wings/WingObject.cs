using Client.Data.BMD;
using Client.Main.Content;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Main.Objects.Wings
{
    public class CustomEffect
    {
        public int BoneID { get; set; }

        public EffectType EffectID { get; set; }

        public Vector3 Angle { get; set; }

        public Vector3 Position { get; set; }

        public float Scale { get; set; }

        public Vector3 Color { get; set; }

        public SpriteObject Effect { get; set; }
    }


    public partial class WingObject : ModelObject
    {
        public List<CustomEffect> _effects { get; set; }
            = new List<CustomEffect>();


        // ============================================================
        // CLASSIC MU ATTACHMENT BONES
        // ============================================================

        //
        // Normal wings are linked to player bone 47.
        //
        private const int DefaultWingBoneLink = 47;


        //
        // MODEL_CAPE_OF_EMPEROR
        //
        // Original MU:
        //
        //     w->LinkBone = 19;
        //
        private const int EmperorCapeBoneLink = 19;


        private const short EmperorCapeItemIndex = 40;
        private const short OverruleCapeItemIndex = 50;
        // Unlike the other capes, original Cape of Lord is in group 13.
        private const short LordCapeItemIndex = 30;
        private const short FighterCapeItemIndex = 49;


        // ============================================================
        // CAPE OF EMPEROR
        //
        // Classic RenderLinkObject transformation:
        //
        //     Vector(0.f, 90.f, 0.f, Angle);
        //
        //     Matrix[0][3] = -47.f;
        //     Matrix[1][3] =  -7.f;
        //     Matrix[2][3] =   0.f;
        //
        // IMPORTANT:
        //
        // This only positions DarkLordRobe02.bmd.
        //
        // The large red cape and its two hanging ribbons are NOT
        // deformed from this BMD. The original client creates them
        // separately through CPhysicsCloth.
        //
        // Those pieces will be implemented in:
        //
        //     WingObject.Emperor.cs
        //     EmperorCapeCloth.cs
        //
        // ============================================================

        private static readonly Vector3 EmperorCapePosition =
            new Vector3(
                -47.0f,
                -7.0f,
                0.0f);


        private static readonly Vector3 EmperorCapeAngle =
            new Vector3(
                0.0f,
                MathHelper.ToRadians(90.0f),
                0.0f);


        // Cape of Overrule (12,50) rigid component is Wing51.bmd.
        // Main RenderLinkObject: bone 19; yaw 90 degrees; (10,-15,0).
        private static readonly Vector3 OverruleCapePosition =
            new Vector3(10.0f, -15.0f, 0.0f);

        private bool _isEmperorCape;
        private bool _isOverruleCape;
        private bool _isLordCape;
        private bool _isFighterCape;

        private bool _savedBaseAttachmentTransform;

        private Vector3 _savedBasePosition;

        private Vector3 _savedBaseAngle;
        private int _savedBaseHiddenMesh = -1;


        // ============================================================
        // ASYNC MODEL CHANGE VERSION
        // ============================================================

        private int _changeVersion;


        private short _type;

        public new short Type
        {
            get => _type;

            set
            {
                if (_type == value)
                    return;


                _type = value;


                int changeVersion =
                    Interlocked.Increment(
                        ref _changeVersion);


                _ = OnChangeType(
                    _type,
                    changeVersion);
            }
        }


        private short itemIndex = -1;

        public short ItemIndex
        {
            get => itemIndex;

            set
            {
                if (itemIndex == value)
                    return;


                itemIndex = value;


                int changeVersion =
                    Interlocked.Increment(
                        ref _changeVersion);


                _ = OnChangeIndex(
                    itemIndex,
                    changeVersion);
            }
        }


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public WingObject()
        {
            RenderShadow = true;

            IsTransparent = true;

            AffectedByTransparency = true;

            BlendState =
                BlendState.AlphaBlend;

            BlendMesh = -1;

            BlendMeshState =
                BlendState.Additive;

            Alpha = 1.0f;


            //
            // Wings/capes have their own BMD animation.
            //
            LinkParentAnimation = false;


            //
            // Default MU wing attachment.
            //
            ParentBoneLink =
                DefaultWingBoneLink;
        }


        // ============================================================
        // DEPTH
        // ============================================================

        protected override float GetDepthBias()
        {
            //
            // Wings and rigid cape parts sit very close to the body.
            //
            // Keep the small existing bias to prevent the attachment
            // from disappearing into the player mesh because of depth
            // precision.
            //
            return -0.000012f;
        }


        // ============================================================
        // CHANGE VERSION HELPERS
        // ============================================================

        private bool IsCurrentChangeVersion(
            int changeVersion)
        {
            return
                Volatile.Read(
                    ref _changeVersion)
                ==
                changeVersion;
        }


        private void UpdateStatusAfterAsyncResolve(
            bool modelResolved)
        {
            if (Status is not (
                GameControlStatus.Ready or
                GameControlStatus.Error))
            {
                return;
            }


            Status =
                modelResolved
                    ? GameControlStatus.Ready
                    : GameControlStatus.Error;
        }


        // ============================================================
        // TYPE-BASED WINGS
        // ============================================================

        private async Task OnChangeType(
            short requestedType,
            int changeVersion)
        {
            if (!IsCurrentChangeVersion(
                    changeVersion))
            {
                return;
            }


            if (requestedType <= 0)
            {
                if (ItemIndex < 0)
                {
                    ApplyEmperorCapeState(
                        false);

                    Model = null;
                }

                return;
            }


            string modelPath =
                Path.Combine(
                    "Item",
                    $"Wing{requestedType:D2}.bmd");


            BMD resolvedModel =
                await BMDLoader.Instance.Prepare(
                    modelPath);


            if (resolvedModel == null)
            {
                modelPath =
                    Path.Combine(
                        "Item",
                        $"Wing{requestedType}.bmd");


                resolvedModel =
                    await BMDLoader.Instance.Prepare(
                        modelPath);
            }


            if (!IsCurrentChangeVersion(
                    changeVersion))
            {
                return;
            }


            //
            // Type-based wings are regular wings.
            //
            ApplyEmperorCapeState(
                false);


            Model =
                resolvedModel;


            UpdateStatusAfterAsyncResolve(
                resolvedModel != null);
        }


        // ============================================================
        // ITEM-BASED WINGS
        // ============================================================

        private async Task OnChangeIndex(
            short requestedItemIndex,
            int changeVersion)
        {
            if (!IsCurrentChangeVersion(
                    changeVersion))
            {
                return;
            }


            if (requestedItemIndex < 0)
            {
                if (Type <= 0)
                {
                    ApplyEmperorCapeState(
                        false);

                    Model = null;
                }

                return;
            }


            // Cape of Lord is item 13,30 (MODEL_HELPER + 30).
            // Its index alone overlaps group 12,30, an unrelated item.
            ItemDefinition itemDefinition =
                ItemDatabase.GetItemDefinition(
                    requestedItemIndex == LordCapeItemIndex ? (byte)13 : (byte)12,
                    requestedItemIndex);


            string modelPath =
                itemDefinition?.TexturePath;


            BMD resolvedModel = null;


            // ========================================================
            // MODEL RESOLUTION
            // ========================================================

            if (string.IsNullOrWhiteSpace(
                    modelPath))
            {
                //
                // Original Cape of Lord (13,30): separate from
                // DarkLordRobe02, the Cape of Emperor model.
                if (requestedItemIndex == LordCapeItemIndex)
                {
                    resolvedModel =
                        await BMDLoader.Instance.Prepare(
                            "Item/DarkLordRobe.bmd");
                }

                //
                // Cape of Emperor.
                //
                // Classic MODEL_WING + 40:
                //
                //     DarkLordRobe02.bmd
                //
                else if (
                    requestedItemIndex ==
                    EmperorCapeItemIndex)
                {
                    resolvedModel =
                        await BMDLoader.Instance.Prepare(
                            "Item/DarkLordRobe02.bmd");
                }
                else if (requestedItemIndex == OverruleCapeItemIndex)
                {
                    resolvedModel =
                        await BMDLoader.Instance.Prepare("Item/Wing51.bmd");
                }
                else if (requestedItemIndex == FighterCapeItemIndex)
                {
                    resolvedModel =
                        await BMDLoader.Instance.Prepare("Item/Wing50.bmd");
                }
            }
            else
            {
                string normalized =
                    modelPath.Replace(
                        "\\",
                        "/");


                //
                // Some item databases store wing models as
                //
                //     Item/Wing/Foo.bmd
                //
                // while BMDLoader expects Item/Foo.bmd.
                //
                if (normalized.Contains(
                        "Item/Wing/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    normalized =
                        Path.Combine(
                            "Item",
                            Path.GetFileName(
                                normalized))
                        .Replace(
                            "\\",
                            "/");
                }


                //
                // Legacy naming compatibility.
                //
                if (normalized.Contains(
                        "DarkLordRobe01",
                        StringComparison.OrdinalIgnoreCase))
                {
                    normalized =
                        normalized.Replace(
                            "DarkLordRobe01",
                            "DarkLordRobe",
                            StringComparison.OrdinalIgnoreCase);
                }


                resolvedModel =
                    await BMDLoader.Instance.Prepare(
                        normalized);


                //
                // Existing compatibility fallback for the old cape.
                //
                if (resolvedModel == null &&
                    requestedItemIndex != LordCapeItemIndex &&
                    normalized.EndsWith(
                        "DarkLordRobe.bmd",
                        StringComparison.OrdinalIgnoreCase))
                {
                    string robe02Path =
                        normalized.Replace(
                            "DarkLordRobe.bmd",
                            "DarkLordRobe02.bmd",
                            StringComparison.OrdinalIgnoreCase);


                    resolvedModel =
                        await BMDLoader.Instance.Prepare(
                            robe02Path);
                }


                //
                // Cape of Emperor must resolve specifically to
                // DarkLordRobe02 if its configured path failed.
                //
                if (resolvedModel == null &&
                    requestedItemIndex ==
                    EmperorCapeItemIndex)
                {
                    resolvedModel =
                        await BMDLoader.Instance.Prepare(
                            "Item/DarkLordRobe02.bmd");
                }

                if (resolvedModel == null &&
                    requestedItemIndex == OverruleCapeItemIndex)
                {
                    resolvedModel =
                        await BMDLoader.Instance.Prepare("Item/Wing51.bmd");
                }
                if (resolvedModel == null &&
                    requestedItemIndex == FighterCapeItemIndex)
                {
                    resolvedModel =
                        await BMDLoader.Instance.Prepare("Item/Wing50.bmd");
                }
                if (resolvedModel == null &&
                    requestedItemIndex == LordCapeItemIndex)
                {
                    resolvedModel =
                        await BMDLoader.Instance.Prepare("Item/DarkLordRobe.bmd");
                }
            }


            if (!IsCurrentChangeVersion(
                    changeVersion))
            {
                return;
            }


            // ========================================================
            // ATTACHMENT TRANSFORM
            // ========================================================

            bool isEmperorCape =
                requestedItemIndex ==
                EmperorCapeItemIndex;
            bool isOverruleCape =
                requestedItemIndex == OverruleCapeItemIndex;


            //
            // Assign the model first.
            //
            // The transform below positions the rigid BMD relative to
            // the player skeleton.
            //
            Model =
                resolvedModel;

            ApplyClassicCapeState(
                resolvedModel == null
                    ? (short)-1
                    : isEmperorCape
                        ? EmperorCapeItemIndex
                        : isOverruleCape
                            ? OverruleCapeItemIndex
                            : (short)-1);

            if (isOverruleCape && resolvedModel != null)
                EnsureOverruleCapeCloth();
            else if (requestedItemIndex == LordCapeItemIndex &&
                     resolvedModel != null)
                EnsureLordCapeCloth();
            else if (requestedItemIndex == FighterCapeItemIndex &&
                     resolvedModel != null)
                EnsureFighterCapeCloth();


            // ========================================================
            // CLASSIC WING-SPECIFIC ASSETS
            // ========================================================

            if (requestedItemIndex == 36)
            {
                await PrepareStormClassicAssetsAsync();


                if (!IsCurrentChangeVersion(
                        changeVersion))
                {
                    return;
                }
            }
            else if (requestedItemIndex == 37)
            {
                await PrepareEternalClassicAssetsAsync();


                if (!IsCurrentChangeVersion(
                        changeVersion))
                {
                    return;
                }
            }
            else if (requestedItemIndex == 38)
            {
                await PrepareIllusionClassicAssetsAsync();


                if (!IsCurrentChangeVersion(
                        changeVersion))
                {
                    return;
                }
            }
            else if (requestedItemIndex == 39)
            {
                await PrepareRuinClassicAssetsAsync();


                if (!IsCurrentChangeVersion(
                        changeVersion))
                {
                    return;
                }
            }
            else if (requestedItemIndex == 43)
            {
                await PrepareDimensionClassicAssetsAsync();

                if (!IsCurrentChangeVersion(
                        changeVersion))
                {
                    return;
                }
            }


            UpdateStatusAfterAsyncResolve(
                resolvedModel != null);
        }


        // ============================================================
        // CLASSIC CAPES: RIGID BMD TRANSFORMS
        // ============================================================

        private void ApplyEmperorCapeState(bool enabled)
        {
            ApplyClassicCapeState(enabled ? EmperorCapeItemIndex : (short)-1);
        }

        private void ApplyClassicCapeState(short capeItemIndex)
        {
            bool emperor = capeItemIndex == EmperorCapeItemIndex;
            bool overrule = capeItemIndex == OverruleCapeItemIndex;
            bool lord = capeItemIndex == LordCapeItemIndex;
            bool fighter = capeItemIndex == FighterCapeItemIndex;

            if (emperor || overrule || lord || fighter)
            {
                // Preserve the original wing transform across S2/S3 changes.
                if (!_isEmperorCape && !_isOverruleCape &&
                    !_isLordCape && !_isFighterCape)
                {
                    _savedBasePosition = Position;
                    _savedBaseAngle = Angle;
                    _savedBaseHiddenMesh = HiddenMesh;
                    _savedBaseAttachmentTransform = true;
                }

                // Original Main renders only the cloth for Lord/Fighter S2.
                // S3 capes also render a rigid mesh attached to bone 19.
                if (emperor || overrule)
                {
                    ParentBoneLink = EmperorCapeBoneLink;
                    Position = overrule ? OverruleCapePosition : EmperorCapePosition;
                    Angle = EmperorCapeAngle;
                }
                else
                {
                    ParentBoneLink = DefaultWingBoneLink;
                    Position = _savedBasePosition;
                    Angle = _savedBaseAngle;
                }

                // Hide all static meshes in BOTH opaque and alpha model passes.
                // Draw() re-enables mesh 0 for S3 only.
                HiddenMesh = -2;

                _isEmperorCape = emperor;
                _isOverruleCape = overrule;
                _isLordCape = lord;
                _isFighterCape = fighter;
                return;
            }

            bool wasCape = _isEmperorCape || _isOverruleCape ||
                           _isLordCape || _isFighterCape;
            ParentBoneLink = DefaultWingBoneLink;

            if (wasCape)
                HiddenMesh = _savedBaseHiddenMesh;

            if (wasCape && _savedBaseAttachmentTransform)
            {
                Position = _savedBasePosition;
                Angle = _savedBaseAngle;
            }

            _savedBaseAttachmentTransform = false;
            _isEmperorCape = false;
            _isOverruleCape = false;
            _isLordCape = false;
            _isFighterCape = false;
        }

        // ============================================================
        // CUSTOM EFFECT CHILDREN
        // ============================================================

        public override async Task Load()
        {
            if (_effects.Count > 0)
            {
                foreach (
                    CustomEffect effect
                    in _effects)
                {
                    effect.Effect =
                        Utils.GetEffectByCode(
                            effect.EffectID);


                    effect.Effect.Light =
                        effect.Color;


                    effect.Effect.Scale =
                        effect.Scale;


                    Children.Add(
                        effect.Effect);
                }
            }


            await base.Load();
        }


        // ============================================================
        // UPDATE
        // ============================================================

        public override void Update(
            GameTime gameTime)
        {
            //
            // IMPORTANT:
            //
            // There is intentionally NO cape vertex deformation here.
            //
            // The original Cape of Emperor does not deform
            // DarkLordRobe02.bmd.
            //
            // Cloth simulation will be handled separately by
            // EmperorCapeCloth.
            //

            base.Update(
                gameTime);


            if (_effects.Count > 0 &&
                BoneTransform != null)
            {
                foreach (
                    CustomEffect effect
                    in _effects)
                {
                    effect.Effect.Position =
                        effect.Position +
                        BoneTransform[
                            effect.BoneID]
                        .Translation;


                    effect.Effect.Angle =
                        effect.Angle +
                        Angle;
                }
            }
        }


        // ============================================================
        // DRAW
        // ============================================================

        public override void Draw(
            GameTime gameTime)
        {
            //
            // Storm needs render time before the normal model pass.
            //
            UpdateStormRenderTime(
                gameTime);


            // ========================================================
            // CLASSIC CAPES: S2 CLOTH ONLY, S3 RIGID ORNAMENT + CLOTH
            // ========================================================

            bool isS3Cape =
                (_isEmperorCape && ItemIndex == EmperorCapeItemIndex) ||
                (_isOverruleCape && ItemIndex == OverruleCapeItemIndex);
            bool isS2Cape =
                (_isLordCape && ItemIndex == LordCapeItemIndex) ||
                (_isFighterCape && ItemIndex == FighterCapeItemIndex);

            if ((isS2Cape || isS3Cape) && Model != null)
            {
                // The Main explicitly skips RenderPartObjectEffect for
                // MODEL_CAPE_OF_LORD and MODEL_CAPE_OF_FIGHTER:
                // no stiff BMD is drawn when equipped, only CPhysicsCloth.
                HiddenMesh = -2;
                base.Draw(gameTime);

                if (isS3Cape)
                {
                    // Keep the rigid top ornament of the original S3 capes.
                    try
                    {
                        HiddenMesh = -1;
                        DrawMesh(0);
                    }
                    finally
                    {
                        HiddenMesh = -2;
                    }
                }
            }
            else
            {
                base.Draw(gameTime);
            }


            // ========================================================

            // CLASSIC EXTERNAL WING EFFECTS
            // ========================================================

            DrawStormClassicEffects(
                gameTime);


            DrawEternalClassicEffects(
                gameTime);


            DrawIllusionClassicEffects(
                gameTime);


            DrawRuinClassicEffects(
                gameTime);

            DrawDimensionClassicEffects(
                gameTime);
        }
    }
}