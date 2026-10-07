using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        /// <summary>
        /// Dispatcher completo de CreateParticle().
        ///
        /// El switch original es enorme, por eso se conserva en cuatro
        /// archivos contiguos A/B/C/D, sin dividir por skill ni por clase.
        /// </summary>
        private void InitializeParticleByType(
            ref ClassicParticle particle,
            in Vector3 sourcePosition,
            in Vector3 sourceAngle,
            in Vector3 sourceLight,
            float sourceScale)
        {
            int type = particle.Type;

            if (type == ClassicTextureIds.BitmapEffect ||
                (type >= ClassicTextureIds.BitmapFlower01 &&
                 type <= ClassicTextureIds.BitmapFlower01 + 2) ||
                type == ClassicTextureIds.BitmapFlareBlue ||
                type == ClassicTextureIds.BitmapFlare + 1 ||
                type == ClassicTextureIds.BitmapBubble ||
                type == ClassicTextureIds.BitmapLightning + 1 ||
                type == ClassicTextureIds.BitmapLightning ||
                type == ClassicTextureIds.BitmapChromeEnergy2 ||
                type == ClassicTextureIds.BitmapFireCursedLich ||
                type == ClassicTextureIds.BitmapFireHik2Mono ||
                type == ClassicTextureIds.BitmapLeafTotemGolem ||
                (type >= ClassicTextureIds.BitmapFire &&
                 type <= ClassicTextureIds.BitmapFire + 3) ||
                type == ClassicTextureIds.BitmapFlame ||
                type == ClassicTextureIds.BitmapFireRed ||
                type == ClassicTextureIds.BitmapRainCircle ||
                type == ClassicTextureIds.BitmapRainCircle + 1 ||
                type == ClassicTextureIds.BitmapEnergy ||
                type == ClassicTextureIds.BitmapMagic ||
                type == ClassicTextureIds.BitmapFlare ||
                type == ClassicTextureIds.BitmapLight + 2 ||
                type == ClassicTextureIds.BitmapMagic + 1 ||
                type == ClassicTextureIds.BitmapBlueBlur ||
                type == ClassicTextureIds.BitmapClud64 ||
                type == ClassicTextureIds.BitmapLight + 3 ||
                type == ClassicTextureIds.BitmapTwinTailWater)
            {
                InitializeParticleCreateA(
                    ref particle,
                    sourcePosition,
                    sourceAngle,
                    sourceLight,
                    sourceScale);
                return;
            }

            if ((type >= ClassicTextureIds.BitmapSmoke &&
                 type <= ClassicTextureIds.BitmapSmoke + 4) ||
                (type >= ClassicTextureIds.BitmapSmokeLine1 &&
                 type <= ClassicTextureIds.BitmapSmokeLine3) ||
                (type >= ClassicTextureIds.BitmapLightningMega1 &&
                 type <= ClassicTextureIds.BitmapLightningMega3) ||
                type == ClassicTextureIds.BitmapFireHik1 ||
                type == ClassicTextureIds.BitmapFireHik1Mono ||
                type == ClassicTextureIds.BitmapFireHik3 ||
                type == ClassicTextureIds.BitmapFireHik3Mono ||
                type == ClassicTextureIds.BitmapLight + 1 ||
                type == ClassicTextureIds.BitmapSpark ||
                type == ClassicTextureIds.BitmapSpark + 1 ||
                type == ClassicTextureIds.BitmapSpark + 2 ||
                type == ClassicTextureIds.BitmapExplotionMono ||
                type == ClassicTextureIds.BitmapExplotion ||
                type == ClassicTextureIds.BitmapSummonSahamuttExplosion ||
                type == ClassicTextureIds.BitmapSpotWater ||
                type == ClassicTextureIds.BitmapFlareRed ||
                type == ClassicTextureIds.BitmapExplotion + 1)
            {
                InitializeParticleCreateB(
                    ref particle,
                    sourcePosition,
                    sourceAngle,
                    sourceLight,
                    sourceScale);
                return;
            }

            if (type == ClassicTextureIds.BitmapShiny ||
                type == ClassicTextureIds.BitmapCherryBlossomEventPetal ||
                type == ClassicTextureIds.BitmapCherryBlossomEventFlower ||
                type == ClassicTextureIds.BitmapShiny + 1 ||
                type == ClassicTextureIds.BitmapShiny + 2 ||
                type == ClassicTextureIds.BitmapShiny + 4 ||
                type == ClassicTextureIds.BitmapShiny + 6 ||
                type == ClassicTextureIds.BitmapPinLight ||
                type == ClassicTextureIds.BitmapOrora ||
                type == ClassicTextureIds.BitmapSnowEffect1 ||
                type == ClassicTextureIds.BitmapSnowEffect2 ||
                type == ClassicTextureIds.BitmapDsEffect ||
                type == ClassicTextureIds.BitmapBlood ||
                type == ClassicTextureIds.BitmapBlood + 1 ||
                type == ClassicTextureIds.BitmapFirecracker ||
                type == ClassicTextureIds.BitmapSwordForce ||
                type == ClassicTextureIds.BitmapCloud ||
                type == ClassicTextureIds.BitmapTorchFire ||
                type == ClassicTextureIds.BitmapGhostCloud1 ||
                type == ClassicTextureIds.BitmapGhostCloud2 ||
                type == ClassicTextureIds.BitmapLight ||
                type == ClassicTextureIds.BitmapPoundingBall ||
                type == ClassicTextureIds.BitmapAdvSmoke ||
                type == ClassicTextureIds.BitmapAdvSmoke + 1 ||
                type == ClassicTextureIds.BitmapTrueFire ||
                type == ClassicTextureIds.BitmapTrueBlue ||
                type == ClassicTextureIds.BitmapHole ||
                type == ClassicTextureIds.BitmapWaterfall1)
            {
                InitializeParticleCreateC(
                    ref particle,
                    sourcePosition,
                    sourceAngle,
                    sourceLight,
                    sourceScale);
                return;
            }

            InitializeParticleCreateD(
                ref particle,
                sourcePosition,
                sourceAngle,
                sourceLight,
                sourceScale);
        }
    }
}
