#ifndef CUSTOM_OCEAN_INPUT_INCLUDED
#define CUSTOM_OCEAN_INPUT_INCLUDED

//short cut unity access per material property
#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

#define _LODCount 8
/*
TEXTURE2D(_DispTex);
SAMPLER(sampler_DispTex);
TEXTURE2D(_NextDispTex);
SAMPLER(sampler_NextDispTex);
TEXTURE2D(_NormalTex);
SAMPLER(sampler_NormalTex);
TEXTURE2D(_NextLODNTex);
SAMPLER(sampler_NextLODNTex);
*/
TEXTURE2D(_DetailNormalNoise);
SAMPLER(sampler_DetailNormalNoise);

TEXTURE2D(_FoamCapTexture);
SAMPLER(sampler_FoamCapTexture);
TEXTURE2D(_FoamTrailTexture);
SAMPLER(sampler_FoamTrailTexture);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    //UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _GridSize)
    UNITY_DEFINE_INSTANCED_PROP(float, _LODIndex)
    //UNITY_DEFINE_INSTANCED_PROP(float, _OceanScale)
    UNITY_DEFINE_INSTANCED_PROP(float4, _TransitionParam)
    //UNITY_DEFINE_INSTANCED_PROP(float4, _CenterPos)
    UNITY_DEFINE_INSTANCED_PROP(float, _LODSize)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

//specify the buffer, use the Shader.setglobal~ to set buffers 
CBUFFER_START(_OceanGlobalData)
    float4 _OceanScaleParams;//x scale log2, y scale, z transition
    //float _OceanScaleTransi;
    
    //LOD related
    float4 _CenterPos;

    //Time
    float _Time;
    
    //Shading Related
    float4 _BaseColor;
    float4 _BrightColor;
    float4 _DarkColor;
    float4 _FoamColor;
    float4 _FresnelColor;
    float4 _SSSColor;

    //change this into bandingoffsetpow
    float4 _BandingOffsetPow;
    float4 _FoamFresnelOffsetPow;
    float4 _SSSOffsetPow;

    
    float4 _DetailNormalParams;
    float4 _HightParams;

    Texture2DArray _DispTexArray;
    Texture2DArray _NormalTexArray;
    //sampler 
    SamplerState linearClampSampler;
    SamplerState linearRepeatSampler;
CBUFFER_END

TEXTURE2D(_CameraOceanDepthTexture);
SAMPLER(sampler_CameraOceanDepthTexture);


float3 SnapToWorldPosition(float3 positionWS)
{
    float Grid = INPUT_PROP(_GridSize) * _OceanScaleParams.y;
    float Grid2 = Grid * 2.0f;

    //snap to 2*unit grid(scaled by parent!)
    positionWS.xz -= frac(unity_ObjectToWorld._m03_m23 / Grid2) * Grid2;

    return positionWS;
}

float3 TransitionLOD(float3 positionWS)
{
    float3 TransitionPosition = positionWS;
    float4 transitionParams = INPUT_PROP(_TransitionParam) * _OceanScaleParams.y;
    float3 centerPos = _CenterPos.xyz;
    float Grid4 = INPUT_PROP(_GridSize) * 4.0f * _OceanScaleParams.y;

    float DistX = abs(positionWS.x - centerPos.x) - abs(transitionParams.x);
    float DistZ = abs(positionWS.z - centerPos.z) - abs(transitionParams.y);
    float TransiFactor = clamp(max(DistX, DistZ) / transitionParams.z, 0.0f, 1.0f);
    float2 POffset = frac(positionWS.xz / Grid4) - float2(0.5f, 0.5f);
    
    if(INPUT_PROP(_LODIndex) == 0)
    {
        TransiFactor =  max(_OceanScaleParams.z, TransiFactor);
        //TransiFactor = 0.5;
    }

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
    //float3 col = _DispTex.SampleLevel(sampler_DispTex, UVn.xy, 0).rgb;
    //float3 col_n = _NextDispTex.SampleLevel(sampler_NextDispTex, UVn.zw, 0).rgb;

    float3 col = _DispTexArray.SampleLevel(linearRepeatSampler, float3(UVn.xy,INPUT_PROP(_LODIndex)), 0).rgb;
    float3 col_n =_DispTexArray.SampleLevel(linearRepeatSampler, float3(UVn.zw,min(INPUT_PROP(_LODIndex)+1,8)), 0).rgb;

    float2 LODUVblend = clamp((abs(UVn.xy - 0.5f) * 2.0f - 0.75f)*5.0f, 0, 1);
    float LODBlendFactor = max(LODUVblend.x, LODUVblend.y);
    //LODBlendFactor = 0.0f;
    col = lerp(col, col_n, LODBlendFactor);

    

    return col;
}

