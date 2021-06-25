#ifndef CUSTOM_OCEAN_BRDF_INCLUDED
#define CUSTOM_OCEAN_BRDF_INCLUDED

#define MIN_REFLECTIVITY 0.04

struct TRDF
{
	// In Ocean We fisrt calculate energy not color
	float diffuse;
	float reflection;
	float refraction;
	float roughness;
	float perceptualRoughness;
	float fresnel;
};

float oneMinusReflectivity(float metallic)
{
	float range = 1.0 - MIN_REFLECTIVITY;
	return range - metallic * range;
}

float SpecularStrength(Surface surface, TRDF trdf, Light light)
{
	//based on CookTorrance??
	float3 h = normalize(light.direction + surface.viewDirection);
	float nh2 = Square(saturate(dot(surface.normal, h)));
	float lh2 = Square(saturate(dot(light.direction, h)));
	float r2 = Square(trdf.roughness);
	float d2 = Square(nh2 * (r2 - 1.0) + 1.0001);
	float normalization = trdf.roughness * 4.0 + 2.0;
	return r2 / (d2 * max(0.1, lh2) * normalization);
}

float DirectHighLight(Surface surface, TRDF trdf, Light light)
{
	return SpecularStrength(surface, trdf, light) * trdf.reflection;
}

float3 DirectTRDF(Surface surface, TRDF trdf, Light light)
{
	return SpecularStrength(surface, trdf, light) * trdf.reflection +trdf.diffuse;
}

float3 IndirectTRDF(Surface surface, TRDF trdf, float3 GIdiffuse, float3 GIspecular)
{
	float fresnelStength = surface.fresnelStrength * Pow4(1.0 - saturate(dot(surface.normal, surface.viewDirection)));
	float3 reflection = GIspecular * lerp(trdf.reflection, trdf.fresnel, fresnelStength);
	reflection /= trdf.roughness *trdf.roughness + 1.0;	
	return (GIdiffuse * trdf.diffuse + reflection) * surface.occlusion;
}

TRDF GetTRDF(inout Surface surface)
{
	//energe distribute to diffuse, reflection and refraction
	TRDF trdf;
	trdf.diffuse = (1 - surface.transparency) * surface.foamMask;
	trdf.reflection = (1 - surface.transparency) * (1- surface.foamMask) ;
	trdf.refraction = surface.transparency;

	//brdf need the perceptualRoughness to get the blurred reflection mip
	trdf.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	trdf.roughness = PerceptualRoughnessToRoughness(trdf.perceptualRoughness);
	trdf.fresnel = saturate(surface.smoothness);
	return trdf;
}



#endif
