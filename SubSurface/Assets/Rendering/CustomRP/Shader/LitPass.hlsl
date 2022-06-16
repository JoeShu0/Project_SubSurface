#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "../ShaderLib/Surface.hlsl"
#include "../ShaderLib/Shadows.hlsl"
#include "../ShaderLib/Light.hlsl"
#include "../ShaderLib/BRDF.hlsl"
#include "../ShaderLib/GI.hlsl"
#include "../ShaderLib/Lighting.hlsl"

//#include "../../Ocean/ShaderLib/Expofog.hlsl"

struct Attributes
{
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	GI_ATTRIBUTE_DATA//this is to have the lightmap UV
	UNITY_VERTEX_INPUT_INSTANCE_ID//this will store the instance ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
	float2 detailUV : VAR_DETAIL_UV;
	float3 normalWS : VAR_NORMAL;
	#if defined(_NORMAL_MAP)
		float4 tangentWS : VAR_TANGENT;
	#endif
	float3 positionWS : VAR_POSITION;
	float depth01 : VAR_DEPTH01;
	GI_VARYINGS_DATA//this is to have the lightmap UV
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex(Attributes input)
{
	Varyings output;
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//transfer instance ID to frag
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	//transfer lightmap UV
	TRANSFER_GI_DATA(input, output);

	//transform UV based on permaterial ST
	output.baseUV = TransformBaseUV(input.baseUV);
	#if defined(_DETAIL_MAP)
		output.detailUV = TransformDetailUV(input.baseUV);
	#else
		output.detailUV = 0.0;
	#endif

	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(output.positionWS);

	//transfer normal
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);

	#if defined(_NORMAL_MAP)
		//transfer tangent
		output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
	#endif

	//#define COMPUTE_DEPTH_01 -(mul( UNITY_MATRIX_MV, v.vertex ).z * _ProjectionParams.w)
	output.depth01 = 1 + TransformWorldToView(output.positionWS).z * _ProjectionParams.w;

	return output;
}

float4 LitPassFragment(Varyings input) : SV_TARGET 
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//ocean depth comparasion
	//float OceanDepth10 = LOAD_TEXTURE2D(_CameraOceanDepthTexture, input.positionCS.xy).a;
	//float OceanDepthDelta = input.depth01 - OceanDepth10;

	//use the new packed config instead of UV
	InputConfig config = GetInputConfig(input.positionCS, input.baseUV, input.detailUV);
	#if defined(_MASK_MAP)
		config.useMask = true;
	#endif
	#if defined(_DETAIL_MAP)
		config.detailUV = input.detailUV;
		config.useDetail = true;
	#endif

	//LOD fade, the fade factor is in x com of unity_LODFade
	ClipLOD(config.fragment, unity_LODFade.x);

	//get the basemap * basecolor
	float4 base = GetBase(config);
	
#if defined(_CLIPPING)
	clip(base.a - GetCutoff(config));
#endif

	Surface surface;
	surface.position = input.positionWS;
	#if defined(_NORMAL_MAP)
		surface.interpolatedNormalWS = input.normalWS;
		surface.normal = NormalTangentToWorld(GetNormalTS(config), input.normalWS, input.tangentWS);
	#else
		surface.normal = normalize(input.normalWS);
		surface.interpolatedNormalWS = surface.normal;
	#endif
	surface.color = base.rgb;
	surface.alpha = base.a;
	surface.metallic = GetMetallic(config);
	surface.occlusion = GetOcclusion(config);
	surface.smoothness = GetSmoothness(config);
	surface.fresnelStrength = GetFresnel(config);
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
	surface.depth = -TransformWorldToView(input.positionWS).z;
	surface.dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
	surface.renderingLayerMask = asuint(unity_RenderingLayer.x);// treat float as uint

#if defined(_PREMULTIPLY_ALPHA)
	BRDF brdf = GetBRDF(surface, true);
#else
	BRDF brdf = GetBRDF(surface);
#endif

	//get the lightmap UV = GI_FRAGMENT_DATA(input)
	GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
	//shadow mask debug
	//return  gi.shadowMask.shadows;
	
	float3 color = GetLighting(surface, brdf, gi);

	//emission
	color += GetEmission(config);

	//return float4(base.rgb ,1.0f);
/*
#if defined(_CLIPPING)
	//Not Clipped Alpha to 1.0
	return float4(color, 1.0f);
#endif
*/	 
	
	
	//float depth10 = 1 - (input.positionCS.w - _ProjectionParams.y) / (_ProjectionParams.z - _ProjectionParams.y);
	//return float4(-OceanDepthDelta*100000, 0.0, 0.0, 1.0);

	//if (OceanDepthDelta < 0)
	//{
		//return float4(1.0, 0.0, 0.0, GetFinalAlpha(surface.alpha));
	//}
	
    float3 viewDirection = surface.viewDirection;

    float4 OceanDepthTexValue = LOAD_TEXTURE2D(_CameraOceanDepthTexture, input.positionCS.xy);
    float OceanSurfaceDepth10 = OceanDepthTexValue.a;
    float OceanSurfaceFacing = OceanDepthTexValue.y;
	
	
    float pixelDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
    float pixelDepth01 = Linear01Depth(input.positionCS.z, _ZBufferParams);
    float pixelDepth10 = 1 - Linear01Depth(input.positionCS.z, _ZBufferParams);
	
    //float depthGap = saturate(-(OceanDepth - pixelDepth) * 0.02); //10米最深
    //float4 RampValue = GetDepthRampColor(depthGap);
    //return RampValue;
	
	/*
	float4 OceanDelta01 = 
		GetWaterTempBlendValue(
			input.positionCS.xy, 
			input.depth01, 
			surface.viewDirection);*/
	
    
	
    float OceanDepthDelta = 0;
    if (OceanSurfaceDepth10 == 0.0)//BG is inifinte far or sky
    {
        OceanDepthDelta = (-viewDirection.y >= 0) ? 0.0 : pixelDepth01;
		//OceanDepthDelta = 1.0;
    }
    else
    {
		//float A = viewDirection.y >= 0 ?
		
		OceanDepthDelta = (OceanSurfaceFacing > 0.0) ? OceanSurfaceDepth10 - pixelDepth10 : pixelDepth01;
        //OceanDepthDelta = saturate(-OceanDepthDelta); //10米最深

    }
	
    //float FarNearDistance = -(1 / (_ZBufferParams.z + _ZBufferParams.w) - 1 / _ZBufferParams.w);
	
    float4 RampValue = GetDepthRampColor(OceanDepthDelta);
    return OceanDepthDelta * 10000;
	
	//float4 OceanDelta01 = clamp(-OceanDepthDelta*2000, 0.0, 1.0);
	
	//return lerp(float4(color, GetFinalAlpha(surface.alpha)),float4(_DarkColor.rgb, GetFinalAlpha(surface.alpha)), OceanDelta01);
}

#endif