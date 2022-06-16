#ifndef CUSTOM_OCEAN_PASS_INCLUDED
#define CUSTOM_OCEAN_PASS_INCLUDED

// All these shading will be revamped to match URP 12
#include "../ShaderLib/OceanSurface.hlsl"
//Shared part
#include "../../CustomRP/ShaderLib/Shadows.hlsl"
#include "../../CustomRP/ShaderLib/Light.hlsl"
//Unique to Ocean
#include "../ShaderLib/OceanTRDF.hlsl"
//Shared part
#include "../ShaderLib/OceanGI.hlsl"
//Unique to Ocean
#include "../ShaderLib/OceanLighting.hlsl"

struct Attributes
{
	float3 positionOS : POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID//this will store the instance ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float3 positionWS : VAR_POSITION;
	float depth01 : VAR_DEPTH01;
	float4 UV : VAR_OCEANUV;
	float2 StaticUV : VAR_DETAILUV;
	float4 DebugColor : VertexDebug;
	//half facing : VFACE;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings OceanPassVertexFunction(Attributes input)
{
	Varyings output;
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//transfer instance ID to frag
	UNITY_TRANSFER_INSTANCE_ID(input, output);

	//Get World and ScreenSpace position
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	
	//Snap tp 2*unit grid(Should be scaled by whole ocean)
	positionWS = SnapToWorldPosition(positionWS);

	//Transition at the edge of LODs
	positionWS = TransitionLOD(positionWS);

	//sample the displacement Texture and add to WPos
	float4 UVn = GetWorldPosUVAndNext(positionWS);
    //float2 UV = (positionWS.xz - _CenterPos.xz) / (_LODSize*INPUT_PROP(_OceanScale)) + 0.5f;
    //float2 UV_n = (positionWS.xz - _CenterPos.xz) / (_LODSize*INPUT_PROP(_OceanScale)) * 0.5f + 0.5f;
	
	//StaticUV for detail tex, current scale and transiton fixed!
	float2 S_UV = positionWS.xz * 0.5f;
	
	float3 Displace = GetOceanDisplacement(UVn);
	positionWS += Displace;


	float3 localPos = mul(unity_WorldToObject, float4(positionWS, 1.0)).xyz;
	//float4 positionCS = TransformObjectToHClip(localPos);
    float4 positionCS = TransformWorldToHClip(positionWS);
	//#define COMPUTE_DEPTH_01 -(mul( UNITY_MATRIX_MV, v.vertex ).z * _ProjectionParams.w)
	//float depth01 = 1+TransformWorldToView(positionWS).z * _ProjectionParams.w;
	
	//计算线性深度
    float Depth = LinearEyeDepth(positionCS.z, _ZBufferParams);
    float Depth01 = Linear01Depth(positionCS.z, _ZBufferParams);
	
	//output.positionCS = TransformWorldToHClip(output.positionWS);
	output.positionWS = positionWS;
    output.positionCS = positionCS;
    output.depth01 = Depth01;
	output.UV = UVn;
	output.StaticUV = S_UV;
	output.DebugColor = float4(Displace, 1.0f);
	return output;
}

Varyings OceanPassVertex(Attributes input)
{
	return OceanPassVertexFunction(input);
}

Varyings OceanDepthPassVertexFront(Attributes input)
{
	Varyings ShiftedOut;
	ShiftedOut = OceanPassVertexFunction(input);
	return ShiftedOut;
}

Varyings OceanDepthPassVertexBack(Attributes input)
{
	Varyings ShiftedOut;
	ShiftedOut = OceanPassVertexFunction(input);
	//push the back side of the water face by 1 unit in the depth buffer, this reduce the 
	//flicking pixel due depth test fail cased by pixels in the same position
	ShiftedOut.positionCS = TransformWorldToHClip(ShiftedOut.positionWS + float3(0.0,-1.0,0.0));
	return ShiftedOut;
}

//Geo shader to get the normal for every triangle
[maxvertexcount(3)]
void OceanDepthPassGeometry(
	triangle Varyings i[3],
	inout TriangleStream<Varyings> stream)
{
	float3 p0 = i[0].positionWS;
	float3 p1 = i[1].positionWS;
	float3 p2 = i[2].positionWS;

	float3 ViewDirection = normalize(p0 - _WorldSpaceCameraPos);
	float3 N = normalize(cross(p1 - p0, p2 - p0));
	float3 doubledNormal = float3(0.0, 0.0, 0.0);
	faceforward(doubledNormal, ViewDirection, N);

	//i[0].DebugColor.w = doubledNormal.y;
	//i[1].DebugColor.w = doubledNormal.y;
	//i[2].DebugColor.w = doubledNormal.y;
	
	stream.Append(i[0]);
	stream.Append(i[1]);
	stream.Append(i[2]);
}

float4 OceanDepthPassFragmentFront(Varyings input, bool IsFront:SV_IsFrontFace) : SV_TARGET
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//GetNormal
	//float4 NormalFoam = GetOceanNormal(input.UV);
	//float3 baseNormalWS = normalize(NormalFoam.xyz);

