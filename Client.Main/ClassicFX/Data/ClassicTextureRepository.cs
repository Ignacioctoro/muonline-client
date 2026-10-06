using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Data.Texture;
using Client.Main.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.ClassicFX.Data
{
    /// <summary>
    /// Una textura ya resuelta desde el Data clásico.
    /// </summary>
    public sealed class ClassicTextureResource
    {
        public int Type
        {
            get;
        }

        public string Path
        {
            get;
        }

        public TextureData Data
        {
            get;
        }

        public Texture2D Texture
        {
            get;
        }

        public SamplerState SamplerState
        {
            get;
        }

        public bool IsReady =>
            Data != null &&
            Texture != null &&
            !Texture.IsDisposed;

        public ClassicTextureResource(
            int type,
            string path,
            TextureData data,
            Texture2D texture,
            SamplerState samplerState)
        {
            Type =
                type;

            Path =
                path;

            Data =
                data;

            Texture =
                texture;

            SamplerState =
                samplerState;
        }
    }

    internal readonly struct ClassicTextureDefinition
    {
        public int Type
        {
            get;
        }

        public string Path
        {
            get;
        }

        public SamplerState SamplerState
        {
            get;
        }

        public ClassicTextureDefinition(
            int type,
            string path,
            SamplerState samplerState)
        {
            Type =
                type;

            Path =
                path;

            SamplerState =
                samplerState;
        }
    }

    /// <summary>
    /// Equivalente inicial del array global Bitmaps[]
    /// utilizado por el Main.
    ///
    /// Por ahora cargamos únicamente un conjunto pequeño
    /// necesario para validar ClassicSpriteRenderer.
    ///
    /// Este catálogo crecerá cuando portemos las familias
    /// Particle / Joint / Effect.
    /// </summary>
    public sealed class ClassicTextureRepository
    {
        private static readonly
            ClassicTextureDefinition[]
            CoreDefinitions =
            [
                new(
                    ClassicTextureIds.BitmapLight,
                    "Effect/flare01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapShiny,
                    "Effect/Shiny01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFlare,
                    "Effect/Flare.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFlareBlue,
                    "Effect/flareBlue.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFlareForce,
                    "Effect/NSkill.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFlareRed,
                    "Effect/flareRed.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFormationMark,
                    "Effect/FormationMark.tga",
                    SamplerState.PointClamp),

                new(
                    ClassicTextureIds.BitmapPinLight,
                    "Effect/pin_lights.jpg",
                    SamplerState.LinearClamp)
            ];

        private readonly
            Dictionary<int, ClassicTextureResource>
            _resources =
                new();

        private bool
            _loaded;

        public async Task LoadCoreAsync()
        {
            if (_loaded)
            {
                return;
            }

            for (int i = 0;
                 i < CoreDefinitions.Length;
                 i++)
            {
                ClassicTextureDefinition definition =
                    CoreDefinitions[i];

                TextureData data =
                    await TextureLoader
                        .Instance
                        .Prepare(
                            definition.Path);

                if (data == null)
                {
                    Console.WriteLine(
                        $"[ClassicFX] Texture data not found: " +
                        $"{definition.Type} -> {definition.Path}");

                    continue;
                }

                Texture2D texture =
                    TextureLoader
                        .Instance
                        .GetTexture2D(
                            definition.Path);

                if (texture == null)
                {
                    Console.WriteLine(
                        $"[ClassicFX] Texture2D could not be created: " +
                        $"{definition.Type} -> {definition.Path}");

                    continue;
                }

                _resources[
                    definition.Type
                ] =
                    new ClassicTextureResource(
                        definition.Type,
                        definition.Path,
                        data,
                        texture,
                        definition.SamplerState);
            }

            _loaded =
                true;

            Console.WriteLine(
                $"[ClassicFX] Loaded {_resources.Count} core textures.");
        }

        public bool TryGet(
            int type,
            out ClassicTextureResource resource)
        {
            if (_resources.TryGetValue(
                    type,
                    out resource))
            {
                return
                    resource != null &&
                    resource.IsReady;
            }

            resource =
                null;

            return false;
        }
    }
}