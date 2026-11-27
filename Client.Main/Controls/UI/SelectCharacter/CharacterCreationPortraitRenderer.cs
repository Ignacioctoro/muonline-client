using System;
using System.Threading.Tasks;
using Client.Data.BMD;
using Client.Main.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.SelectCharacter
{
    /// <summary>Original Logo/NewFace portraits; no equipment or world-camera mutation.</summary>
    internal sealed class CharacterCreationPortraitRenderer : IDisposable
    {
        private readonly BMD[] _models = new BMD[7];
        private readonly Matrix[][] _bones = new Matrix[7][];
        private readonly BoundingBox[] _bounds = new BoundingBox[7];
        private readonly Vector3[][] _corners = new Vector3[7][];
        private AlphaTestEffect _effect;
        private readonly BlendState _portraitBlend = new()
        {
            ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha,
        };
        private RenderTarget2D _surface;
        private bool _disposed;
        private int _lastFace = -1;
        private double _lastTime = -1;
        private static readonly Vector3[] Angles =
        {
            new(0, 0, -40), new(0, 0, -12), new(8, 0, 5),
            new(8, 0, -13), new(8, 0, -18), new(2, 0, 2), Vector3.Zero,
        };

        public async Task LoadAsync()
        {
            var tasks = new Task<BMD>[7];
            for (int i = 0; i < tasks.Length; i++) tasks[i] = BMDLoader.Instance.Prepare($"Logo/NewFace{i + 1:D2}.bmd");
            var models = await Task.WhenAll(tasks);
            if (_disposed) return;
            for (int i = 0; i < models.Length; i++)
            {
                _models[i] = models[i];
                if (models[i] == null) continue;
                _bones[i] = new Matrix[models[i].Bones.Length];
                BuildBones(i, 0, restPose: true);
                Vector3 min = new(float.MaxValue), max = new(float.MinValue);
                foreach (var mesh in models[i].Meshes)
                foreach (var vertex in mesh.Vertices)
                {
                    var bone = vertex.Node >= 0 && vertex.Node < _bones[i].Length ? _bones[i][vertex.Node] : Matrix.Identity;
                    var point = Vector3.Transform(new Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z), bone);
                    min = Vector3.Min(min, point); max = Vector3.Max(max, point);
                }
                _bounds[i] = new BoundingBox(min, max);
                _corners[i] = _bounds[i].GetCorners();
            }
        }

        private void BuildBones(int face, double seconds, bool restPose = false)
        {
            var model = _models[face];
            var result = _bones[face];
            float frame = (float)(seconds * (face == 5 ? 6.25 : 7.5));
            int introKeys = model.Actions.Length > 1 ? model.Actions[1].NumAnimationKeys : 0;
            int action = !restPose && introKeys > 0 && frame < introKeys - 1 ? 1 : 0;
            if (restPose) frame = 0;
            else if (action == 0) frame = Math.Max(0, frame - Math.Max(0, introKeys - 1));
            for (int i = 0; i < result.Length; i++)
            {
                var bone = model.Bones[i];
                Matrix local = Matrix.Identity;
                if (bone.Matrixes.Length > 0)
                {
                    var track = bone.Matrixes[Math.Min(action, bone.Matrixes.Length - 1)];
                    if (track.Position?.Length > 0 && track.Quaternion?.Length > 0)
                    {
                        int count = Math.Min(track.Position.Length, track.Quaternion.Length);
                        float key = frame % count;
                        int a = (int)key, b = (a + 1) % count;
                        var qa = track.Quaternion[a]; var qb = track.Quaternion[b];
                        var pa = track.Position[a]; var pb = track.Position[b];
                        var q = Quaternion.Slerp(new Quaternion(qa.X, qa.Y, qa.Z, qa.W), new Quaternion(qb.X, qb.Y, qb.Z, qb.W), key - a);
                        local = Matrix.CreateFromQuaternion(q);
                        local.Translation = Vector3.Lerp(new Vector3(pa.X, pa.Y, pa.Z), new Vector3(pb.X, pb.Y, pb.Z), key - a);
                    }
                }
                result[i] = bone.Parent >= 0 && bone.Parent < i ? local * result[bone.Parent] : local;
            }
        }

        public Texture2D Render(int face, int width, int height, double seconds)
        {
            if (_disposed || face < 0 || face >= 7 || _models[face] == null) return null;
            width = Math.Clamp(width, 128, 640); height = Math.Clamp(height, 128, 640);
            bool resized = _surface == null || _surface.Width != width || _surface.Height != height;
            if (!resized && face == _lastFace && seconds >= _lastTime && seconds - _lastTime < 1.0 / 30) return _surface;
            var gd = MuGame.Instance.GraphicsDevice;
            if (resized)
            {
                _surface?.Dispose();
                _surface = new RenderTarget2D(gd, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24);
            }
            // Reject fully transparent hair texels before they write depth;
            // otherwise their invisible polygons can hide the face behind them.
            _effect ??= new AlphaTestEffect(gd) { VertexColorEnabled = true, AlphaFunction = CompareFunction.Greater, ReferenceAlpha = 2 };
            BuildBones(face, seconds);
            var angles = Angles[face];
            var world = Matrix.CreateRotationX(MathHelper.ToRadians(angles.X)) *
                Matrix.CreateRotationY(MathHelper.ToRadians(angles.Y)) * Matrix.CreateRotationZ(MathHelper.ToRadians(angles.Z));
            Vector3 min = new(float.MaxValue), max = new(float.MinValue);
            foreach (var corner in _corners[face])
            {
                var point = Vector3.Transform(corner, world);
                min = Vector3.Min(min, point); max = Vector3.Max(max, point);
            }
            var center = (min + max) * 0.5f;
            var size = max - min;
            float fov = MathHelper.ToRadians(24), aspect = width / (float)height;
            float distance = Math.Max(size.Z, size.X / aspect) / (2 * MathF.Tan(fov / 2)) * 1.08f + size.Y / 2;
            _effect.World = world;
            _effect.View = Matrix.CreateLookAt(center + new Vector3(0, -distance, 0), center, Vector3.UnitZ);
            _effect.Projection = Matrix.CreatePerspectiveFieldOfView(fov, aspect, Math.Max(0.1f, distance / 1000), distance * 4 + 100);
            _effect.DiffuseColor = Vector3.One; _effect.Alpha = 1;
            var targets = gd.GetRenderTargets();
            var blend = gd.BlendState; var depth = gd.DepthStencilState; var raster = gd.RasterizerState; var sampler = gd.SamplerStates[0];
            try
            {
                gd.SetRenderTarget(_surface); gd.Clear(Color.Transparent);
                gd.BlendState = _portraitBlend; gd.DepthStencilState = DepthStencilState.Default;
                gd.RasterizerState = RasterizerState.CullNone; gd.SamplerStates[0] = SamplerState.LinearClamp;
                var model = _models[face];
                for (int i = 0; i < model.Meshes.Length; i++)
                {
                    DynamicVertexBuffer vertices = null; DynamicIndexBuffer indices = null;
                    BMDLoader.Instance.GetModelBuffers(model, i, Color.White, _bones[face], ref vertices, ref indices);
                    var texture = TextureLoader.Instance.GetTexture2D(BMDLoader.Instance.GetTexturePath(model, model.Meshes[i].TexturePath));
                    if (vertices == null || indices == null || texture == null) continue;
                    _effect.Texture = texture; gd.SetVertexBuffer(vertices); gd.Indices = indices;
                    foreach (var pass in _effect.CurrentTechnique.Passes)
                    {
                        pass.Apply(); gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, indices.IndexCount / 3);
                    }
                }
            }
            finally
            {
                gd.SetRenderTargets(targets); gd.BlendState = blend; gd.DepthStencilState = depth;
                gd.RasterizerState = raster; gd.SamplerStates[0] = sampler;
            }
            _lastFace = face; _lastTime = seconds;
            return _surface;
        }

        public void Dispose()
        {
            _disposed = true; _surface?.Dispose(); _effect?.Dispose(); _portraitBlend.Dispose();
            // BMD models, textures and mesh buffers belong to the shared loaders.
        }
    }
}
