#ifndef CUSTOM_OCEAN_GET_LIGHTING_INCLUDED
#define CUSTOM_OCEAN_GET_LIGHTING_INCLUDED


float3 IncomingLightForOcean(Surface surface, Light light)
{
	return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;
}

float IncomingLightGradient(Surface surface, Light light)
{
	return saturate(dot(surface.normal, light.direction) * light.attenuation);
}

float3 GetOceanLighting(Surface surface, TRDF trdf, Light light)
{
	return IncomingLightForOcean(surface, light) * DirectTRDF(surface, trdf, light);
}

float3 GetOceanLightDirectHightLight(Surface surface, TRDF trdf, Light light)
{
	return DirectHighLight(surface, trdf, light) * light.color;
}


bool RenderingLayerMaskOverlap(Surface surface, Light light)
{
	//bitwise operation for mask AND.
	return (surface.renderingLayerMask & light.renderingLayerMask) != 0;
}

void GenerateDirectionalLightEffectData(Surface surface, TRDF trdf, Light light,
	inout float3 ColorTint, inout float Gradient, inout float3 Highlight, inout float3 Subsurface)
{
	Highlight += DirectHighLight(surface, trdf, light) * clamp(light.attenuation + 0.001f, 0.0, 1.0) * light.color;
	Subsurface += GetSubSurfaceColor(surface, light) * clamp(light.attenuation + 0.1f, 0, 1);
	Gradient += IncomingLightGradient(surface, light) * clamp(light.attenuation + 0.1f, 0, 1);
	ColorTint += light.color;
}

float3 GetOceanColorBanding( float Gradient)
{
	float3 color = _DarkColor.rgb;

	float baseMask = clamp(
		pow(abs(Gradient + _BandingOffsetPow.x), _BandingOffsetPow.y),
		0.0f,
		1.0f);
	color = lerp(color, _BaseColor.rgb, baseMask);

	float brightMask = clamp(
		pow(abs(Gradient + _BandingOffsetPow.z), _BandingOffsetPow.w),
		0.0f,
		1.0f);
	color = lerp(color, _BrightColor.rgb, brightMask);
	
	return color;
}



float3 GetOceanLighting(Surface surfaceWS, TRDF trdf, GI gi)
{
	//Get the per-pixel shadow data
	ShadowData shadowData = GetShadowData(surfaceWS);
	shadowData.shadowMask = gi.shadowMask;
	//return gi.shadowMask.shadows.rgb;
	//float3 color = gi.diffuse * brdf.diffuse;
	//gi.diffuse = 1.0f;
	//float3 color = IndirectBRDF(surfaceWS, brdf, gi.diffuse, gi.specular);
	float3 ColorTint = float3(0.0, 0.0, 0.0);
	//return color;
	float Gradient = 0.0;
	float3 Highlight = float3(0.0, 0.0, 0.0);
	float3 Subsurface = float3(0.0, 0.0, 0.0);
	//loop for directional lighing

	for (int i = 0; i < GetDirectionalLightCount(); i++)
	{
		Light light = GetDirectionalLight(i, surfaceWS, shadowData);
		//color += light.attenuation;
		if (RenderingLayerMaskOverlap(surfaceWS, light))
		{
			GenerateDirectionalLightEffectData(surfaceWS, trdf, light, ColorTint, Gradient, Highlight, Subsurface);
		}
	}

	//gradient does not take light intensity into count.addint directional light in runtime will cause problem
	Gradient /= (float)GetDirectionalLightCount();
	float3 color = GetOceanColorBanding(Gradient);
	//this add makes the foam too stand out....
	color += surfaceWS.foamMask * clamp(Gradient+0.05f, 0,1);
	color += Highlight;
	color += Subsurface;
	color *= ColorTint;

	
	//loop for other lights
#if defined(_LIGHTS_PER_OBJECT)
	for (int j = 0; j < min(unity_LightData.y, 8); j++)
	{
		int lightIndex = unity_LightIndices[(uint)j / 4][(uint)j % 4];
		Light light = GetOtherLight(lightIndex, surfaceWS, shadowData);
		if (RenderingLayerMaskOverlap(surfaceWS, light))
		{
			color += GetOceanLighting(surfaceWS, trdf, light);
		}
	}
#else
	for (int j = 0; j < GetOtherLightCount(); j++)
	{
		Light light = GetOtherLight(j, surfaceWS, shadowData);
		if (RenderingLayerMaskOverlap(surfaceWS, light))
		{
			color += GetOceanLighting(surfaceWS, trdf, light);
		}
	}
#endif

	return color;
	
}



#endif