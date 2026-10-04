#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_5_0
    #define PS_SHADERMODEL ps_5_0
#endif


// ============================================================================
// Classic MU item material renderer
//
// This shader intentionally does NOT implement the previous Neffis
// ghost/rainbow/fresnel system.
//
// It reproduces the classic MU generated UVs used by:
//
// RENDER_CHROME
// RENDER_CHROME2
// RENDER_CHROME4
// RENDER_METAL
//
// The C# renderer performs the classic multi-pass sequence.
// ============================================================================


float4x4 World;
float4x4 View;
float4x4 Projection;


// ---------------------------------------------------------------------------
// BASE ITEM TEXTURE
// ---------------------------------------------------------------------------

texture DiffuseTexture;

sampler2D DiffuseSampler = sampler_state
{
    Texture = <DiffuseTexture>;

    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;

    AddressU = Wrap;
    AddressV = Wrap;
};


// ---------------------------------------------------------------------------
// CLASSIC CHROME / METAL TEXTURE
//
// The same texture parameter is used for:
//
// Chrome01
// Chrome02
// Shiny01
//
// Chrome textures repeat.
// Shiny01/metal uses clamp.
// ---------------------------------------------------------------------------

texture MaterialTexture;


// ============================================================================
// CLASSIC MU MATERIAL SAMPLERS
//
// Original ZzzOpenData.cpp:
//
// Chrome01
//     GL_LINEAR
//     GL_REPEAT
//
// Chrome02
//     GL_NEAREST
//     GL_CLAMP
//
// Shiny01
//     GL_LINEAR
//     GL_CLAMP
// ============================================================================


// RENDER_CHROME
sampler2D Chrome01Sampler = sampler_state
{
    Texture = <MaterialTexture>;

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;

    AddressU = Wrap;
    AddressV = Wrap;
};


// RENDER_CHROME2 / RENDER_CHROME4
sampler2D Chrome02Sampler = sampler_state
{
    Texture = <MaterialTexture>;

    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;

    AddressU = Clamp;
    AddressV = Clamp;
};


// RENDER_METAL
sampler2D Shiny01Sampler = sampler_state
{
    Texture = <MaterialTexture>;

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};


// ---------------------------------------------------------------------------
// MATERIAL PARAMETERS
//
// PassMode:
//
// 0 = base texture
// 1 = RENDER_CHROME
// 2 = RENDER_CHROME2
// 3 = RENDER_CHROME4
// 4 = RENDER_METAL
// ---------------------------------------------------------------------------

int PassMode = 0;

float BaseLightScale = 1.0;

float3 MaterialColor =
    float3(
        1.0,
        1.0,
        1.0);

float MaterialIntensity = 1.0;


// Time is supplied in SECONDS by Neffis.
//
// Original MU WorldTime is milliseconds.
// The classic equations below are converted accordingly.

float Time = 0.0;

float Alpha = 1.0;

// UV offset for special direct-texture animated passes
float2 DiffuseUVOffset =
    float2(0.0, 0.0);


// ============================================================================
// SHADOW MAP
// ============================================================================

texture ShadowMap;

sampler2D ShadowSampler = sampler_state
{
    Texture = <ShadowMap>;

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;

    AddressU = Clamp;
    AddressV = Clamp;
};


float4x4 LightViewProjection;

float2 ShadowMapTexelSize =
    float2(
        1.0 / 2048.0,
        1.0 / 2048.0);

float ShadowBias = 0.0015;
float ShadowNormalBias = 0.0025;

float ShadowsEnabled = 0.0;
float ShadowStrength = 0.5;


// ============================================================================
// VERTEX DATA
// ============================================================================

struct VertexShaderInput
{
    float4 Position
        : POSITION0;

    float4 Color
        : COLOR0;

    float3 Normal
        : NORMAL0;

    float2 TextureCoordinate
        : TEXCOORD0;
};


struct VertexShaderOutput
{
    float4 Position
        : SV_POSITION;

    float3 WorldPosition
        : TEXCOORD0;

