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

float4 unity_OrthoParams;
float4 _ProjectionParams;// this input is not supported in SRP batcher
float4 _ScreenParams;//xy is the screen dimensions
float4 _ZBufferParams;//conversion factors for depth raw to linear


float4 _ScaledScreenParams;

// TODO: all affine matrices should be 3x4.
// TODO: sort these vars by the frequency of use (descending), and put commonly used vars together.
// Note: please use UNITY_MATRIX_X macros instead of referencing matrix variables directly.
float4x4 _PrevViewProjMatrix;
float4x4 _ViewProjMatrix;
float4x4 _NonJitteredViewProjMatrix;
float4x4 _ViewMatrix;
float4x4 _ProjMatrix;
float4x4 _InvViewProjMatrix;
float4x4 _InvViewMatrix;
float4x4 _InvProjMatrix;
float4 _InvProjParam;
float4 _ScreenSize; // {w, h, 1/w, 1/h}
float4 _FrustumPlanes[6]; // {(a, b, c) = N, d = -dot(N, P)} [L, R, T, B, N, F]


float4x4 OptimizeProjectionMatrix(float4x4 M)
{
    // Matrix format (x = non-constant value).
    // Orthographic Perspective  Combined(OR)
    // | x 0 0 x |  | x 0 x 0 |  | x 0 x x |
    // | 0 x 0 x |  | 0 x x 0 |  | 0 x x x |
    // | x x x x |  | x x x x |  | x x x x | <- oblique projection row
    // | 0 0 0 1 |  | 0 0 x 0 |  | 0 0 x x |
    // Notice that some values are always 0.
    // We can avoid loading and doing math with constants.
    M._21_41 = 0;
    M._12_42 = 0;
    return M;
}
#endif