	//float3 viewDirection = -normalize(_WorldSpaceCameraPos - input.positionWS);

	//float Facing = dot(baseNormalWS, viewDirection);

	//reconstruct Normal This may be the cause of that seam at horizal!!!
	float Facing = IsFront ? 1.0 : -1.0;
	
    float pixelDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
    float pixelDepth01 = Linear01Depth(input.positionCS.z, _ZBufferParams);

    float pixelDepth10 = 1-Linear01Depth(input.positionCS.z, _ZBufferParams);
	
    return float4(0.0, Facing, 0.0, pixelDepth10);
	//return depth;
}

float4 OceanDepthPassFragmentBack(Varyings input, bool IsFront:SV_IsFrontFace) : SV_TARGET
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);

    float pixelDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
    float pixelDepth01 = Linear01Depth(input.positionCS.z, _ZBufferParams);
	
	return float4(0.0, -1.0, 0.0, input.depth01);

}

float4 OceanBackPassFragment(Varyings input) : SV_TARGET
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//IsOrthographicCamera() ?
	//OrthographicDepthBufferToLinear(input.positionCS.z): 
	//input.positionCS.w;
	float3 ToplightDir = float3(0.0,1.0,0.0);
	float LightGradient = dot( 
		normalize(ToplightDir + GetOceanNormal(input.UV).xyz),  
		-normalize(_WorldSpaceCameraPos - input.positionWS));
	
    LightGradient = pow(LightGradient, 5.0);
    return float4(LightGradient, LightGradient, LightGradient, 1.0) + _DarkColor;
	//return depth;
}