    float3 Normal
        : TEXCOORD1;

    float2 TextureCoordinate
        : TEXCOORD2;

    float2 ChromeUV
        : TEXCOORD3;

    float2 Chrome2UV
        : TEXCOORD4;

    float2 Chrome4UV
        : TEXCOORD5;

    float2 MetalUV
        : TEXCOORD6;

    float4 VertexColor
        : COLOR0;
};


// ============================================================================
// SHADOW
// ============================================================================

float SampleShadow(
    float3 worldPos,
    float3 normal)
{
    float4 lightPos =
        mul(
            float4(
                worldPos,
                1.0),
            LightViewProjection);


    float3 proj =
        lightPos.xyz /
        lightPos.w;


    float2 uv =
        proj.xy *
        0.5 +
        0.5;


    float depth =
        proj.z *
        0.5 +
        0.5;


    float2 uvClamped =
        saturate(
            uv);


    float inBounds =
        step(
            abs(
                uv.x -
                uvClamped.x)
            +
            abs(
                uv.y -
                uvClamped.y),

            0.0001);


    float ndotl =
        saturate(
            normal.z);


    float bias =
        ShadowBias
        +
        ShadowNormalBias *
        (1.0 - ndotl);


    float shadow =
        0.0;


    [unroll]
    for (int x = -1;
         x <= 1;
         x++)
    {
        [unroll]
        for (int y = -1;
             y <= 1;
             y++)
        {
            float2 offset =
                float2(
                    x,
                    y)
                *
                ShadowMapTexelSize;


            float sampleDepth =
                tex2D(
                    ShadowSampler,
                    uv + offset)
                .r;


            shadow +=
                step(
                    depth - bias,
                    sampleDepth);
        }
    }


    return
        lerp(
            1.0,

            shadow /
            9.0,

            inBounds *
            ShadowsEnabled);
}


// ============================================================================
// VERTEX SHADER
// ============================================================================

VertexShaderOutput MainVS(
    VertexShaderInput input)
{
    VertexShaderOutput output =
        (VertexShaderOutput)0;


    float4 worldPosition =
        mul(
            input.Position,
            World);


    float4 viewPosition =
        mul(
            worldPosition,
            View);


    output.Position =
        mul(
            viewPosition,
            Projection);


    output.WorldPosition =
        worldPosition.xyz;


    // The CPU BMD renderer has already transformed the normal by the
    // animated bone. We only apply the object's world transform here.

    // IMPORTANT:
    //
    // BMDLoader already transforms normals by their animated bone.
    // The original MU client generates Chrome UVs directly from
    // NormalTransform[] before applying the object's world rotation.
    //
    // Applying World here makes the chrome reflection rotate twice and
    // produces aggressive shimmering while the character moves/turns.

    float3 normal =
        normalize(
            input.Normal);

    output.Normal =
        normal;


    output.TextureCoordinate =
        input.TextureCoordinate;


    output.VertexColor =
        input.Color;


    // ========================================================================
    // CLASSIC MU UV GENERATION
    // ========================================================================


    // ------------------------------------------------------------------------
    // RENDER_CHROME
    //
    // Original:
    //
    // Wave = (WorldTime % 10000) * 0.0001
    //
    // U = Normal.z * 0.5 + Wave
    // V = Normal.y * 0.5 + Wave * 2
    //
    // Time is seconds here:
    //
    // WorldTime / 10000
    // =
    // TimeSeconds / 10
    // ------------------------------------------------------------------------

    float wave =
        frac(
            Time *
            0.1);


    output.ChromeUV =
        float2(
            normal.z *
            0.5 +
            wave,

            normal.y *
            0.5 +
            wave *
            2.0);


    // ------------------------------------------------------------------------
    // RENDER_CHROME2
    //
    // Original:
    //
    // Wave2 =
    //     (WorldTime % 5000) * 0.00024 - 0.4
    //
    // U =
    //     (Normal.z + Normal.x) * 0.8
    //     + Wave2 * 2
    //
    // V =
    //     (Normal.y + Normal.x)
    //     + Wave2 * 3
    //
    // Converted to seconds:
    //
    // period = 5 seconds
    // range  = -0.4 .. 0.8
    // ------------------------------------------------------------------------

    float wave2 =
        frac(
            Time *
            0.2)
        *
        1.2
        -
        0.4;


    output.Chrome2UV =
        float2(
            (normal.z +
             normal.x)
            *
            0.8
            +
            wave2 *
            2.0,

            (normal.y +
             normal.x)
            +
            wave2 *
            3.0);


    // ------------------------------------------------------------------------
    // RENDER_CHROME4
    //
    // Original:
    //
    // L.x = cos(WorldTime * 0.001)
    // L.y = sin(WorldTime * 0.002)
    // L.z = 1
    //
    // Time is already seconds here.
    //
    // IMPORTANT:
    // Do NOT normalize L.
    // The original client does not normalize it.
    // ------------------------------------------------------------------------

    float3 chromeLight =
        float3(
            cos(
                Time),

            sin(
                Time *
                2.0),

            1.0);


    float chrome4U =
        dot(
            normal,
            chromeLight);


    float chrome4V =
        1.0
        -
        dot(
            normal,
            chromeLight);


    chrome4V -=
        normal.z *
        0.5
        +
        wave *
        3.0;


    chrome4U +=
        normal.y *
        0.5
        +
        chromeLight.y *
        3.0;


    output.Chrome4UV =
        float2(
            chrome4U,
            chrome4V);


    // ------------------------------------------------------------------------
    // RENDER_METAL
    //
    // Original fallback chrome coordinates used with BITMAP_SHINY:
    //
    // U = Normal.z * 0.5 + 0.2
    // V = Normal.y * 0.5 + 0.5
    //
    // This is intentionally NOT animated.
    // ------------------------------------------------------------------------

    output.MetalUV =
        float2(
            normal.z *
            0.5 +
            0.2,

            normal.y *
            0.5 +
            0.5);


    return output;
}


