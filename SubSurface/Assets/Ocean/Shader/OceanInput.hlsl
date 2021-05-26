#ifndef CUSTOM_OCEAN_INPUT_PASS_INCLUDED
#define CUSTOM_OCEAN_INPUT_PASS_INCLUDED

//short cut unity access per material property
#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _GridSize)
    UNITY_DEFINE_INSTANCED_PROP(float4, _TransitionParam)
    UNITY_DEFINE_INSTANCED_PROP(float4, _CenterPos)
    UNITY_DEFINE_INSTANCED_PROP(float, _LODSize)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


float3 SnapToWorldPosition(float3 positionWS, float gridSize, float oceanScale)
{
    float Grid = gridSize * oceanScale;
    float Grid2 = Grid * 2.0f;

    //snap to 2*unit grid(scaled by parent!)
    positionWS.xz -= frac(unity_ObjectToWorld._m03_m23 / Grid2) * Grid2;

    return positionWS;
}

#endif