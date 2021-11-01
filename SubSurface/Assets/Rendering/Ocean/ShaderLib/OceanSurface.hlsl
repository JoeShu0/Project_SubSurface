#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

struct Surface
{
	float3 position;
	float3 normal;
	float3 interpolatedNormalWS;
	float transparency;
	float foamMask;
	float smoothness;
	float3 viewDirection;//from frag to view
	float depth;
	float alpha;
	float occlusion;
	float fresnelStrength;
	float dither;
	uint renderingLayerMask;//bit mask for rendering layer
};

#endif