using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects
{
    public abstract partial class ModelObject
    {
        /// <summary>
        /// Permite a renderers especializados derivados reutilizar los buffers
        /// animados que ModelObject ya construyó, sin duplicar skinning CPU.
        /// </summary>
        protected bool TryGetDerivedMeshRenderData(
            int mesh,
            out VertexBuffer vertexBuffer,
            out IndexBuffer indexBuffer,
            out Texture2D texture)
        {
            vertexBuffer = null;
            indexBuffer = null;
            texture = null;

            if (Model?.Meshes == null ||
                mesh < 0 ||
                mesh >= Model.Meshes.Length ||
                _boneVertexBuffers == null ||
                _boneIndexBuffers == null ||
                _boneTextures == null ||
                mesh >= _boneVertexBuffers.Length ||
                mesh >= _boneIndexBuffers.Length ||
                mesh >= _boneTextures.Length ||
                _boneVertexBuffers[mesh] == null ||
                _boneIndexBuffers[mesh] == null ||
                _boneTextures[mesh] == null ||
                IsHiddenMesh(mesh))
            {
                return false;
            }

            vertexBuffer = _boneVertexBuffers[mesh];
            indexBuffer = _boneIndexBuffers[mesh];
            texture = _boneTextures[mesh];

            return true;
        }
    }
}