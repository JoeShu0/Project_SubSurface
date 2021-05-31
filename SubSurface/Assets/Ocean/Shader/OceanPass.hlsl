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
	float3 DebugColor : VertexDebug;
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

	//Transition at the edge of LODs
	positionWS = TransitionLOD(positionWS, INPUT_PROP(_OceanScale));

	//sample the displacement Texture and add to WPos
	float4 UVn = GetWorldPosUVAndNext(positionWS);
    //float2 UV = (positionWS.xz - _CenterPos.xz) / (_LODSize*INPUT_PROP(_OceanScale)) + 0.5f;
    //float2 UV_n = (positionWS.xz - _CenterPos.xz) / (_LODSize*INPUT_PROP(_OceanScale)) * 0.5f + 0.5f;
	
	//StaticUV for detail tex, current scale and transiton fixed!
    float2 S_UV = positionWS.xz * 0.5f ;
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
	output.DebugColor = debugDisplacecolor;
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
	float OceanDepth = LOAD_TEXTURE2D(_CameraOceanDepthTexture, input.positionCS_SS.xy).a;
	
	float4 NormalFoam = GetOceanNormal(input.UV);
	float3 normal = NormalFoam.xyz;
	float3 foam = NormalFoam.w;

	Surface surface;
	surface.position = input.positionWS;
	surface.normal = normalize(normal);
	surface.color = INPUT_PROP(_BaseColor).rgb;
	surface.alpha = 0.5;
	surface.metallic = 0;
	surface.occlusion = 1;
	surface.smoothness = 0.5;
	surface.fresnelStrength = 1;
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
	//surface.depth = -TransformWorldToView(input.positionWS).z;
	//surface.dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
	//surface.renderingLayerMask = asuint(unity_RenderingLayer.x);// treat float as uint

	//BRDF brdf = GetBRDF(surface);

	//GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);

	//float3 color = GetLighting(surface, brdf, gi);

	//?????Dont konw why the OrthographicDepthBufferToLinear case the tile to offsets?????
	//bufferDepth = IsOrthographicCamera() ?
		//OrthographicDepthBufferToLinear(bufferDepth) :
		//LinearEyeDepth(bufferDepth, _ZBufferParams);
	//return float4(INPUT_PROP(_BaseColor).rgb, 0.5);
	
	//Basic lighting
	float color = foam;

	return float4(color, color, color, 1.0);
}

#endif