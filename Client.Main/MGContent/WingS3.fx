#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_5_0
    #define PS_SHADERMODEL ps_5_0
#endif


float4x4 WorldViewProjection;

texture DiffuseTexture;

float2 TexCoordOffset =
    float2(
        0.0,
        0.0);

float3 Tint =
    float3(
        1.0,
        1.0,
        1.0);

float Alpha =
    1.0;


sampler2D DiffuseSampler =
    sampler_state
{
    Texture =
        <DiffuseTexture>;

    MinFilter =
        Linear;

    MagFilter =
        Linear;

    MipFilter =
        Linear;

    AddressU =
        Wrap;

    AddressV =
        Wrap;
};


struct VertexInput
{
    float3 Position
        : POSITION0;

    float4 Color
        : COLOR0;

    float3 Normal
        : NORMAL0;

    float2 TexCoord
        : TEXCOORD0;
};


struct PixelInput
{
    float4 Position
        : SV_POSITION;

    float2 TexCoord
        : TEXCOORD0;
};


PixelInput VS_Main(
    VertexInput input)
{
    PixelInput output;


    output.Position =
        mul(
            float4(
                input.Position,
                1.0),
            WorldViewProjection);


    output.TexCoord =
        input.TexCoord +
        TexCoordOffset;


    return output;
}


float4 PS_Main(
    PixelInput input)
    : COLOR
{
    float4 tex =
        tex2D(
            DiffuseSampler,
            input.TexCoord);


    // Preserve true transparent/cutout pixels,
    // but avoid carrying soft unwanted transparency
    // into the solid Storm base mesh.
    if (tex.a < 0.05)
    {
        discard;
    }


    float3 finalColor =
        tex.rgb *
        Tint *
        Alpha;


    return float4(
        finalColor,
        1.0);
}


technique WingS3
{
    pass P0
    {
        VertexShader =
            compile VS_SHADERMODEL
            VS_Main();

        PixelShader =
            compile PS_SHADERMODEL
            PS_Main();
    }
}