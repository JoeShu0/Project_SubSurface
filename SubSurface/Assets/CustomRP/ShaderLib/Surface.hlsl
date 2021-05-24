#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

struct Surface
{
	float3 position;
	float3 normal;
	float3 interpolatedNormalWS;
	float3 color;
	float3 viewDirection;//from frag to view
	float depth;
	float alpha;
	float metallic;
	float occlusion;
	float smoothness;
	float fresnelStrength;
	float dither;
	uint renderingLayerMask;//bit mask for rendering layer
};

#endif
