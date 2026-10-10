using System;
using System.Collections.Generic;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Rendering
{
    /// <summary>
    /// MuMain ZzzEffectJoint.cpp RenderJoints(), lines 6981-7352.
    /// Generates the original two crossed tail strips from JOINT.Tails.
    /// All 3D rendering goes through the existing ClassicBillboardRenderer
    /// batch (the quad API is not a billboard: corners are world-space).
    /// This class neither allocates per segment nor owns another GPU renderer.
    /// </summary>
    internal sealed class ClassicJointRenderer
    {
        private readonly ClassicBillboardRenderer _quads;
        private readonly ClassicTextureRepository _textures;
        // One warning per unresolved texture, not one message per frame.
        private readonly HashSet<int> _missingTextureWarnings = new();

        public ClassicJointRenderer(ClassicBillboardRenderer quads,
            ClassicTextureRepository textures)
        {
            _quads = quads ?? throw new ArgumentNullException(nameof(quads));
            _textures = textures ?? throw new ArgumentNullException(nameof(textures));
        }

        public void Begin() => _quads.Begin();
        public void End() => _quads.End();

        public void QueueJoint(ref ClassicJoint joint, byte pass,
            float worldTimeMilliseconds, float frameFactor)
        {
            if (joint.NumTails <= 0 || joint.RenderFace == 0 ||
                joint.Tails == null || joint.Tails.Length < 8)
                return;
            if (pass == 1 && joint.OnlyOneRender == 2 ||
                pass == 2 && joint.OnlyOneRender == 1)
                return;
            if (!_textures.TryGet(joint.TexType, out ClassicTextureResource texture))
            {
                if (_missingTextureWarnings.Add(joint.TexType))
                    Console.WriteLine($"[ClassicFX][Joint] No loaded texture for TexType={joint.TexType} (joint type={joint.Type}, subtype={joint.SubType}).");
                return;
            }

            ClassicBlendMode blend = joint.RenderType switch
            {
                2 => ClassicBlendMode.AlphaTest,
                3 => ClassicBlendMode.Subtract,
                4 => ClassicBlendMode.Luminance,
                _ => ClassicBlendMode.Glow
            };
            if (joint.Type == ClassicFxRuntime.ClassicModelSpearSkill &&
                joint.SubType == 15)
                blend = ClassicBlendMode.Subtract;
            if (joint.Type == ClassicTextureIds.BitmapFlareBlue &&
                joint.SubType == 20)
                blend = ClassicBlendMode.Luminance;

            ClassicDepthMode depth = joint.Type ==
                ClassicTextureIds.BitmapJointHealing && joint.SubType == 8
                ? ClassicDepthMode.Disabled :
                blend == ClassicBlendMode.AlphaTest
                    ? ClassicDepthMode.ReadWrite
                    : ClassicDepthMode.ReadOnly;

            int count = Math.Min(joint.NumTails,
                Math.Min(ClassicJoint.MaxTailSegments - 1,
                    joint.Tails.Length / ClassicJoint.VerticesPerTail - 1));
            int denominator = Math.Max(1, joint.MaxTails - 1);
            float scroll = ((int)worldTimeMilliseconds % 1000) * 0.001f;
            for (int segment = 0; segment < count; segment++)
            {
                if (joint.Type == ClassicTextureIds.BitmapSmoke &&
                    joint.SubType == 0 && segment == 0)
                    continue;
                if (joint.Type == ClassicTextureIds.BitmapJointHealing &&
                    (joint.SubType == 9 || joint.SubType == 10) &&
                    segment == count - 1)
                    continue;

                int offset = segment * ClassicJoint.VerticesPerTail;
                Vector3 c0 = joint.Tails[offset];
                Vector3 c1 = joint.Tails[offset + 1];
                Vector3 c2 = joint.Tails[offset + 2];
                Vector3 c3 = joint.Tails[offset + 3];
                Vector3 n0 = joint.Tails[offset + 4];
                Vector3 n1 = joint.Tails[offset + 5];
                Vector3 n2 = joint.Tails[offset + 6];
                Vector3 n3 = joint.Tails[offset + 7];
                if (!ValidQuad(c0, c1, c2, c3) ||
                    !ValidQuad(n0, n1, n2, n3))
                    continue;

                // Main's tail gap guard prevents giant connecting polygons.
                Vector3 midCur = (c0 + c1) * 0.5f;
                Vector3 midNext = (n0 + n1) * 0.5f;
                if (Vector3.DistanceSquared(midCur, midNext) > 3600f)
                    continue;

                float l1, l2;
                if (joint.TileMapping)
                {
                    l1 = (joint.NumTails - segment) / 16f;
                    l2 = (joint.NumTails - segment - 1) / 16f;
                }
                else if (joint.ReverseUv == 3)
                {
                    l1 = 1f - segment / (float)denominator;
                    l2 = 1f - (segment + 1) / (float)denominator;
                }
                else
                {
                    l1 = (joint.NumTails - segment) / (float)denominator;
                    l2 = (joint.NumTails - segment - 1) / (float)denominator;
                }

                bool thunder = joint.Type == ClassicTextureIds.BitmapJointThunder ||
                    joint.Type == ClassicTextureIds.BitmapJointThunder + 1;
                if (thunder)
                {
                    l1 = l1 * MathF.Pow(2f, frameFactor) - scroll;
                    l2 = l2 * MathF.Pow(2f, frameFactor) - scroll;
                }
                bool flareForce = joint.Type == ClassicTextureIds.BitmapFlareForce &&
                    (joint.SubType >= 0 && joint.SubType <= 4 ||
                     joint.SubType >= 11 && joint.SubType <= 13);
                if (flareForce)
                {
                    float halfTails = Math.Max(1, (joint.MaxTails - 1) / 2);
                    l1 = (joint.NumTails - segment) / halfTails - scroll;
                    l2 = (joint.NumTails - segment - 1) / halfTails - scroll;
                }
                if (joint.TileMapping)
                {
                    float t = MathF.Pow(2f, frameFactor);
                    l1 = l1 * t - scroll * t;
                    l2 = l2 * t - scroll * t;
                }

                // Healing 9/10 modifies Light during rendering, once per
                // valid native segment, before calculating its face color.
                if (joint.Type == ClassicTextureIds.BitmapJointHealing &&
                    (joint.SubType == 9 || joint.SubType == 10) &&
                    joint.Target.HasOwner)
                    joint.Light *= MathF.Pow(0.9978f, frameFactor);

                Vector3 light = joint.Light;
                if (joint.Type == ClassicTextureIds.BitmapJointForce &&
                    joint.SubType == 0)
                {
                    float lum = (joint.MaxTails - segment) /
                        (float)Math.Max(1, joint.MaxTails) * 2f;
                    lum *= MathF.Pow(MathF.Max(0f, joint.Light.X), frameFactor);
                    light = new Vector3(lum);
                    // The native special FORCE/0 branch draws only face 2,
                    // regardless of RenderFace.
                    _quads.QueueWorldQuad(texture, c0, c1, n1, n0,
                        new Vector2(l1, 0f), new Vector2(l1, 1f),
                        new Vector2(l2, 1f), new Vector2(l2, 0f),
                        light, blend, depth);
                    continue;
                }

                if (joint.Type == ClassicTextureIds.BitmapJointThunder + 1 &&
                    joint.SubType == 0)
                {
                    int tail = (int)joint.Light.Z;
                    if (tail < segment) light = Vector3.Zero;
                    else if (tail == segment)
                    {
                        float lum = joint.Light.Z - segment;
                        light = new Vector3(lum);
                    }
                    else light = new Vector3(0.7f);
                }
                else if (flareForce)
                {
                    float lum = (joint.NumTails - 1 - segment) /
                        (float)Math.Max(1, joint.MaxTails) * 2f;
                    light *= lum;
                }
                else if (joint.Type == ClassicTextureIds.BitmapJointForce &&
                         joint.SubType == 1)
                {
                    float lum = (1f - (joint.NumTails - segment) /
                        (float)Math.Max(1, joint.NumTails)) * 2f;
                    light *= lum;
                }

                float v1 = joint.ReverseUv == 1 ? 1f : 0f;
                float v2 = joint.ReverseUv == 1 ? 0f : 1f;
                if (joint.ReverseUv == 2)
                {
                    l1 = 1f - l1;
                    l2 = 1f - l2;
                }

                // RenderFace.ONE = vertices [2,3], TWO = vertices [0,1].
                // UVs reproduce the native cross-strip winding and scrolling.
                if ((joint.RenderFace & 1) != 0)
                    _quads.QueueWorldQuad(texture, c2, c3, n3, n2,
                        new Vector2(l1, v2), new Vector2(l1, v1),
                        new Vector2(l2, v1), new Vector2(l2, v2),
                        light, blend, depth);
                if ((joint.RenderFace & 2) != 0)
                {
                    float u1 = l1, u2 = l2;
                    if (thunder)
                    {
                        u1 += scroll * 2f;
                        u2 += scroll * 2f;
                    }
                    _quads.QueueWorldQuad(texture, c0, c1, n1, n0,
                        new Vector2(u1, v1), new Vector2(u1, v2),
                        new Vector2(u2, v2), new Vector2(u2, v1),
                        light, blend, depth);
                }
            }
        }

        // MuMain ZzzEffectJoint.cpp IsValidTailQuad():
        // Reject tail quads whose midpoint is effectively world origin.
        private static bool ValidQuad(
            in Vector3 a, in Vector3 b, in Vector3 c, in Vector3 d)
        {
            if (!Finite(a) || !Finite(b) || !Finite(c) || !Finite(d))
                return false;

            Vector3 midpoint = (a + b) * 0.5f;
            float lengthSquared = Vector3.Dot(midpoint, midpoint);
            return float.IsFinite(lengthSquared) && lengthSquared >= 1f;
        }

        private static bool Finite(in Vector3 v) =>
            float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
    }
}
