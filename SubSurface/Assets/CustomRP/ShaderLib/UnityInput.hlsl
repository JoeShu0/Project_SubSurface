#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED

//Enable unity SRP Batcher forbuffer per Drawcall
CBUFFER_START(UnityPerDraw)
	//these values will be set by unity once per draw call
	float4x4 unity_ObjectToWorld;
	float4x4 unity_WorldToObject;
	float4 unity_LODFade;

	real4 unity_WorldTransformParams;
	//perobject light stuff must following unity_WorldTransformParams
	real4 unity_LightData;//number of lights in Y
	real4 unity_LightIndices[2];//up to 8 lights index

	float4 unity_OrthoParams;
	float4 _ProjectionParams;
	float4 _ScreenParams;//xy is the screen dimensions
	float4 _ZBufferParams;//conversion factors for depth raw to linear

	//object layer mask
	float4 unity_RenderingLayer;

	//Unity probes occlusion data for dynamic assets
	float4 unity_ProbesOcclusion;
	float4 unity_SpecCube0_HDR; //if the reflect map use HDR or not

	float4 unity_LightmapST;//light map UV offset
	float4 unity_DynamicLightmapST;//for SRP batcher

	//lighting probe coefficient
	float4 unity_SHAr;
	float4 unity_SHAg;
	float4 unity_SHAb;
	float4 unity_SHBr;
	float4 unity_SHBg;
	float4 unity_SHBb;
	float4 unity_SHC;

	//LightProbeProxyVolume
	float4 unity_ProbeVolumeParams;
	float4x4 unity_ProbeVolumeWorldToObject;
	float4 unity_ProbeVolumeSizeInv;
	float4 unity_ProbeVolumeMin;
CBUFFER_END

float3 _WorldSpaceCameraPos;
float4x4 unity_MatrixV;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;

#endif
