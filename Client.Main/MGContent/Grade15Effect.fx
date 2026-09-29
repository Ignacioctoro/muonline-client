// ============================================================================
// Grade15Effect.fx
// Classic MU Online +15 armor linked-object renderer.
//
// Reproduces the three RenderMesh() passes used by the original client:
//
//  1) RENDER_TEXTURE | RENDER_BRIGHT
//     class15_effect_main
//
//  2) RENDER_TEXTURE | RENDER_BRIGHT
//     BITMAP_RGB_MIX + animated U coordinate
//
//  3) RENDER_TEXTURE | RENDER_CHROME4
//     BITMAP_CHROME2 + generated chrome coordinates
//
// Time is expressed in SECONDS on the MonoGame side.
// ============================================================================

#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_5_0
    #define PS_SHADERMODEL ps_5_0
#endif


// ============================================================================
// MATRICES
// ============================================================================

float4x4 World;
float4x4 WorldViewProjection;


// ============================================================================
// CLASSIC +15 PARAMETERS
// ============================================================================

float Time = 0.0;

float Brightness = 1.0;

float TexCoordUOffset = 0.0;

float Alpha = 1.0;


// ============================================================================
// TEXTURES
// ============================================================================

// class15_effect_main
texture DiffuseTexture;

sampler2D DiffuseSampler = sampler_state
{
    Texture = <DiffuseTexture>;

    AddressU = Wrap;
    AddressV = Wrap;

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
};


// BITMAP_RGB_MIX
// Data/Item/rgb_mix.OZJ
texture RgbMixTexture;

sampler2D RgbMixSampler = sampler_state
{
    Texture = <RgbMixTexture>;

    AddressU = Wrap;
    AddressV = Wrap;

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
};


// BITMAP_CHROME2
// Data/Effect/Chrome02.OZJ
texture ChromeTexture;

sampler2D ChromeSampler = sampler_state
{
    Texture = <ChromeTexture>;

    AddressU = Wrap;
    AddressV = Wrap;

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
};


// ============================================================================
// VERTEX INPUT / OUTPUT
// ============================================================================

struct VertexInput
{
    float3 Position : POSITION0;
    float4 Color    : COLOR0;
    float3 Normal   : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};


struct PixelInput
{
    float4 Position : SV_POSITION;

    float2 TexCoord : TEXCOORD0;

    float3 Normal   : TEXCOORD1;

    float4 Color    : COLOR0;
};


// ============================================================================
// VERTEX SHADER
// ============================================================================

PixelInput VS_Main(VertexInput input)
{
    PixelInput output;

    output.Position =
        mul(
            float4(input.Position, 1.0),
            WorldViewProjection);

    // BMDLoader already transforms the normal by the BMD bone.
    // Here we only apply the linked object's world rotation.
    float3 worldNormal =
        mul(
            input.Normal,
            (float3x3)World);

    float normalLengthSq =
        dot(worldNormal, worldNormal);

    if (normalLengthSq > 0.000001)
    {
        worldNormal *=
            rsqrt(normalLengthSq);
    }

    output.Normal =
        worldNormal;

    output.TexCoord =
        input.TexCoord;

    output.Color =
        input.Color;

    return output;
}


// ============================================================================
// PASS 1
//
// Original:
//
// fLight =
//     0.8f -
//     abs(sin(WorldTime * 0.0018f) * 0.5f);
//
// RenderMesh(
//     0,
//     RENDER_TEXTURE | RENDER_BRIGHT,
//     ...,
//     fLight - 0.1f,
//     ...);
//
// Brightness is calculated in C# so this shader can be reused directly.
// ============================================================================

float4 PS_Base(PixelInput input) : SV_Target
{
    float4 textureColor =
        tex2D(
            DiffuseSampler,
            input.TexCoord);

    float3 finalColor =
        textureColor.rgb *
        input.Color.rgb *
        Brightness *
        Alpha;

    return float4(
        finalColor,
        textureColor.a * Alpha);
}


// ============================================================================
// PASS 2 - RGB MIX
//
// Original:
//
// texCoordU =
//     abs(
//         sin(
//             WorldTime *
//             0.0005f));
//
// RenderMesh(
//     ...,
//     BITMAP_RGB_MIX);
//
// RenderMesh adds texCoordU to the original mesh UV.
// ============================================================================

float4 PS_RgbMix(PixelInput input) : SV_Target
{
    float2 uv =
        input.TexCoord;

    uv.x +=
        TexCoordUOffset;

    float4 textureColor =
        tex2D(
            RgbMixSampler,
            uv);

    float3 finalColor =
        textureColor.rgb *
        input.Color.rgb *
        Brightness *
        Alpha;

    return float4(
        finalColor,
        textureColor.a * Alpha);
}


// ============================================================================
// PASS 3 - CHROME4
//
// Original MU:
//
// wave =
//     (WorldTime % 10000) *
//     0.0001f;
//
// light =
// {
//     cos(WorldTime * 0.001f),
//     sin(WorldTime * 0.002f),
//     1.0f
// };
//
// chromeU = dot(normal, light);
//
// chromeV = 1.0f - chromeU;
//
// chromeV -=
//     normal.z * 0.5f +
//     wave * 3.0f;
//
// chromeU +=
//     normal.y * 0.5f +
//     light.y * 3.0f;
//
// RENDER_CHROME4 uses BITMAP_CHROME2.
// ============================================================================

float4 PS_Chrome4(PixelInput input) : SV_Target
{
    float3 normal =
        input.Normal;

    float normalLengthSq =
        dot(normal, normal);

    if (normalLengthSq > 0.000001)
    {
        normal *=
            rsqrt(normalLengthSq);
    }

    // WorldTime was milliseconds in the classic client.
    //
    // (WorldTime % 10000) * 0.0001
    //
    // With Time expressed in seconds this becomes:
    //
    // frac(Time * 0.1)
    //
    float wave =
        frac(Time * 0.1);

    float3 chromeLight =
        float3(
            cos(Time),
            sin(Time * 2.0),
            1.0);

    float chromeU =
        dot(
            normal,
            chromeLight);

    float chromeV =
        1.0 -
        chromeU;

    chromeV -=
        normal.z * 0.5 +
        wave * 3.0;

    chromeU +=
        normal.y * 0.5 +
        chromeLight.y * 3.0;

    float2 chromeUv =
        float2(
            chromeU,
            chromeV);

    float4 textureColor =
        tex2D(
            ChromeSampler,
            chromeUv);

    float3 finalColor =
        textureColor.rgb *
        input.Color.rgb *
        Alpha;

    return float4(
        finalColor,
        textureColor.a * Alpha);
}


// ============================================================================
// TECHNIQUES
// ============================================================================

technique Grade15Base
{
    pass Pass1
    {
        VertexShader =
            compile VS_SHADERMODEL
            VS_Main();

        PixelShader =
            compile PS_SHADERMODEL
            PS_Base();
    }
}


technique Grade15RgbMix
{
    pass Pass1
    {
        VertexShader =
            compile VS_SHADERMODEL
            VS_Main();

        PixelShader =
            compile PS_SHADERMODEL
            PS_RgbMix();
    }
}


technique Grade15Chrome4
{
    pass Pass1
    {
        VertexShader =
            compile VS_SHADERMODEL
            VS_Main();

        PixelShader =
            compile PS_SHADERMODEL
            PS_Chrome4();
    }
}