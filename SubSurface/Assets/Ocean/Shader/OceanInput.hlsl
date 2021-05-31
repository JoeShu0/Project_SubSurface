#ifndef CUSTOM_OCEAN_INPUT_PASS_INCLUDED
#define CUSTOM_OCEAN_INPUT_PASS_INCLUDED

//short cut unity access per material property
#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

TEXTURE2D(_DispTex);
SAMPLER(sampler_DispTex);
TEXTURE2D(_NextDispTex);

TEXTURE2D(_NormalTex);
SAMPLER(sampler_NormalTex);
TEXTURE2D(_NextLODNTex);


UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _GridSize)
    UNITY_DEFINE_INSTANCED_PROP(float, _LODIndex)
    UNITY_DEFINE_INSTANCED_PROP(float, _OceanScale)
    UNITY_DEFINE_INSTANCED_PROP(float4, _TransitionParam)
    //UNITY_DEFINE_INSTANCED_PROP(float4, _CenterPos)
    UNITY_DEFINE_INSTANCED_PROP(float, _LODSize)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
//specify the buffer, use the Shader.setglobal~ to set buffers 
CBUFFER_START(_OceanData)
    float4 _CenterPos;
    //float4 _CamProjectionParams;
CBUFFER_END

TEXTURE2D(_CameraOceanDepthTexture);
SAMPLER(sampler_CameraOceanDepthTexture);


float3 SnapToWorldPosition(float3 positionWS, float oceanScale)
{
    float Grid = INPUT_PROP(_GridSize) * oceanScale;
    float Grid2 = Grid * 2.0f;

    //snap to 2*unit grid(scaled by parent!)
    positionWS.xz -= frac(unity_ObjectToWorld._m03_m23 / Grid2) * Grid2;

    return positionWS;
}

float3 TransitionLOD(float3 positionWS, float oceanScale)
{
    float3 TransitionPosition = positionWS;
    float4 transitionParams = INPUT_PROP(_TransitionParam) * oceanScale;
    float3 centerPos = _CenterPos.xyz;
    float Grid4 = INPUT_PROP(_GridSize) * 4.0f * oceanScale;

    float DistX = abs(positionWS.x - centerPos.x) - abs(transitionParams.x);
    float DistZ = abs(positionWS.z - centerPos.z) - abs(transitionParams.y);
    float TransiFactor = clamp(max(DistX, DistZ) / transitionParams.z, 0.0f, 1.0f);
    float2 POffset = frac(positionWS.xz / Grid4) - float2(0.5f, 0.5f);
    
    //TransiFactor = 1;
    const float MinTransitionRadius =0.26f;
    if (abs(POffset.x) < MinTransitionRadius)
    {
        TransitionPosition.x += POffset.x * Grid4 * TransiFactor;
    }
        
    if (abs(POffset.y) < MinTransitionRadius)
    {
        TransitionPosition.z += POffset.y * Grid4 * TransiFactor;
    }
        
    //TransitionPosition.x += POffset.x * Grid4 * TransiFactor;
    //TransitionPosition.z += POffset.y * Grid4 * TransiFactor;
    return TransitionPosition;
}

float3 GetOceanDisplacement(float4 UVn)
{
    //sample displacement tex
    float3 col = _DispTex.SampleLevel(sampler_DispTex, UVn.xy, 0).rgb;
    float3 col_n = _NextDispTex.SampleLevel(sampler_DispTex, UVn.zw, 0).rgb;

    float2 LODUVblend = clamp((abs(UVn.xy - 0.5f) / 0.5f - 0.75f)*5.0f, 0, 1);
    float LODBlendFactor = max(LODUVblend.x, LODUVblend.y);
    //LODBlendFactor = 0.0f;
    col = lerp(col, col_n, LODBlendFactor);
    return col;
}

float4 GetOceanNormal(float4 UVn)
{
    //sample displacement tex
    float4 col = _NormalTex.SampleLevel(sampler_NormalTex, UVn.xy, 0);
    float4 col_n = _NextLODNTex.SampleLevel(sampler_NormalTex, UVn.zw, 0);

    float2 LODUVblend = clamp((abs(UVn.xy - 0.5f) / 0.5f - 0.75f) * 5.0f, 0, 1);
    float LODBlendFactor = max(LODUVblend.x, LODUVblend.y);
    //LODBlendFactor = 0.0f;
    col = lerp(col, col_n, LODBlendFactor);
    return col;
}

float4 GetWorldPosUVAndNext(float3 positionWS)
{
    float2 UV = (positionWS.xz - _CenterPos.xz) / 
        (INPUT_PROP(_LODSize) * INPUT_PROP(_OceanScale)) + 0.5f;
    float2 UV_n = (positionWS.xz - _CenterPos.xz) / 
        (INPUT_PROP(_LODSize) * INPUT_PROP(_OceanScale)) * 0.5f + 0.5f;
    return float4(UV, UV_n);
}

#endif