// ============================================================================
// CLASSIC CHROME02 SAMPLING HELPER
//
// HLSL/MonoGame MGFX does not allow declaring a function inside MainPS.
// Keep this helper at global scope.
// ============================================================================

float4 SampleClassicChrome02(
    float2 uv,
    float strength)
{
    float2 clampedUv =
        saturate(
            uv);


    float2 overflow =
        abs(
            uv -
            clampedUv);


    float maxOverflow =
        max(
            overflow.x,
            overflow.y);


    // Soft attenuation instead of killing Chrome4 completely.
    //
    // 0 overflow -> 1.00
    // 0.5        -> ~0.75
    // 1.0        -> ~0.60
    // 2.0        -> ~0.43
    //
    // This keeps the reflection alive while avoiding the hard
    // clamp-to-edge flash we had before.
    float fade =
        1.0 /
        (
            1.0 +
            maxOverflow *
            0.65
        );


    float4 c =
        tex2D(
            Chrome02Sampler,
            clampedUv);


    c.rgb *=
        fade *
        strength;


    return c;
}


// ============================================================================
// PIXEL SHADER
// ============================================================================

float4 MainPS(
    VertexShaderOutput input)
    : COLOR
{
    // ========================================================================
    // PASS 0
    // BASE ITEM
    // ========================================================================

    if (PassMode == 0)
    {
        float4 color =
            tex2D(
                DiffuseSampler,
                input.TextureCoordinate);


        if (color.a < 0.1)
        {
            discard;
        }


        // Neffis already stores terrain/body light in the vertex color.
        //
        // Using it here is much closer to the original BMD renderer than the
        // previous ItemMaterial shader, which generated a completely separate
        // high-intensity lighting model.

        color.rgb *=
            input.VertexColor.rgb;


        // Original:
        //
        // +7 / +8  -> Light * 0.8
        // +9..+15  -> Light * 0.9

        color.rgb *=
            BaseLightScale;


        float shadowTerm =
            SampleShadow(
                input.WorldPosition,
                normalize(
                    input.Normal));


        float shadowMix =
            lerp(
                1.0 -
                ShadowStrength,

                1.0,

                shadowTerm);


        color.rgb *=
            shadowMix;


        color.a *=
            input.VertexColor.a *
            Alpha;


        return color;
    }


    // ========================================================================
    // PASS 5
    // CLASSIC EXCELLENT OVERLAY
    //
    // Original MU:
    //
    // Luminosity = sin(WorldTime * 0.002f) * 0.5f + 0.5f;
    // BodyLight  = (Luminosity, Luminosity * 0.3f, 1.0f - Luminosity);
    // RenderBody(RENDER_TEXTURE | RENDER_BRIGHT)
    //
    // C# supplies the animated BodyLight through MaterialColor and uses
    // GL_ONE + GL_ONE equivalent blending.
    // ========================================================================

    if (PassMode == 5)
    {
        float4 excellent =
            tex2D(
                DiffuseSampler,
                input.TextureCoordinate);

        if (excellent.a < 0.1)
        {
            discard;
        }

        excellent.rgb *=
            MaterialColor *
            MaterialIntensity *
            Alpha;

        excellent.a *=
            Alpha;

        return excellent;
    }
        // ========================================================================
        // PASS 6
        // DIRECT DIFFUSE ADDITIVE WITH ANIMATED UV
        //
        // Used for classic special meshes such as Flamberge flame layer.
        // ========================================================================

        if (PassMode == 6)
        {
            float2 animatedUV =
                input.TextureCoordinate
                +
                DiffuseUVOffset;

            float4 flame =
                tex2D(
                    DiffuseSampler,
                    animatedUV);

            if (flame.a < 0.1)
            {
                discard;
            }

            flame.rgb *=
                MaterialColor *
                MaterialIntensity *
                Alpha;

            flame.a *=
                Alpha;

            return flame;
        }


    // ========================================================================
    // CLASSIC ADDITIVE MATERIAL PASSES
    // ========================================================================

    float2 materialUV =
        input.ChromeUV;


    if (PassMode == 2)
    {
        materialUV =
            input.Chrome2UV;
    }
    else if (PassMode == 3)
    {
        materialUV =
            input.Chrome4UV;
    }
    else if (PassMode == 4)
    {
        materialUV =
            input.MetalUV;
    }


    float4 material;

    // ------------------------------------------------------------
    // Match original MU filtering/wrapping exactly.
    //
    // Pass 1 = Chrome01
    //          LINEAR + REPEAT
    //
    // Pass 2 = Chrome02
    // Pass 3 = Chrome4 using Chrome02 bitmap
    //          NEAREST + CLAMP
    //
    // Pass 4 = Shiny01
    //          LINEAR + CLAMP
    // ------------------------------------------------------------

    if (PassMode == 1)
    {
        material =
            tex2D(
                Chrome01Sampler,
                materialUV);
    }
    else if (PassMode == 2)
    {
        // CHROME2 normal
        material =
            SampleClassicChrome02(
                materialUV,
                1.0);
    }
    else if (PassMode == 3)
    {
        // CHROME4:
        // use the generated Chrome4 UVs and keep the soft overflow fade.
        // Strength is back at 1.0 now that the hard flashing is controlled
        // by SampleClassicChrome02().
        material =
            SampleClassicChrome02(
                materialUV,
                1.0);
    }
    else
    {
        material =
            tex2D(
                Shiny01Sampler,
                materialUV);
    }


    float3 finalColor =
        material.rgb
        *
        MaterialColor
        *
        MaterialIntensity
        *
        Alpha;


    // GL_ONE / GL_ONE blending is performed by C#.
    // Alpha does not control the hardware blend in that mode, therefore it
    // has already been multiplied into RGB above.

    return
        float4(
            finalColor,
            1.0);
}


// ============================================================================
// TECHNIQUE
// ============================================================================

technique ClassicItemMaterial
{
    pass P0
    {
        VertexShader =
            compile VS_SHADERMODEL
            MainVS();

        PixelShader =
            compile PS_SHADERMODEL
            MainPS();
    }
}