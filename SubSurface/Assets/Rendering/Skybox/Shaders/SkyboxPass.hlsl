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
    
    return half4(col, 1.0);
}

#endif