#nullable enable

using Client.Main.Content;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Body part whose classic +15 linked effect should control an attachment.
    /// </summary>
    public enum Grade15BodyPart
    {
        Helm,
        Armor,
        Pants,
        Gloves,
        Boots
    }

    /// <summary>
    /// Reproduces one linked object created by the original MU
    /// NextGradeObjectRender() path for +15 armor.
    ///
    /// Each instance:
    /// - attaches a class15_*.bmd model to the original player bone;
    /// - uses the original local position/rotation from RenderLinkObject();
    /// - adds the three classic sprites:
    ///     BITMAP_MAGIC      -> Effect/Magic_Ground1
    ///     BITMAP_SHINY + 5  -> Effect/Shiny05
    ///     BITMAP_PIN_LIGHT  -> Effect/pin_lights
    ///
    /// Visibility is driven by the actual equipped body part ItemLevel,
    /// so the same object works for local and remote players.
    /// </summary>
    public sealed class Grade15ArmorEffect : ModelObject
    {
        // The classic MU client uses ONE, ONE for these bright linked effects.
        // MonoGame's built-in BlendState.Additive is SourceAlpha, One, which is
        // close but not identical. Using ONE, ONE also makes the black areas of
        // the JPG effect textures contribute nothing instead of appearing as
        // opaque planes.
        private static readonly BlendState MuBrightAdditive = new()
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One
        };

        private readonly Grade15BodyPart _bodyPart;
        private readonly string _modelPath;

        private readonly Grade15Sprite _magicSprite;
        private readonly Grade15Sprite _shinySprite;
        private readonly Grade15Sprite _pinLightSprite;

        public override bool IsStaticForCaching => false;

        // These BMDs are visual effect cards. They should not be shaded like a
        // normal world model; the classic client renders them as bright/additive.
        protected override bool AllowDynamicLightingShader => false;
        protected override bool AllowLightingUpdates => false;

        public Grade15ArmorEffect(
            Grade15BodyPart bodyPart,
            string modelPath,
            int parentBone,
            Vector3 localPosition,
            Vector3 localRotationDegrees)
        {
            _bodyPart = bodyPart;
            _modelPath = modelPath;

            ParentBoneLink = parentBone;

            Position = localPosition;
            Angle = new Vector3(
                MathHelper.ToRadians(localRotationDegrees.X),
                MathHelper.ToRadians(localRotationDegrees.Y),
                MathHelper.ToRadians(localRotationDegrees.Z));

            Scale = 1f;

            // The class15 model is a linked visual effect, not a physical
            // body part. The classic client renders the effect planes with
            // bright additive blending and without writing depth.
            RenderShadow = false;
            Interactive = false;

            IsTransparent = true;
            AffectedByTransparency = true;

            // Every mesh in class15_*.bmd is an effect mesh.
            BlendMesh = -2;
            BlendState = MuBrightAdditive;
            BlendMeshState = MuBrightAdditive;
            BlendMeshLight = 1.0f;
            DepthState = DepthStencilState.DepthRead;

            // Keep the vertex colour white. We do not want terrain/sun lighting
            // to darken an additive effect texture.
            LightEnabled = false;
            Light = Vector3.One;
            Color = Color.White;
            UseSunLight = false;

            // Keep this independent from the generic item material system.
            // The BMD is rendered as its own linked effect model.
            ItemLevel = 0;
            IsExcellentItem = false;
            IsAncientItem = false;

            Hidden = true;

            // Classic:
            // fLight2 = abs(sin(WorldTime * 0.01));
            // Light = (0.2*fLight2, 0.4*fLight2, 1.0*fLight2)
            // CreateSprite(BITMAP_MAGIC, ..., 0.12f, ...)
            _magicSprite = new Grade15Sprite(
                "Effect/Magic_Ground1.jpg",
                0.12f,
                Vector3.Zero);

            // Classic:
            // Light = (0.4, 0.7, 1.0)
            // CreateSprite(BITMAP_SHINY + 5, ..., 0.4f, ...)
            //
            // Season 6's texture table ultimately maps BITMAP_SHINY + 5
            // to Effect/shiny05.jpg.
            _shinySprite = new Grade15Sprite(
                "Effect/Shiny05.jpg",
                0.40f,
                new Vector3(0.4f, 0.7f, 1.0f));

            // Classic:
            // Light = (0.1, 0.3, 1.0)
            // CreateSprite(BITMAP_PIN_LIGHT, ..., 0.6f, ..., 90.0f)
            _pinLightSprite = new Grade15Sprite(
                "Effect/pin_lights.jpg",
                0.60f,
                new Vector3(0.1f, 0.3f, 1.0f))
            {
                Angle = new Vector3(
                    0f,
                    0f,
                    MathHelper.ToRadians(90f))
            };

            Children.Add(_magicSprite);
            Children.Add(_shinySprite);
            Children.Add(_pinLightSprite);
        }

        protected override bool ShouldApplyItemMaterial(int meshIndex)
        {
            // class15_*.bmd is already an effect object. It must not inherit
            // the equipped item's generic +7/+15/Excellent material shader.
            return false;
        }

        public override async Task Load()
        {
            // class15 BMDs live in Data/Item and reference their textures
            // from the same Item directory (class15_effect_main).
            Model = await BMDLoader.Instance.Prepare(_modelPath);
            await base.Load();
        }

        public override void Update(GameTime gameTime)
        {
            if (Parent is not PlayerObject player)
            {
                Hidden = true;
                return;
            }

            Matrix[]? playerBones =
                player.GetBoneTransforms();

            bool hasParentBone =
                playerBones != null &&
                ParentBoneLink >= 0 &&
                ParentBoneLink < playerBones.Length;

            bool enabled =
                hasParentBone &&
                GetEquippedLevel(player) >= 15;

            if (!enabled)
            {
                Hidden = true;
                return;
            }

            Hidden = false;

            base.Update(gameTime);

            // The original client creates the three sprites at bone 0 of the
            // linked class15 model. Using the local root translation keeps the
            // sprites attached to that same point while the player moves.
            Vector3 spriteLocalPosition =
                Vector3.Zero;

            Matrix[]? effectBones =
                GetBoneTransforms();

            if (effectBones != null &&
                effectBones.Length > 0)
            {
                spriteLocalPosition =
                    effectBones[0].Translation;
            }

            _magicSprite.Position =
                spriteLocalPosition;

            _shinySprite.Position =
                spriteLocalPosition;

            _pinLightSprite.Position =
                spriteLocalPosition;

            // WorldTime in the original client is milliseconds:
            // abs(sin(WorldTime * 0.01f))
            // == abs(sin(seconds * 10)).
            float timeSeconds =
                (float)gameTime.TotalGameTime.TotalSeconds;

            float fLight2 =
                MathF.Abs(
                    MathF.Sin(
                        timeSeconds * 10f));

            _magicSprite.Light =
                new Vector3(
                    0.2f * fLight2,
                    0.4f * fLight2,
                    1.0f * fLight2);
        }

        private int GetEquippedLevel(
            PlayerObject player)
        {
            return _bodyPart switch
            {
                Grade15BodyPart.Helm =>
                    player.Helm?.ItemLevel ?? 0,

                Grade15BodyPart.Armor =>
                    player.Armor?.ItemLevel ?? 0,

                Grade15BodyPart.Pants =>
                    player.Pants?.ItemLevel ?? 0,

                Grade15BodyPart.Gloves =>
                    player.Gloves?.ItemLevel ?? 0,

                Grade15BodyPart.Boots =>
                    player.Boots?.ItemLevel ?? 0,

                _ => 0
            };
        }

        /// <summary>
        /// Small persistent billboard used instead of creating/destroying
        /// classic sprites every frame.
        /// </summary>
        private sealed class Grade15Sprite : SpriteObject
        {
            private readonly string _texturePath;

            public override string TexturePath =>
                _texturePath;

            public Grade15Sprite(
                string texturePath,
                float scale,
                Vector3 light)
            {
                _texturePath =
                    texturePath;

                Scale =
                    scale;

                Light =
                    light;

                LightEnabled =
                    true;

                IsTransparent =
                    true;

                AffectedByTransparency =
                    true;

                BlendState =
                    MuBrightAdditive;

                DepthState =
                    DepthStencilState.DepthRead;

                Interactive =
                    false;
            }
        }
    }
}
