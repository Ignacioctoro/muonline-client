using Microsoft.Xna.Framework;

namespace Client.Main.Content
{
    /// <summary>
    /// Allows objects to procedurally modify texture coordinates during buffer generation.
    /// Implement alongside IVertexDeformer and return from GetVertexDeformer().
    /// BMDLoader will detect the interface and apply UV transformations.
    /// </summary>
    public interface ITexCoordDeformer
    {
        Vector2 DeformTexCoord(float u, float v);
    }

    /// <summary>
    /// Optional mesh-specific UV deformation. Used by native Fury Strike
    /// EarthQuake overlays so that unblended BMD meshes remain unchanged.
    /// </summary>
    public interface IMeshTexCoordDeformer : ITexCoordDeformer
    {
        Vector2 DeformTexCoord(int meshIndex, float u, float v);
    }
}
