#ifndef CUSTOM_SKYBOX_INPUT_INCLUDED
#define CUSTOM_SKYBOX_INPUT_INCLUDED

CBUFFER_START(_OceanGlobalData)
float4 _DarkColor;
CBUFFER_END

TEXTURE2D(_CameraOceanDepthTexture);
SAMPLER(sampler_CameraOceanDepthTexture);

TEXTURE2D(_OceanDepthRamp);
SAMPLER(sampler_OceanDepthRamp);

float4 GetDepthRampColor(float DepthGap)
{
    float2 DepthRampUV = float2(DepthGap, 0.5);
    float4 map = SAMPLE_TEXTURE2D(_OceanDepthRamp, sampler_OceanDepthRamp, DepthRampUV);
    return map;
}

#endif