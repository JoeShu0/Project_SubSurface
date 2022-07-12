#ifndef CUSTOM_SKYBOX_PASS_INCLUDED
#define CUSTOM_SKYBOX_PASS_INCLUDED



struct Attributes
{
    float3 positionOS : POSITION;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 positionLS : VAR_LOCALPOS;
    //float4 DebugColor : VertexDebug;
	//half facing : VFACE;
};

Varyings SkyboxVertex(Attributes input)
{
    Varyings output;
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);
    output.positionLS = float4(input.positionOS, 1.0);
    return output;
}

float4 SkyboxFragment(Varyings input) : SV_TARGET
{
    half3 col = normalize(input.positionLS.xyz);
    
    half Upper = step(0, input.positionLS.y);
    col = Upper * col + (1 - Upper) * _DarkColor;
    /*
    float4 OceanDepthTexValue = LOAD_TEXTURE2D(_CameraOceanDepthTexture, input.positionCS.xy);
    float OceanSurfaceDepth10 = OceanDepthTexValue.a;
    float OceanSurfaceFacing = OceanDepthTexValue.y;
    
    float pixelDepth01 = Linear01Depth(input.positionCS.z, _ZBufferParams);
    float pixelDepth10 = 1 - pixelDepth01;
    
    float OceanDepthDelta = 0;
    if (OceanSurfaceDepth10 == 0.0)//BG is inifinte far or sky
    {
        OceanDepthDelta = (-viewDirection.y >= 0) ? 0.0 : pixelDepth01;
    }
    else
    {
        OceanDepthDelta = (OceanSurfaceFacing > 0.0) ? OceanSurfaceDepth10 - pixelDepth10 : pixelDepth01;
    }
    
    float4 RampValue = GetDepthRampColor(OceanDepthDelta * 2000);
    //return float4(RampValue.rgb * col.rgb, 1.0);
    */
    
    float3 RampValue = GetDepthRampColor(1.0).rgb;
    return half4(col * RampValue, 1.0);
}

#endif