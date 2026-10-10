namespace Client.Main.ClassicFX.Data
{
    /// <summary>
    /// IDs originales del catálogo BITMAP_* de MU.
    ///
    /// Los valores corresponden a _TextureIndex.h del Main
    /// con los defines activos de la referencia actual de MuMain.
    ///
    /// NO renumerar.
    /// </summary>
    public static class ClassicTextureIds
    {
        // -------------------------------------------------------------
        // World / interface / event bitmaps usados por ClassicFX.
        // -------------------------------------------------------------

        public const int BitmapRainCircle =
            30354;

        public const int BitmapCursedTempleEffectMasker =
            31744;

        public const int BitmapGhostCloud1 =
            31746;

        public const int BitmapGhostCloud2 =
            31747;

        public const int BitmapTorchFire =
            31748;

        public const int BitmapEventCloud =
            31749;

        public const int BitmapExtLoginImpact =
            31108;

        // -------------------------------------------------------------
        // Effect texture range.
        // -------------------------------------------------------------

        public const int BitmapLight =
            32002;

        public const int BitmapBlur =
            32018;

        public const int BitmapBlur2 =
            32243;

        public const int BitmapSwordForce =
            32039;

        public const int BitmapChrome =
            32042;

        public const int BitmapSpark =
            32048;

        public const int BitmapEnergy =
            32054;

        public const int BitmapLightning =
            32065;

        public const int BitmapFire =
            32071;

        public const int BitmapFlame =
            32077;

        public const int BitmapBlood =
            32083;

        public const int BitmapExplotion =
            32095;

        public const int BitmapSmoke =
            32101;

        public const int BitmapShiny =
            32113;

        public const int BitmapFlower01 =
            32126;

        public const int BitmapFlare =
            32131;

        public const int BitmapJointThunder =
            32134;

        public const int BitmapJointLaser =
            32137;

        public const int BitmapMagic =
            32140;

        public const int BitmapCloud =
            32146;

        public const int BitmapSpotWater =
            32153;

        public const int BitmapFirecracker =
            32211;

        public const int BitmapBubble =
            32214;

        // Original _TextureIndex.h: JOINT_FORCE lies between BUBBLE and CHROME2.
        public const int BitmapJointForce =
            32215;

        public const int BitmapChrome2 =
            32216;

        public const int BitmapJointSpirit =
            32220;

        public const int BitmapJointFire =
            32221;

        public const int BitmapJointSpark =
            32222;

        // Original _TextureIndex.h: joint energy/healing slots.
        public const int BitmapJointEnergy =
            32223;

        public const int BitmapJointHealing =
            32224;

        public const int BitmapFlareBlue =
            32229;

        public const int BitmapFlareForce =
            32230;

        public const int BitmapFlareRed =
            32231;

        public const int BitmapFlash =
            32236;

        public const int BitmapInferno =
            32237;

        public const int BitmapLava =
            32238;

        // Native BITMAP_CRATER is immediately before FORMATION_MARK.
        public const int BitmapCrater =
            32240;

        public const int BitmapFormationMark =
            32241;

        public const int BitmapPlus =
            32242;

        // _TextureIndex.h immediately after BITMAP_FENRIR_THUNDER 32250.
        // Original five-frame terrain sequence, do not renumber.
        public const int BitmapFenrirFootThunder1 =
            32251;
        public const int BitmapFenrirFootThunder2 =
            32252;
        public const int BitmapFenrirFootThunder3 =
            32253;
        public const int BitmapFenrirFootThunder4 =
            32254;
        public const int BitmapFenrirFootThunder5 =
            32255;

        // Original _TextureIndex.h: scorpion tail precedes DS_EFFECT.
        public const int BitmapScolpionTail =
            32256;

        public const int BitmapDsEffect =
            32257;

        public const int BitmapDsShock =
            32258;

        public const int BitmapClud64 =
            32260;

        public const int BitmapChrome3 =
            32267;

        // Alias legible. El nombre histórico del Main es CLUD64.
        public const int BitmapCloud64 =
            BitmapClud64;

        public const int BitmapBlueBlur =
            32261;

        public const int BitmapAdvSmoke =
            32263;

        public const int BitmapPoundingBall =
            32265;

        public const int BitmapHole =
            32266;

        public const int BitmapChromeEnergy2 =
            32271;

        // MuMain _TextureIndex.h, BITMAP_PIERCING.
        public const int BitmapPiercing =
            32275;

        public const int BitmapShockWave =
            32278;

        // Adjacent native BITMAP_DAMAGE_01_MONO (_TextureIndex.h).
        public const int BitmapDamage01Mono =
            BitmapShockWave + 1;

        public const int BitmapSwordEffectMono =
            32280;

        public const int BitmapTrueFire =
            32282;

        public const int BitmapTrueBlue =
            32283;

        public const int BitmapJointSpirit2 =
            32284;

        public const int BitmapWaterfall1 =
            32286;

        public const int BitmapWaterfall2 =
            32287;

        public const int BitmapWaterfall3 =
            32288;

        public const int BitmapWaterfall4 =
            32289;

        public const int BitmapWaterfall5 =
            32290;

        public const int BitmapEffect =
            32292;

        public const int BitmapTwinTailWater =
            32300;

        public const int BitmapSnowEffect1 =
            32308;

        public const int BitmapSnowEffect2 =
            32309;

        public const int BitmapGmAurora =
            32312;

        // Original _TextureIndex.h, four slots after LuckyCharmEffect53.
        public const int BitmapLuckySealEffect =
            32324;

        public const int BitmapCherryBlossomEventPetal =
            32353;

        public const int BitmapCherryBlossomEventFlower =
            32354;

        public const int BitmapExplotionMono =
            32356;

        public const int BitmapFireRed =
            32368;

        public const int BitmapFireCursedLich =
            32369;

        public const int BitmapLeafTotemGolem =
            32370;

        public const int BitmapSummonSahamuttExplosion =
            32372;

        // Alias para no romper el nombre ya usado por el renderer.
        public const int BitmapSummonSahamutExplosion =
            BitmapSummonSahamuttExplosion;

        public const int BitmapPinLight =
            32374;

        public const int BitmapDrainLifeGhost =
            32375;

        // MuMain _TextureIndex.h: immediately after DRAIN_LIFE_GHOST.
        public const int BitmapMagicZin =
            32376;

        public const int BitmapOrora =
            32377;

        public const int BitmapSmokeLine1 =
            32384;

        public const int BitmapSmokeLine2 =
            32385;

        public const int BitmapSmokeLine3 =
            32386;

        public const int BitmapLightningMega1 =
            32387;

        public const int BitmapLightningMega2 =
            32388;

        public const int BitmapLightningMega3 =
            32389;

        public const int BitmapFireHik1 =
            32390;

        public const int BitmapFireHik3 =
            32391;

        public const int BitmapFireHik1Mono =
            32392;

        public const int BitmapFireHik2Mono =
            32393;

        public const int BitmapFireHik3Mono =
            32394;

        public const int BitmapRaklionClouds =
            32397;

        // MuMain BITMAP_TWLIGHT (immediately before 2LINE_GHOST).
        public const int BitmapTwlight = 32402;

        public const int Bitmap2LineGhost =
            32403;

        public const int BitmapAgAdditionEffect =
            32434;

        public const int BitmapSbumb =
            32466;

        public const int BitmapForcePillar =
            32467;

        public const int BitmapSwordEff =
            32468;

        public const int BitmapDamage1 =
            32469;

        public const int BitmapGroundWind =
            32470;

        public const int BitmapDamage2 =
            32472;

        public const int BitmapGroundSmoke =
            32477;
    }
}
