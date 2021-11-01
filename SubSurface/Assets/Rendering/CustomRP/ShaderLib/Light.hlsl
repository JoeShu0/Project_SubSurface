#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64

CBUFFER_START(_CustomLight)
	int _DirectionalLightCount;
	float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirectionalLightDirectionsAndMasks[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];

	int _OtherLightCount;
	float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightDirectionsAndMasks[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];
CBUFFER_END

struct Light
{
	float3 color;
	float3 direction;
	float attenuation;
	uint renderingLayerMask;
};

int GetDirectionalLightCount()
{
	return _DirectionalLightCount;
}

int GetOtherLightCount()
{
	return _OtherLightCount;
}

DirectionalShadowData GetDirectionalLightShadowData(int lightIndex, ShadowData shadowData)
{
	DirectionalShadowData data;

	//due to we want to use Global ShadowData strength(cascade strength) for realtime to baked transition
	//We can not jius apply it here
	data.strength = _DirectionalLightShadowData[lightIndex].x; //* shadowData.strength;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
	data.normalBias = _DirectionalLightShadowData[lightIndex].z;
	data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
	return data;
}

OtherShadowData GetOtherShadowData(int lightIndex)
{
	OtherShadowData data;
	
	data.strength = _OtherLightShadowData[lightIndex].x; //* shadowData.strength;
	data.tileIndex = _OtherLightShadowData[lightIndex].y;
	data.shadowMaskChannel = _OtherLightShadowData[lightIndex].w;
	data.lightPositionWS = 0.0;
	data.spotDirectionWS = 0.0;
	data.isPoint = _OtherLightShadowData[lightIndex].z == 1.0;
	data.lightDirectionWS = 0.0;
	return data;
}

//Get per fragment light data
Light GetDirectionalLight(int index, Surface surfaceWS, ShadowData shadowData)
{
	Light light;
	light.color = _DirectionalLightColors[index].xyz;
	light.direction = _DirectionalLightDirectionsAndMasks[index].xyz;
	light.renderingLayerMask = asuint(_DirectionalLightDirectionsAndMasks[index].w);
	DirectionalShadowData dirShadowData = GetDirectionalLightShadowData(index, shadowData);
	//this attenuation is perfragment not per light
	light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
	//light.attenuation = shadowData.cascadeIndex * 0.25;
	return light;
}

Light GetOtherLight(int index, Surface surfaceWS, ShadowData shadowData)
{
	Light light;
	light.color = _OtherLightColors[index].xyz;
	float3 position = _OtherLightPositions[index].xyz;
	float3 ray = position - surfaceWS.position;
	light.direction = normalize(ray);

	float distanceSqr = max(dot(ray, ray), 0.00001);
	float rangeAttenuation = Square(
		saturate(1.0 - Square(distanceSqr * _OtherLightPositions[index].w))
		);

	float4 spotAngles = _OtherLightSpotAngles[index];
	float3 spotDirection = _OtherLightDirectionsAndMasks[index].xyz;
	light.renderingLayerMask = asuint(_OtherLightDirectionsAndMasks[index].w);
	float spotAttenuation =Square(
		saturate(dot(spotDirection, light.direction) *
		spotAngles.x + spotAngles.y)
	);

	OtherShadowData otherShadowData = GetOtherShadowData(index);
	otherShadowData.lightPositionWS = position;
	otherShadowData.spotDirectionWS = spotDirection;
	otherShadowData.lightDirectionWS = light.direction;
	light.attenuation = GetOtherShadowAttenuation(otherShadowData, shadowData, surfaceWS) * 
		spotAttenuation * rangeAttenuation / distanceSqr;

	return light;
}

#endif