float4 GetOceanNormal(float4 UVn)
{
    //sample displacement tex
    //float4 col = _DispTex.SampleLevel(sampler_DispTex, UVn.xy, 0);
    //float4 col_n = _NextDispTex.SampleLevel(sampler_NextDispTex, UVn.zw, 0);
    //float4 col = _NormalTex.SampleLevel(sampler_NormalTex, UVn.xy, 0);
    //float4 col_n = _NextLODNTex.SampleLevel(sampler_NextLODNTex, UVn.zw, 0);
    float4 col = _NormalTexArray.SampleLevel(linearRepeatSampler, float3(UVn.xy,INPUT_PROP(_LODIndex)), 0);
    float4 col_n =_NormalTexArray.SampleLevel(linearRepeatSampler, float3(UVn.zw,min(INPUT_PROP(_LODIndex)+1,8)), 0);

    float2 LODUVblend = clamp((abs(UVn.xy - 0.5f) / 0.5f - 0.75f) * 5.0f, 0, 1);
    float LODBlendFactor = max(LODUVblend.x, LODUVblend.y);
    //LODBlendFactor = 1.0f;
    col = lerp(col, col_n, LODBlendFactor);

    return col;
}

float4 GetWorldPosUVAndNext(float3 positionWS)
{
    float2 UV = (positionWS.xz - _CenterPos.xz) / 
        (INPUT_PROP(_LODSize) * _OceanScaleParams.y) + 0.5f;
    float2 UV_n = (positionWS.xz - _CenterPos.xz) / 
        (INPUT_PROP(_LODSize) * _OceanScaleParams.y) * 0.5f + 0.5f;
    return float4(UV, UV_n);
}

float3 GetTangentDetailNormal(float2 staticUV)
{
    half2 wave01UV = (float2(_Time * .4, _Time * 0.4) + staticUV) * 2;
    half2 wave02UV = (float2(_Time * 0.2, -_Time * 0.2) + staticUV) * 1;
    half2 wave03UV = (float2(-_Time * 0.1, -_Time * 0.1) + staticUV) * 0.5;
    float3 normal01 = DecodeNormal(SAMPLE_TEXTURE2D(_DetailNormalNoise, sampler_DetailNormalNoise, wave01UV), 1);
    float3 normal02 = DecodeNormal(SAMPLE_TEXTURE2D(_DetailNormalNoise, sampler_DetailNormalNoise, wave02UV), 1);
    float3 normal03 = DecodeNormal(SAMPLE_TEXTURE2D(_DetailNormalNoise, sampler_DetailNormalNoise, wave03UV), 1);
    //float scale = 1;//INPUT_PROP(_NormalScale);
    float3 normal = normalize(normal01 + normal02 + normal03);

    return normal;
}
/*
half3 GetMovingTangnetNormal(half2 uv, half2 MoveDir = half2(1, 1))
{
    MoveDir = normalize(MoveDir);
    half2 wave01UV = (float2(_Time.x * _Wave01Parms.x, _Time.x * _Wave01Parms.y) * MoveDir + uv) * _Wave01Parms.z;
    half2 wave02UV = (float2(_Time.x * _Wave02Parms.x, _Time.x * _Wave02Parms.y) * MoveDir + uv) * _Wave02Parms.z;
    half2 wave03UV = (float2(_Time.x * _Wave03Parms.x, _Time.x * _Wave03Parms.y) * MoveDir + uv) * _Wave03Parms.z;
    half3 tangentNormal01 = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, wave01UV));
    half3 tangentNormal02 = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, wave02UV));
    half3 tangentNormal03 = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, wave03UV));
    half3 tangentNormal = normalize(
        tangentNormal01 * half3(_Wave01Parms.w, _Wave01Parms.w, 1.0) +
        tangentNormal02 * half3(_Wave02Parms.w, _Wave02Parms.w, 1.0) +
        tangentNormal03 * half3(_Wave03Parms.w, _Wave03Parms.w, 1.0)
        );
    return tangentNormal;
}*/

float3 DetailTangentNormalToWorld(float3 tangentDetailNormal, float3 worldBaseNormal)
{
    //try recon binormal and tangent using x->tangent
    float3 binormalWS = normalize(cross(float3(1, 0, 0), worldBaseNormal));
    float3 tangentWS = normalize(cross(worldBaseNormal, binormalWS));
    /*
    float3 FinalNormal = 
        tangentWS * tangentDetailNormal.x +
        binormalWS * tangentDetailNormal.y +
        tangentDetailNormal * tangentDetailNormal.z;
    */

    float3 FinalNormal = NormalTangentToWorld(tangentDetailNormal, worldBaseNormal, float4(tangentWS, 1.0));
    //return tangentDetailNormal;
    return normalize(FinalNormal);
}
/*
float4 GetBufferColor(float2 Position_SS, float2 uvOffset = float2(0.0, 0.0))
{
    float2 uv = Position_SS + uvOffset;
    return SAMPLE_TEXTURE2D_LOD(_CameraColorTexture, sampler_linear_clamp, uv, 0);
}

float4 GetBufferDepth(float2 Position_SS, float2 uvOffset = float2(0.0, 0.0))
{
    float2 uv = Position_SS + uvOffset;
    return SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, sampler_linear_clamp, uv, 0);
}*/

#endif