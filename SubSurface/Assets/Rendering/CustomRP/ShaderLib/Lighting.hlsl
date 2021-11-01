#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED


float3 IncomingLight(Surface surface, Light light)
{
	return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
	return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

bool RenderingLayerMaskOverlap(Surface surface, Light light)
{
	//bitwise operation for mask AND.
	return (surface.renderingLayerMask & light.renderingLayerMask) != 0;
}

float3 GetLighting(Surface surfaceWS, BRDF brdf, GI gi)
{
	//Get the per-pixel shadow data
	ShadowData shadowData = GetShadowData(surfaceWS);
	shadowData.shadowMask = gi.shadowMask;
	//return gi.shadowMask.shadows.rgb;
	//float3 color = gi.diffuse * brdf.diffuse;
	float3 color = IndirectBRDF(surfaceWS, brdf, gi.diffuse, gi.specular);
	//loop for directional lighing
	for (int i = 0; i < GetDirectionalLightCount(); i++)
	{
		Light light = GetDirectionalLight(i, surfaceWS, shadowData);
		if(RenderingLayerMaskOverlap(surfaceWS, light))
		{
			color += GetLighting(surfaceWS, brdf, light);
		}
	}

	//loop for other lights
	#if defined(_LIGHTS_PER_OBJECT)
		for (int j = 0; j < min(unity_LightData.y, 8); j++)
		{
			int lightIndex = unity_LightIndices[(uint)j/4][(uint)j%4];
			Light light = GetOtherLight(lightIndex, surfaceWS, shadowData);
			if(RenderingLayerMaskOverlap(surfaceWS, light))
			{
				color += GetLighting(surfaceWS, brdf, light);
			}
		}
	#else
		for (int j = 0; j < GetOtherLightCount(); j++)
		{
			Light light = GetOtherLight(j, surfaceWS, shadowData);
			if(RenderingLayerMaskOverlap(surfaceWS, light))
			{
			color += GetLighting(surfaceWS, brdf, light);
			}
			//color += light.direction;
		}
	#endif

	return color;
}



#endif
