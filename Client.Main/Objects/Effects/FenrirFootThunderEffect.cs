using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading.Tasks;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Animated electrical flash used underneath Fenrir's paws.
    /// Based on the original MU Online Fenrir foot thunder effect.
    /// </summary>
    public class FenrirFootThunderEffect : EffectObject
    {
        private const int FrameCount = 5;
        private const float FrameDuration = 0.20f;
        private const float TotalDuration = FrameCount * FrameDuration;

        private readonly Texture2D[] _textures = new Texture2D[FrameCount];

        private static readonly VertexPositionTexture[] _vertices =
        {
            new(new Vector3(-1f, 0f, -1f), new Vector2(0f, 0f)),
            new(new Vector3( 1f, 0f, -1f), new Vector2(1f, 0f)),
            new(new Vector3(-1f, 0f,  1f), new Vector2(0f, 1f)),
            new(new Vector3( 1f, 0f,  1f), new Vector2(1f, 1f))
        };

        private static readonly short[] _indices =
        {
            0, 1, 2,
            2, 1, 3
        };

        private int _currentFrame;
        private float _frameTimer;
        private float _lifeTimer;

        private Vector3 _effectPosition;

        /// <summary>
        /// Approximate radius of the effect in MU world units.
        /// </summary>
        public float EffectSize { get; set; } = 32f;

        public FenrirFootThunderEffect()
        {
            IsTransparent = true;
            AffectedByTransparency = true;

            BlendState = BlendState.Additive;
            DepthState = DepthStencilState.DepthRead;

            LightEnabled = false;

            Alpha = 1f;

            // Effect starts disabled.
            Hidden = true;

            BoundingBoxLocal =
                new BoundingBox(Vector3.Zero, Vector3.Zero);
        }

        public override async Task LoadContent()
        {
            for (int i = 0; i < FrameCount; i++)
            {
                string path =
                    $"Effect/eff_lightinga{i + 1:00}.jpg";

                await TextureLoader.Instance.Prepare(path);

                _textures[i] =
                    TextureLoader.Instance.GetTexture2D(path);
            }
        }

        /// <summary>
        /// Starts the Fenrir foot flash at a world-space position.
        /// </summary>
        public void Trigger(Vector3 worldPosition)
        {
            _effectPosition = worldPosition;

            _currentFrame = 0;
            _frameTimer = 0f;
            _lifeTimer = 0f;

            Alpha = 1f;
            Hidden = false;

            WorldPosition =
                Matrix.CreateTranslation(worldPosition);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Hidden)
                return;

            float dt =
                (float)gameTime.ElapsedGameTime.TotalSeconds;

            _lifeTimer += dt;
            _frameTimer += dt;

            if (_frameTimer >= FrameDuration)
            {
                _frameTimer -= FrameDuration;
                _currentFrame++;

                if (_currentFrame >= FrameCount)
                {
                    Hidden = true;
                    return;
                }
            }

            // Fade progressively like the original MU effect.
            Alpha = MathHelper.Clamp(
                1f - (_lifeTimer / TotalDuration),
                0f,
                1f);

            // Slightly above ground to avoid Z fighting.
            WorldPosition =
                Matrix.CreateTranslation(
                    _effectPosition + new Vector3(0f, 0f, 1.5f));
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Hidden)
                return;

            if (_currentFrame < 0 ||
                _currentFrame >= _textures.Length)
                return;

            Texture2D texture = _textures[_currentFrame];

            if (texture == null)
                return;

            GraphicsDevice gd =
                GraphicsManager.Instance.GraphicsDevice;

            BasicEffect effect =
                GraphicsManager.Instance.BasicEffect3D;

            BlendState previousBlend =
                gd.BlendState;

            DepthStencilState previousDepth =
                gd.DepthStencilState;

            RasterizerState previousRasterizer =
                gd.RasterizerState;

            SamplerState previousSampler =
                gd.SamplerStates[0];

            bool previousTextureEnabled =
                effect.TextureEnabled;

            bool previousVertexColorEnabled =
                effect.VertexColorEnabled;

            bool previousLightingEnabled =
                effect.LightingEnabled;

            Texture2D previousTexture =
                effect.Texture;

            Matrix previousWorld =
                effect.World;

            Matrix previousView =
                effect.View;

            Matrix previousProjection =
                effect.Projection;

            Vector3 previousDiffuse =
                effect.DiffuseColor;

            float previousAlpha =
                effect.Alpha;

            try
            {
                gd.BlendState = BlendState.Additive;
                gd.DepthStencilState = DepthStencilState.DepthRead;
                gd.RasterizerState = RasterizerState.CullNone;
                gd.SamplerStates[0] = SamplerState.LinearClamp;

                effect.TextureEnabled = true;
                effect.VertexColorEnabled = false;
                effect.LightingEnabled = false;

                effect.Texture = texture;

                effect.DiffuseColor = Vector3.One;
                effect.Alpha = TotalAlpha;

                effect.World =
                    Matrix.CreateScale(EffectSize)
                    * Matrix.CreateTranslation(
                        _effectPosition.X,
                        _effectPosition.Y,
                        _effectPosition.Z + 1.5f);

                effect.View =
                    Camera.Instance.View;

                effect.Projection =
                    Camera.Instance.Projection;

                foreach (EffectPass pass
                         in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    gd.DrawUserIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        _vertices,
                        0,
                        _vertices.Length,
                        _indices,
                        0,
                        2);
                }
            }
            finally
            {
                effect.TextureEnabled =
                    previousTextureEnabled;

                effect.VertexColorEnabled =
                    previousVertexColorEnabled;

                effect.LightingEnabled =
                    previousLightingEnabled;

                effect.Texture =
                    previousTexture;

                effect.World =
                    previousWorld;

                effect.View =
                    previousView;

                effect.Projection =
                    previousProjection;

                effect.DiffuseColor =
                    previousDiffuse;

                effect.Alpha =
                    previousAlpha;

                gd.BlendState =
                    previousBlend;

                gd.DepthStencilState =
                    previousDepth;

                gd.RasterizerState =
                    previousRasterizer;

                gd.SamplerStates[0] =
                    previousSampler;
            }

            base.Draw(gameTime);
        }
    }
}