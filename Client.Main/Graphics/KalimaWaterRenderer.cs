using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Graphics
{
    /// <summary>Kalima's separate CSWaterTerrain surface, not the ordinary tile-water shader.</summary>
    internal sealed class KalimaWaterRenderer : IDisposable
    {
        private const int WaveSize = 256;
        // Native uses 42 points; expand coverage for MonoGame's higher, zoomable camera.
        private const int Grid = 96;
        private const float Spacing = 50f;
        private readonly int[][] _wave = { new int[WaveSize * WaveSize], new int[WaveSize * WaveSize] };
        private readonly Vector3[] _positions = new Vector3[Grid * Grid];
        private readonly Vector3[] _normals = new Vector3[Grid * Grid];
        private readonly VertexPositionColorTexture[] _baseVertices = new VertexPositionColorTexture[Grid * Grid];
        private readonly VertexPositionColorTexture[] _lightVertices = new VertexPositionColorTexture[Grid * Grid];
        private readonly short[] _indices = new short[(Grid - 1) * (Grid - 1) * 6];
        private readonly BlendState _additive = new()
        {
            ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One,
        };
        private Texture2D _water, _reflection;
        private AlphaTestEffect _effect;
        private int _page;
        private double _time, _accumulator, _nextRipple = 2.6;
        private readonly Random _random = new(25);
        private bool _disposed;

        public KalimaWaterRenderer()
        {
            int n = 0;
            for (int y = 0; y < Grid - 1; y++)
            for (int x = 0; x < Grid - 1; x++)
            {
                int a = y * Grid + x, b = a + 1, c = a + Grid, d = c + 1;
                _indices[n++] = (short)a; _indices[n++] = (short)b;
                _indices[n++] = (short)(((x + y) & 1) == 0 ? d : c);
                _indices[n++] = (short)(((x + y) & 1) == 0 ? a : b);
                _indices[n++] = (short)d; _indices[n++] = (short)c;
            }
        }

        public async Task LoadAsync()
        {
            var textures = await Task.WhenAll(
                TextureLoader.Instance.PrepareAndGetTexture("Object25/water1.OZT"),
                TextureLoader.Instance.PrepareAndGetTexture("Object25/water2.OZJ"));
            if (_disposed) return;
            _water = textures[0]; _reflection = textures[1];
        }

        public void Update(GameTime gameTime, Vector3 hero)
        {
            // Original simulation runs at 25 Hz. Do not catch up suspended/background time.
            _accumulator += Math.Min(0.1, Math.Max(0, gameTime.ElapsedGameTime.TotalSeconds));
            while (_accumulator >= 0.04)
            {
                _accumulator -= 0.04;
                _time += 0.04;
                if (_time >= _nextRipple)
                {
                    AddRipple((int)(hero.X / Spacing) + _random.Next(-15, 15), (int)(hero.Y / Spacing) + 25);
                    _nextRipple += 2.6;
                }
                _page ^= 1;
                var next = _wave[_page]; var old = _wave[_page ^ 1];
                for (int y = 1; y < WaveSize - 1; y++)
                for (int x = 1; x < WaveSize - 1; x++)
                {
                    int i = y * WaveSize + x;
                    int h = ((old[i + WaveSize] + old[i - WaveSize] + old[i + 1] + old[i - 1]) >> 1) - next[i];
                    next[i] = h - (h >> 4);
                }
            }
        }

        private void AddRipple(int x, int y)
        {
            // Native addSineWave(x,y,20,2,2000), including its circular radius test.
            var page = _wave[_page];
            for (int dy = -2; dy < 2; dy++)
            for (int dx = -20; dx < 20; dx++)
            {
                int px = x + dx, py = y + dy, square = dx * dx + dy * dy;
                if (px < 1 || py < 1 || px >= WaveSize - 1 || py >= WaveSize - 1 || square >= 40) continue;
                float distance = MathF.Sqrt(square * (1024f / 20) * (1024f / 20));
                page[py * WaveSize + px] += (int)((MathF.Cos(distance) + 65535) * 2000) >> 19;
            }
        }

        private float Height(int x, int y)
        {
            if (x < 0 || y < 0 || x >= WaveSize || y >= WaveSize) return 350;
            float t = (float)(_time * 1000);
            int a = (int)(MathF.Sin(t * 0.005f + y * 0.1f + x * 0.1f) * 50);
            int base1 = (int)(a - MathF.Sin(t * 0.003f + x * 0.1f + y * 0.5f) * 50);
            int b = (int)(MathF.Sin(t * 0.001f + y * 0.5f + x * 0.5f) * 25);
            int base2 = (int)(b - MathF.Sin(t * 0.002f + x + y * 0.3f) * 25);
            return 350 + _wave[_page][y * WaveSize + x] * 0.5f + (base1 + base2) * 0.25f;
        }

        private void BuildMesh(Vector3 hero)
        {
            int startX = (int)(hero.X / Spacing) - Grid / 2 - 4;
            int startY = (int)(hero.Y / Spacing) - Grid / 2 + 6;
            Array.Clear(_normals, 0, _normals.Length);
            for (int y = 0; y < Grid; y++)
            for (int x = 0; x < Grid; x++)
                _positions[y * Grid + x] = new Vector3((startX + x) * Spacing, (startY + y) * Spacing, Height(startX + x, startY + y));
            for (int i = 0; i < _indices.Length; i += 3)
            {
                int a = _indices[i], b = _indices[i + 1], c = _indices[i + 2];
                var normal = Vector3.Normalize(Vector3.Cross(_positions[b] - _positions[a], _positions[c] - _positions[a]));
                _normals[a] += normal; _normals[b] += normal; _normals[c] += normal;
            }
            for (int i = 0; i < _positions.Length; i++)
            {
                var normal = _normals[i].LengthSquared() > 0 ? Vector3.Normalize(_normals[i]) : Vector3.UnitZ;
                var uv = new Vector2(normal.Y * 0.5f + 0.5f, normal.Z * 0.5f + 0.1f);
                float light = 1 - Vector3.Dot(normal, new Vector3(1, -1, 1));
                _baseVertices[i] = new VertexPositionColorTexture(_positions[i], new Color(0.2f, 0.5f, 0.65f), uv);
                _lightVertices[i] = new VertexPositionColorTexture(_positions[i], new Color(light, light * 2.5f, light * 3), uv);
            }
        }

        public void Draw(Vector3 hero)
        {
            if (_disposed || _water == null || _reflection == null) return;
            var gd = MuGame.Instance.GraphicsDevice;
            _effect ??= new AlphaTestEffect(gd) { VertexColorEnabled = true, AlphaFunction = CompareFunction.Greater, ReferenceAlpha = 0 };
            BuildMesh(hero);
            _effect.World = Matrix.Identity; _effect.View = Camera.Instance.View; _effect.Projection = Camera.Instance.Projection;
            var blend = gd.BlendState; var depth = gd.DepthStencilState; var raster = gd.RasterizerState; var sampler = gd.SamplerStates[0];
            try
            {
                gd.RasterizerState = RasterizerState.CullNone;
                gd.BlendState = BlendState.NonPremultiplied; gd.DepthStencilState = DepthStencilState.Default;
                _effect.Texture = _water;
                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply(); gd.SamplerStates[0] = SamplerState.LinearWrap;
                    gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _baseVertices, 0, _baseVertices.Length, _indices, 0, _indices.Length / 3);
                }
                // Native second pass: GL_ONE/GL_ONE, depth-test on, depth writes off.
                gd.BlendState = _additive; gd.DepthStencilState = DepthStencilState.DepthRead;
                _effect.Texture = _reflection;
                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply(); gd.SamplerStates[0] = SamplerState.PointClamp;
                    gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _lightVertices, 0, _lightVertices.Length, _indices, 0, _indices.Length / 3);
                }
            }
            finally
            {
                gd.BlendState = blend; gd.DepthStencilState = depth; gd.RasterizerState = raster; gd.SamplerStates[0] = sampler;
            }
        }

        public void Dispose()
        {
            _disposed = true; _effect?.Dispose(); _additive.Dispose();
            // Textures are owned by TextureLoader.
        }
    }
}