float4 OceanPassFragment(Varyings input) : SV_TARGET 
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	
	
	//Depth10 for ocean depth(not used in here)
	float OceanDepth10 = LOAD_TEXTURE2D(_CameraOceanDepthTexture, input.positionCS.xy).a;
	float OceanDepthDelta = input.depth01 - OceanDepth10;
	//!!!!this breaks when the render scale changes!!!!
	//Also water fog should take effect
	float4 OceanDelta01 = clamp(-OceanDepthDelta*5000, 0.0, 1.0);
	//clip water to avoid render water layer when the tiles are high
	//clip(OceanDepthDelta < 0.0 ? -1 : 1);

	//sample normal foam mask 
	float4 NormalFoam = GetOceanNormal(input.UV);
	float3 baseNormalWS = normalize(NormalFoam.xyz);
	float foam = NormalFoam.w;

	//sample foam cap tex and foam trail tex and cal the final foam MASK
	float foamCap = pow(abs(foam + 0.3f), 100.0f);
	float TailTex = _FoamTrailTexture.Sample(sampler_FoamTrailTexture, input.StaticUV * 0.25f).b * 2.0f;
	float foamTrail = pow(abs(foam + _FoamFresnelOffsetPow.x), _FoamFresnelOffsetPow.y) * TailTex;
    float foamMask = (1 - TailTex) > (foam * 2.0) ? 0.0 : 1.0; //clamp(max(foamCap, foamTrail), 0.0f, 1.0f);
    foamMask *= (foam * 2.0);
	//Get the detail normal and combine with base normal
	//detial normal need multi sample for distance fade and 
	//panning to add dynamic, Also a multiplier to adjust effect 
	float3 DetailTangentNormal = GetTangentDetailNormal(input.StaticUV * 0.05f);
	float3 normalWS = DetailTangentNormalToWorld(DetailTangentNormal, baseNormalWS);
    //float3 normalWS = baseNormalWS;
	
	
	//Waht do we need
	//banding color on Water surface with foam(color changes based on sun and other lights
	//	sun direction indicate Banding, strength&color indicate color tint, other lights uses additive*(1-tranparency)*diffuse? to color) 
	//limited tranparency on the water (Depth clip (relative to water surface) in object side)
	//limited tranparency but better view range below the water (Depth clip in object side) In another pass
	//specular from directional light(only sun)
	//specular reflection only relate to sky box and in frsnel&distance
	//frsnel on edge

	//******Surface setup******
	Surface surface;
	surface.position = input.positionWS;
	surface.normal = normalWS;
	surface.interpolatedNormalWS = baseNormalWS;

	surface.alpha = 1.0f;
	surface.occlusion = 1.0f;
	surface.fresnelStrength = 1;
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
	surface.depth = -TransformWorldToView(input.positionWS).z;
	surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);
	surface.renderingLayerMask = asuint(unity_RenderingLayer.x);// treat float as uint
	surface.foamMask = foamMask;
	surface.transparency = 1.0f;
	surface.smoothness = 0.9f;

	TRDF trdf = GetTRDF(surface);

	GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, trdf);
	//temp solution to disable GI diffuse
	//gi.diffuse = 0;
	

	float3 objcolor = GetOceanLighting(surface, trdf, gi);

	//?????Dont konw why the OrthographicDepthBufferToLinear cause the tile to offsets?????
	//bufferDepth = IsOrthographicCamera() ?
		//OrthographicDepthBufferToLinear(bufferDepth) :
		//LinearEyeDepth(bufferDepth, _ZBufferParams);
	//return float4(INPUT_PROP(_BaseColor).rgb, 0.5);
	
	//Basic lighting
	float4 color = float4(surface.normal,1- surface.transparency);
	
	//return float4(foam,0.0f,0.0f, 1.0f);
	
	//return float4(input.UV.xy % 0.07f * 10.0f+float2(0.01,0.01), 0.0,1.0);
	/*
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

	float foamCap = pow(abs(foam + 0.3f),100.0f);
	float TailTex = _FoamTrailTexture.Sample(sampler_FoamTrailTexture, input.StaticUV * 0.1f).r * 2.0f;
	float foamTrail = pow(abs(foam + _FoamFresnelOffsetPow.x),  _FoamFresnelOffsetPow.y) * TailTex;
	float foamMask = clamp(
		max(foamCap, foamTrail),
		0.0f,
		1.0f);
	color = lerp(color, _FoamColor, foamMask);
	float fresnelMask = clamp(
		pow(abs(1-ViewNormalGradient + _FoamFresnelOffsetPow.z), _FoamFresnelOffsetPow.w),
		0.0f,
		1.0f);
	color = lerp(color, _FresnelColor, fresnelMask);
	
	color += clamp(pow(abs(SunReflect + 0.25), 100), 0 ,1) * pow(abs(1-foamMask),5) * 1.5f;
	*/

	//brdf.diffuse = _BaseColor;
	//float3 objcolor = GetOceanLighting(surface, brdf, gi);

	//return float4(LightNormalGradient, 0.0,0.0,1.0);
	//max(color.a, 1-ViewNormalGradient)
	Fragment frag = GetFragment(input.positionCS);
	float3 buffercolor = GetBufferColor(frag).rgb;

	objcolor = lerp(objcolor, buffercolor, surface.transparency);
	return float4(objcolor, 1.0f);
	//return float4(objcolor,1-surface.transparency);//
	//SunReflect = dot(normalize(float3(-0.5, -0.5, 0.0)), reflectDir);

	//return float4(SunReflect, 0.0, 0.0,1.0f) + float4(0.1f,0.1f,0.1f,0.0f);
}

#endif