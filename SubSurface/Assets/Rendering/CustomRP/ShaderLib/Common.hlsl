#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"

#define UNITY_MATRIX_M     unity_ObjectToWorld
#define UNITY_MATRIX_I_M   unity_WorldToObject
#define UNITY_MATRIX_V     unity_MatrixV
#define UNITY_MATRIX_I_V   unity_MatrixInvV
#define UNITY_MATRIX_P     OptimizeProjectionMatrix(glstate_matrix_projection)
#define UNITY_MATRIX_I_P   unity_MatrixInvP
#define UNITY_MATRIX_VP    unity_MatrixVP
#define UNITY_MATRIX_I_VP  unity_MatrixInvVP
#define UNITY_MATRIX_MV    mul(UNITY_MATRIX_V, UNITY_MATRIX_M)
#define UNITY_MATRIX_T_MV  transpose(UNITY_MATRIX_MV)
#define UNITY_MATRIX_IT_MV transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V))
#define UNITY_MATRIX_MVP   mul(UNITY_MATRIX_VP, UNITY_MATRIX_M)
#define UNITY_PREV_MATRIX_M   unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M unity_MatrixPreviousMI

//If we are using shadow distance mode, 
//we need define the shadowmask before input unityinstancing, otherwise the GPU Instancing will break
#if defined(_SHADOW_MASK_DISTANCE) || defined(_SHADOW_MASK_ALWAYS)
	#define SHADOW_SHADOWMASK
#endif
//these include files requires the var above to be defined
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
//texture packing?
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);

bool IsOrthographicCamera () 
{
	return unity_OrthoParams.w;
}

float OrthographicDepthBufferToLinear (float rawDepth) 
{
	#if UNITY_REVERSED_Z
		rawDepth = 1.0 - rawDepth;
	#endif
	return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y;
}


#include "Fragment.hlsl"

float Square(float v)
{
	return v * v;
}

float DistanceSquared(float3 pA, float3 pB)
{
	return dot(pA - pB, pA - pB);
}

void ClipLOD (Fragment fragment, float fade)
{
	#if defined(LOD_FADE_CROSSFADE)
		float dither = InterleavedGradientNoise(fragment.positionSS, 0);
		clip(fade + (fade < 0.0 ? dither : -dither));
	#endif
}

float3 DecodeNormal (float4 sample, float scale)
{
	//unpack normal texture based on platform
	#if defined(UNITY_NO_DXT5nm)
	return UnpackNormalRGB(sample, scale);
	#else
	return UnpackNormalmapRGorAG(sample, scale);
	#endif
}

float3 NormalTangentToWorld (float3 normalTS, float3 normalWS, float4 tangentWS)
{
	float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
	return TransformTangentToWorld(normalTS, tangentToWorld);
}

bool IsPerspectiveProjection()
{
    return (unity_OrthoParams.w == 0);
}

// Returns the forward (central) direction of the current view in the world space.
float3 GetViewForwardDir()
{
    float4x4 viewMat = UNITY_MATRIX_V;
    return -viewMat[2].xyz;
}

half3 GetWorldSpaceNormalizeViewDir(float3 positionWS)
{
    if (IsPerspectiveProjection())
    {
        // Perspective
        float3 V = _WorldSpaceCameraPos - positionWS;
        return half3(normalize(V));
    }
    else
    {
        // Orthographic
        return half3(-GetViewForwardDir());
    }
}

#endif
