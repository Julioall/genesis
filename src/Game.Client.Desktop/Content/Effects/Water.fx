#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 WorldViewProjection;
float3 CameraPosition;
float TimeSeconds;
float WaveAmplitude;
float FogEnabled;
float FogStart;
float FogEnd;
float4 FogColor;

struct VertexInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 FlowDirection : TEXCOORD0;
};

struct VertexOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 WorldPosition : TEXCOORD0;
    float2 FlowDirection : TEXCOORD1;
};

VertexOutput MainVS(VertexInput input)
{
    VertexOutput output;
    float3 worldPosition = input.Position.xyz;

    float phaseA = dot(worldPosition.xz, float2(0.72, 0.35)) * (6.2831853 / 360.0)
        + TimeSeconds * 0.72;
    float phaseB = dot(worldPosition.xz, float2(-0.31, 0.95)) * (6.2831853 / 230.0)
        - TimeSeconds * 1.03;
    float phaseDetail = dot(worldPosition.xz, float2(0.91, 0.42)) * (6.2831853 / 94.0)
        + TimeSeconds * 1.38;
    worldPosition.y += WaveAmplitude * (sin(phaseA) * 0.31 + sin(phaseB) * 0.15
        + sin(phaseDetail) * 0.06);

    output.Position = mul(float4(worldPosition, 1.0), WorldViewProjection);
    output.WorldPosition = input.Position.xyz;
    output.FlowDirection = input.FlowDirection;
    output.Color = input.Color;
    return output;
}

float4 MainPS(VertexOutput input) : COLOR0
{
    float2 flowDirection = input.FlowDirection;
    float flowLength = length(flowDirection);
    float glint;
    float4 waterColor = input.Color;

    if (flowLength > 0.5)
    {
        flowDirection /= flowLength;
        float phase = dot(input.WorldPosition.xz, flowDirection) * 0.11 - TimeSeconds * 1.65;
        glint = pow(saturate(sin(phase)), 10.0) * 0.16;
    }
    else
    {
        float phase = input.WorldPosition.x * 0.037 + input.WorldPosition.z * 0.051
            + TimeSeconds * 0.72;
        glint = pow(saturate(sin(phase)), 7.0) * 0.035;

        float broadCrest = sin(input.WorldPosition.x * 0.72 * (6.2831853 / 360.0)
            + input.WorldPosition.z * 0.35 * (6.2831853 / 360.0) + TimeSeconds * 0.72) * 0.52
            + sin(input.WorldPosition.x * -0.31 * (6.2831853 / 230.0)
            + input.WorldPosition.z * 0.95 * (6.2831853 / 230.0) - TimeSeconds * 1.03) * 0.34
            + sin(input.WorldPosition.x * 0.91 * (6.2831853 / 94.0)
            + input.WorldPosition.z * 0.42 * (6.2831853 / 94.0) + TimeSeconds * 1.38) * 0.14;
        glint += max(0.0, broadCrest) * 0.06 * WaveAmplitude;
    }

    waterColor.rgb = lerp(waterColor.rgb, float3(157.0 / 255.0, 196.0 / 255.0, 190.0 / 255.0), glint);
    if (FogEnabled > 0.5)
    {
        float distanceToCamera = distance(input.WorldPosition, CameraPosition);
        float fogAmount = saturate((distanceToCamera - FogStart) / max(1.0, FogEnd - FogStart));
        waterColor.rgb = lerp(waterColor.rgb, FogColor.rgb, fogAmount);
    }
    return waterColor;
}

technique WaterSurface
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
