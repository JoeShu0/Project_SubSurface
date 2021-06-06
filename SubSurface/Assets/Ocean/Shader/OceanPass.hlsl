#ifndef CUSTOM_OCEAN_PASS_INCLUDED
#define CUSTOM_OCEAN_PASS_INCLUDED


#include "../../CustomRP/ShaderLib/Surface.hlsl"
#include "../../CustomRP/ShaderLib/Shadows.hlsl"
#include "../../CustomRP/ShaderLib/Light.hlsl"
#include "../../CustomRP/ShaderLib/BRDF.hlsl"
#include "../../CustomRP/ShaderLib/GI.hlsl"
#include "../../CustomRP/ShaderLib/Lighting.hlsl"



struct Attributes
{
	float3 positionOS : POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID//this will store the instance ID
};

struct Varyings
{
	float4 positionCS_SS : SV_POSITION;
	float3 positionWS : VAR_POSITION;
	float depth01 : VAR_DEPTH01;
	float4 UV : VAR_OCEANUV;
	float2 StaticUV : VAR_DETAILUV;
	float4 DebugColor : VertexDebug;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings OceanPassVertex(Attributes input)
{
	Varyings output;
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//transfer instance ID to frag
	UNITY_TRANSFER_INSTANCE_ID(input, output);

	//Get World and ScreenSpace position
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	
	//Snap tp 2*unit grid(Should be scaled by whole ocean)
	positionWS = SnapToWorldPosition(positionWS, INPUT_PROP(_OceanScale));

	//StaticUV for detail tex, current scale and transiton fixed!
	float2 S_UV = positionWS.xz * 0.5f;

	//Transition at the edge of LODs
	positionWS = TransitionLOD(positionWS, INPUT_PROP(_OceanScale));

	//sample the displacement Texture and add to WPos
	float4 UVn = GetWorldPosUVAndNext(positionWS);
    //float2 UV = (positionWS.xz - _CenterPos.xz) / (_LODSize*INPUT_PROP(_OceanScale)) + 0.5f;
    //float2 UV_n = (positionWS.xz - _CenterPos.xz) / (_LODSize*INPUT_PROP(_OceanScale)) * 0.5f + 0.5f;
	
	
	float3 debugDisplacecolor = GetOceanDisplacement(UVn);
	positionWS += debugDisplacecolor;


	float3 localPos = mul(unity_WorldToObject, float4(positionWS, 1.0)).xyz;
	//float4 SrcPos = TransformObjectToHClip(localPos);
	float4 SrcPos = TransformWorldToHClip(positionWS);
	//#define COMPUTE_DEPTH_01 -(mul( UNITY_MATRIX_MV, v.vertex ).z * _ProjectionParams.w)
	float depth01 = 1+TransformWorldToView(positionWS).z * _ProjectionParams.w;

	//output.positionCS_SS = TransformWorldToHClip(output.positionWS);
	output.positionWS = positionWS;
	output.positionCS_SS = SrcPos;
	output.depth01 = depth01;
	output.UV = UVn;
	output.StaticUV = S_UV;
	output.DebugColor = float4(debugDisplacecolor,0.0);
	return output;
}

float4 OceanDepthPassFragment(Varyings input) : SV_TARGET
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//float depth10 = 1 - ;
		//IsOrthographicCamera() ?
		//OrthographicDepthBufferToLinear(input.positionCS_SS.z): 
		//input.positionCS_SS.w;
	return float4(0.0,0.0,0.0, input.depth01);
	//return depth;
}

float4 OceanPassFragment(Varyings input) : SV_TARGET 
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//Depth10 for ocean depth(not used in here)
	float OceanDepth10 = LOAD_TEXTURE2D(_CameraOceanDepthTexture, input.positionCS_SS.xy).a;
	
	float4 NormalFoam = GetOceanNormal(input.UV);
	float3 normalWS = normalize(NormalFoam.xyz);
	float foam = NormalFoam.w;

	
	//Get the detail normal and combine with base normal
	float3 DetailTangentNormal = GetTangentDetailNormal(input.StaticUV);
	//normalWS = DetailTangentNormalToWorld(DetailTangentNormal, normalWS);


	//******Surface setup******
	Surface surface;
	surface.position = input.positionWS;
	surface.normal = normalWS;

	surface.color = _BaseColor.rgb;
	surface.alpha = 1.0f;
	surface.metallic = 0;
	surface.occlusion = 1;
	surface.smoothness = 0.5;
	surface.fresnelStrength = 1;
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
	surface.depth = -TransformWorldToView(input.positionWS).z;
	//surface.dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
	surface.renderingLayerMask = asuint(unity_RenderingLayer.x);// treat float as uint

	//BRDF brdf = GetBRDF(surface);

	//GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);

	//float3 color = GetLighting(surface, brdf, gi);

	//?????Dont konw why the OrthographicDepthBufferToLinear case the tile to offsets?????
	//bufferDepth = IsOrthographicCamera() ?
		//OrthographicDepthBufferToLinear(bufferDepth) :
		//LinearEyeDepth(bufferDepth, _ZBufferParams);
	//return float4(INPUT_PROP(_BaseColor).rgb, 0.5);
	
	//Basic lighting
	float4 color = float4(input.DebugColor.rgb,1.0f);

	

	//**********experimental part**********
	float3 sunDirection = float3(-0.5,-0.5,0.0);

	float3 reflectDir = normalize(reflect(surface.viewDirection, surface.normal));
	float SunReflect = pow(saturate(dot(normalize(sunDirection), reflectDir)), _HightParams.x);
	
	float LightNormalGradient = dot(-sunDirection, surface.normal);
	float ViewNormalGradient = dot(surface.viewDirection, surface.normal);
	

	color = _DarkColor;
	float baseMask = clamp(
		pow(abs(LightNormalGradient + _BrightOffsetPow.x), _BrightOffsetPow.y),
		0.0f,
		1.0f);
	color = lerp(color, _BaseColor, baseMask);
	float brightMask = clamp(
		pow(abs(LightNormalGradient + _BrightOffsetPow.z), _BrightOffsetPow.w),
		0.0f,
		1.0f);
	color = lerp(color, _BrightColor, brightMask);

	float foamMask = clamp(
		pow(abs(foam + _FoamFresnelOffsetPow.x), _FoamFresnelOffsetPow.y),
		0.0f,
		1.0f);
	color = lerp(color, _FoamColor, foamMask);
	float fresnelMask = clamp(
		pow(abs(1-ViewNormalGradient + _FoamFresnelOffsetPow.z), _FoamFresnelOffsetPow.w),
		0.0f,
		1.0f);
	color = lerp(color, _FresnelColor, fresnelMask);

	
	
	return float4(color.rgb, max(color.a, 1-ViewNormalGradient));//
	//SunReflect = dot(normalize(float3(-0.5, -0.5, 0.0)), reflectDir);

	return float4(SunReflect, foam, 0.0,1.0f) + float4(0.1f,0.1f,0.1f,0.0f);
}

#endif