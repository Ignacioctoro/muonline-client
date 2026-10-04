using Client.Data;
using Client.Data.BMD;
using Client.Main;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Objects.Effects;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;

namespace Client.Main.Objects.Player
{
    public class WeaponObject : ModelObject
    {
        private int _type;
        private new ILogger _logger = ModelObject.AppLoggerFactory?.CreateLogger<WeaponObject>();
        private readonly WeaponTrailEffect _trail;
        private Vector3 _trailTipLocal;
        private int _trailTipBone = -1;
        private int _trailLevelCache = -1;
        private bool _trailExcellentCache;
        private bool _trailAncientCache;

        public new int Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    _ = OnChangeTypeAsync();
                }
            }
        }

        public bool IsRightHand { get; set; }
        public string TexturePath { get; set; }

        public WeaponObject()
        {
            RenderShadow = true;
            LinkParentAnimation = true;

            if (Constants.ENABLE_WEAPON_TRAIL)
            {
                _trail = new WeaponTrailEffect
                {
                    Hidden = true
                };
                _trail.SamplePoint = SampleWeaponTip;
                Children.Add(_trail);
            }
        }

        private async Task OnChangeTypeAsync()
        {
            ParentBoneLink = IsRightHand ? 10 : 15;

            string modelPath = GetWeaponPath(Type);
            if (string.IsNullOrEmpty(modelPath))
            {
                Model = null;
                return;
            }

            Model = await BMDLoader.Instance.Prepare(modelPath);
            if (Model == null)
            {
                _logger?.LogWarning("WeaponObject: Failed to load model for Type {Type}. Path: {Path}", Type, modelPath);
                Status = Models.GameControlStatus.Error;
            }
            else
            {
                UpdateTrailFromModel();
            }
        }

        private static string GetWeaponPath(int type)
        {
            var modelType = (ModelType)type;
            int groupBase = type / 512 * 512;
            int id = type - groupBase;

            string category = ((ModelType)groupBase).ToString().Replace("ITEM_GROUP_", "").Split('_')[0];

            if (category == "MACE") category = "Mace";

            if (id >= 0)
            {
                return $"Item/{category}{id + 1:D2}.bmd";
            }

            return null;
        }
                // ================================================================
        // CLASSIC MU - FLAMBERGE
        //
        // Original client:
        // MODEL_SWORD + 26
        // Data/Item/Sword_27.bmd
        //
        // Meshes:
        // 0 = normal
        // 1 = bright + chrome
        // 2 = normal
        // 3 = bright
        // 4 = bright
        // 5 = bright + animated V offset
        // ================================================================

        private bool IsClassicFlamberge =>
            ItemGroup == 0 &&
            ItemNumber == 26;

        protected override bool IsBlendMesh(int mesh)
        {
            if (IsClassicFlamberge)
            {
                switch (mesh)
                {
                    case 1:
                    case 3:
                    case 4:
                    case 5:
                        return true;
                }
            }

            return base.IsBlendMesh(mesh);
        }

        protected override bool ShouldApplyItemMaterial(int meshIndex)
        {
            if (IsClassicFlamberge)
            {
                // These meshes are effect layers in the original client.
                //
                // Do NOT run the generic +7/+15 item material renderer over
                // them because it converts the effect geometry into normal
                // weapon surfaces.
                switch (meshIndex)
                {
                    case 1:
                    case 3:
                    case 4:
                    case 5:
                        return false;
                }
            }

            return base.ShouldApplyItemMaterial(meshIndex);
        }
        public override void DrawMesh(int mesh)
        {
            if (IsClassicFlamberge)
            {
                // ============================================================
                // MODEL_SWORD + 26 - Flamberge
                // ============================================================

                // Mesh 1:
                //
                // BRIGHT reddish diffuse
                // +
                // BRIGHT / CHROME white
                if (mesh == 1)
                {
                    DrawClassicBrightDiffuseWithChrome(
                        mesh,
                        new Vector3(
                            1.0f,
                            0.0f,
                            0.2f));

                    return;
                }

                // Meshes 3 / 4:
                //
                // Original:
                // RenderMesh(
                //     mesh,
                //     RENDER_TEXTURE | RENDER_BRIGHT,
                //     ...);
                //
                // BodyLight remains white from the previous pass.
                if (mesh == 3 ||
                    mesh == 4)
                {
                    DrawClassicFlambergeBrightMesh(
                        mesh);

                    return;
                }

                // Mesh 5:
                //
                // RENDER_TEXTURE | RENDER_BRIGHT
                // +
                // animated V texture coordinate.
                if (mesh == 5)
                {
                    DrawClassicFlambergeAnimatedFlame(
                        mesh);

                    return;
                }
            }

            // Meshes 0 / 2 and every other weapon use the
            // normal renderer.
            base.DrawMesh(mesh);
        }

                private void DrawClassicFlambergeAnimatedFlame(int mesh)
        {
            if (!TryGetDerivedMeshRenderData(
                    mesh,
                    out VertexBuffer vertexBuffer,
                    out IndexBuffer indexBuffer,
                    out Texture2D texture))
            {
                return;
            }

            var effect =
                GraphicsManager.Instance.ItemMaterialEffect;

            if (effect == null)
            {
                return;
            }

            var gd = GraphicsDevice;

            var previousRasterizer = gd.RasterizerState;
            var previousBlend = gd.BlendState;
            var previousDepth = gd.DepthStencilState;

            try
            {
                // 4-frame vertical atlas:
                // V = 0.00 / 0.25 / 0.50 / 0.75
                //
                // Ajusta el 80 si lo quieres más rápido o más lento.
                int frame =
                    (Environment.TickCount / 80) & 3;

                float vOffset =
                    frame * 0.25f;

                effect.CurrentTechnique =
                    effect.Techniques[0];

                GraphicsManager.Instance
                    .ShadowMapRenderer
                    ?.ApplyShadowParameters(effect);

                effect.Parameters["World"]
                    ?.SetValue(WorldPosition);

                effect.Parameters["View"]
                    ?.SetValue(Camera.Instance.View);

                effect.Parameters["Projection"]
                    ?.SetValue(Camera.Instance.Projection);

                effect.Parameters["DiffuseTexture"]
                    ?.SetValue(texture);

                effect.Parameters["Time"]
                    ?.SetValue(Environment.TickCount * 0.001f);

                effect.Parameters["Alpha"]
                    ?.SetValue(TotalAlpha);

                effect.Parameters["PassMode"]
                    ?.SetValue(6);

                effect.Parameters["MaterialColor"]
                    ?.SetValue(Vector3.One);

                effect.Parameters["MaterialIntensity"]
                    ?.SetValue(1.0f);

                effect.Parameters["DiffuseUVOffset"]
                    ?.SetValue(new Vector2(0.0f, vOffset));

                effect.Parameters["BaseLightScale"]
                    ?.SetValue(1.0f);

                effect.Parameters["ShadowStrength"]
                    ?.SetValue(0.0f);

                gd.SetVertexBuffer(vertexBuffer);
                gd.Indices = indexBuffer;

                gd.RasterizerState =
                    RasterizerState.CullNone;

                gd.BlendState =
                _classicItemBrightAdditive;

                gd.DepthStencilState =
                    GraphicsManager.ReadOnlyDepth;

                int primitiveCount =
                    indexBuffer.IndexCount / 3;

                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    gd.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        0,
                        primitiveCount);
                }
            }
            finally
            {
                effect.Parameters["PassMode"]
                    ?.SetValue(0);

                effect.Parameters["DiffuseUVOffset"]
                    ?.SetValue(Vector2.Zero);

                effect.Parameters["MaterialColor"]
                    ?.SetValue(Vector3.One);

                effect.Parameters["MaterialIntensity"]
                    ?.SetValue(1.0f);

                gd.RasterizerState = previousRasterizer;
                gd.BlendState = previousBlend;
                gd.DepthStencilState = previousDepth;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (_trail != null &&
                (_trailLevelCache != ItemLevel || _trailExcellentCache != IsExcellentItem || _trailAncientCache != IsAncientItem))
            {
                RefreshTrailColor();
            }

            base.Update(gameTime);
            // Force invalidation is now handled at parent level in ModelObject.Update()
        }
                private void DrawClassicFlambergeBrightMesh(int mesh)
        {
            if (!TryGetDerivedMeshRenderData(
                    mesh,
                    out VertexBuffer vertexBuffer,
                    out IndexBuffer indexBuffer,
                    out Texture2D texture))
            {
                return;
            }

            var effect =
                GraphicsManager.Instance.ItemMaterialEffect;

            if (effect == null)
            {
                return;
            }

            var gd =
                GraphicsDevice;

            var previousRasterizer =
                gd.RasterizerState;

            var previousBlend =
                gd.BlendState;

            var previousDepth =
                gd.DepthStencilState;

            try
            {
                effect.CurrentTechnique =
                    effect.Techniques[0];

                GraphicsManager.Instance
                    .ShadowMapRenderer
                    ?.ApplyShadowParameters(effect);

                effect.Parameters["World"]
                    ?.SetValue(WorldPosition);

                effect.Parameters["View"]
                    ?.SetValue(Camera.Instance.View);

                effect.Parameters["Projection"]
                    ?.SetValue(Camera.Instance.Projection);

                effect.Parameters["DiffuseTexture"]
                    ?.SetValue(texture);

                effect.Parameters["Time"]
                    ?.SetValue(Environment.TickCount * 0.001f);

                effect.Parameters["Alpha"]
                    ?.SetValue(TotalAlpha);

                // PassMode 5 is our direct diffuse BRIGHT pass.
                effect.Parameters["PassMode"]
                    ?.SetValue(5);

                // The original leaves BodyLight white after the
                // second Flamberge mesh-1 pass.
                effect.Parameters["MaterialColor"]
                    ?.SetValue(Vector3.One);

                effect.Parameters["MaterialIntensity"]
                    ?.SetValue(1.0f);

                effect.Parameters["BaseLightScale"]
                    ?.SetValue(1.0f);

                effect.Parameters["ShadowStrength"]
                    ?.SetValue(0.0f);

                effect.Parameters["DiffuseUVOffset"]
                    ?.SetValue(Vector2.Zero);

                gd.SetVertexBuffer(
                    vertexBuffer);

                gd.Indices =
                    indexBuffer;

                // Original MU:
                // EnableAlphaBlend()
                // glBlendFunc(GL_ONE, GL_ONE)
                gd.BlendState =
                    _classicItemBrightAdditive;

                // Bright/effect geometry must not write depth.
                gd.DepthStencilState =
                    GraphicsManager.ReadOnlyDepth;

                gd.RasterizerState =
                    RasterizerState.CullNone;

                int primitiveCount =
                    indexBuffer.IndexCount / 3;

                foreach (EffectPass pass
                    in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    gd.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        0,
                        primitiveCount);
                }
            }
            finally
            {
                effect.Parameters["PassMode"]
                    ?.SetValue(0);

                effect.Parameters["MaterialColor"]
                    ?.SetValue(Vector3.One);

                effect.Parameters["MaterialIntensity"]
                    ?.SetValue(1.0f);

                effect.Parameters["DiffuseUVOffset"]
                    ?.SetValue(Vector2.Zero);

                gd.RasterizerState =
                    previousRasterizer;

                gd.BlendState =
                    previousBlend;

                gd.DepthStencilState =
                    previousDepth;
            }
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();
            UpdateTrailFromModel();
        }

        private void UpdateTrailFromModel()
        {
            if (_trail == null)
                return;

            _trail.SetTipFromModel(Model);
            ComputeTrailTip(Model);
            RefreshTrailColor();
        }

        private void RefreshTrailColor()
        {
            if (_trail == null)
                return;

            _trail.SetTrailColor(GetTrailColor());

            _trailLevelCache = ItemLevel;
            _trailExcellentCache = IsExcellentItem;
            _trailAncientCache = IsAncientItem;
        }

        private Color GetTrailColor()
        {
            if (IsAncientItem) return new Color(0.45f, 0.9f, 1f);
            if (IsExcellentItem) return new Color(0.5f, 1f, 0.6f);
            if (ItemLevel >= 7) return new Color(1f, 0.82f, 0.55f);
            return new Color(0.8f, 0.9f, 1f);
        }

        private Vector3 SampleWeaponTip()
        {
            if (_trailTipBone >= 0 && BoneTransform != null && _trailTipBone < BoneTransform.Length)
            {
                Vector3 animated = Vector3.Transform(_trailTipLocal, BoneTransform[_trailTipBone]);
                return Vector3.Transform(animated, WorldPosition);
            }

            // Fallback: use object origin (no tip data).
            return Vector3.Transform(_trailTipLocal, WorldPosition);
        }

        private void ComputeTrailTip(BMD model)
        {
            _trailTipBone = -1;
            _trailTipLocal = Vector3.Zero;

            if (model?.Meshes == null || model.Meshes.Length == 0)
                return;

            Matrix[] restBones = BuildRestPose(model);
            if (restBones == null)
                return;

            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            Vector3 sum = Vector3.Zero;
            int count = 0;

            foreach (var mesh in model.Meshes)
            {
                var verts = mesh.Vertices;
                if (verts == null) continue;

                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 pos = TransformVertex(restBones, verts[i]);
                    min = Vector3.Min(min, pos);
                    max = Vector3.Max(max, pos);
                    sum += pos;
                    count++;
                }
            }

            if (count == 0)
                return;

            Vector3 centroid = sum / count;
            Vector3 extents = max - min;

            int axis = 0;
            float axisLen = extents.X;
            if (extents.Y > axisLen) { axis = 1; axisLen = extents.Y; }
            if (extents.Z > axisLen) { axis = 2; axisLen = extents.Z; }

            Vector3 axisVec = axis switch
            {
                0 => Vector3.UnitX,
                1 => Vector3.UnitY,
                _ => Vector3.UnitZ
            };

            Vector3 refPoint = (restBones != null && restBones.Length > 0) ? restBones[0].Translation : Vector3.Zero;

            if (!SelectTipVertex(model, restBones, centroid, axisVec, refPoint, preferSkinned: true, out _trailTipLocal, out _trailTipBone))
            {
                SelectTipVertex(model, restBones, centroid, axisVec, refPoint, preferSkinned: false, out _trailTipLocal, out _trailTipBone);
            }
        }

        private static bool SelectTipVertex(BMD model, Matrix[] restBones, Vector3 centroid, Vector3 axisVec, Vector3 refPoint, bool preferSkinned, out Vector3 tipLocal, out int tipBone)
        {
            bool hasMin = false, hasMax = false;
            Vector3 minLocal = Vector3.Zero, maxLocal = Vector3.Zero;
            int minBone = -1, maxBone = -1;
            float minProj = 0f, maxProj = 0f;
            Vector3 minPosWorld = Vector3.Zero, maxPosWorld = Vector3.Zero;

            foreach (var mesh in model.Meshes)
            {
                var verts = mesh.Vertices;
                if (verts == null) continue;

                for (int i = 0; i < verts.Length; i++)
                {
                    var vert = verts[i];
                    if (preferSkinned && vert.Node < 0)
                        continue;

                    Vector3 pos = TransformVertex(restBones, vert);
                    float proj = Vector3.Dot(pos - centroid, axisVec);
                    if (!hasMax || proj > maxProj)
                    {
                        hasMax = true;
                        maxProj = proj;
                        maxLocal = ToXna(vert.Position);
                        maxBone = vert.Node;
                        maxPosWorld = pos;
                    }
                    if (!hasMin || proj < minProj)
                    {
                        hasMin = true;
                        minProj = proj;
                        minLocal = ToXna(vert.Position);
                        minBone = vert.Node;
                        minPosWorld = pos;
                    }
                }
            }

            if (!hasMin && !hasMax)
            {
                tipLocal = Vector3.Zero;
                tipBone = -1;
                return false;
            }

            // Choose the endpoint farther from the weapon's root/reference point to bias toward the blade tip.
            float maxDist = hasMax ? Vector3.DistanceSquared(refPoint, maxPosWorld) : float.MinValue;
            float minDist = hasMin ? Vector3.DistanceSquared(refPoint, minPosWorld) : float.MinValue;

            // Recompute world positions if we skipped skinning (posWorld would be local).
            if (hasMax && maxBone >= 0 && restBones != null && maxBone < restBones.Length)
            {
                maxDist = Vector3.DistanceSquared(refPoint, Vector3.Transform(maxLocal, restBones[maxBone]));
            }
            if (hasMin && minBone >= 0 && restBones != null && minBone < restBones.Length)
            {
                minDist = Vector3.DistanceSquared(refPoint, Vector3.Transform(minLocal, restBones[minBone]));
            }

            if (maxDist > minDist || !hasMin)
            {
                tipLocal = maxLocal;
                tipBone = maxBone;
            }
            else if (minDist > maxDist || !hasMax)
            {
                tipLocal = minLocal;
                tipBone = minBone;
            }
            else
            {
                // Distances tied; pick the side with larger absolute projection.
                if (MathF.Abs(maxProj) >= MathF.Abs(minProj))
                {
                    tipLocal = maxLocal;
                    tipBone = maxBone;
                }
                else
                {
                    tipLocal = minLocal;
                    tipBone = minBone;
                }
            }

            return true;
        }

        private static Matrix[] BuildRestPose(BMD model)
        {
            if (model?.Bones == null || model.Bones.Length == 0)
                return null;

            var bones = model.Bones;
            var result = new Matrix[bones.Length];

            for (int i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                Matrix local = Matrix.Identity;

                if (bone != BMDTextureBone.Dummy &&
                    bone.Matrixes != null &&
                    bone.Matrixes.Length > 0 &&
                    bone.Matrixes[0].Quaternion?.Length > 0 &&
                    bone.Matrixes[0].Position?.Length > 0)
                {
                    var bm = bone.Matrixes[0];
                    local = Matrix.CreateFromQuaternion(ToXna(bm.Quaternion[0]));
                    local.Translation = ToXna(bm.Position[0]);
                }

                if (bone.Parent >= 0 && bone.Parent < bones.Length)
                    result[i] = local * result[bone.Parent];
                else
                    result[i] = local;
            }

            return result;
        }

        private static Vector3 TransformVertex(Matrix[] bones, Client.Data.BMD.BMDTextureVertex vert)
        {
            Vector3 local = ToXna(vert.Position);
            if (vert.Node >= 0 && bones != null && vert.Node < bones.Length)
            {
                return Vector3.Transform(local, bones[vert.Node]);
            }
            return local;
        }

        private static Vector3 ToXna(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
        private static Quaternion ToXna(System.Numerics.Quaternion q) => new Quaternion(q.X, q.Y, q.Z, q.W);
    }
}
