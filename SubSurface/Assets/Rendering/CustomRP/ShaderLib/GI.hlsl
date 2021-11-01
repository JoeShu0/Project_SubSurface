#ifndef CUSTOM_GI_INCLUDED
#define CUSTOM_GI_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"

//TEX and sampler for lightmap
TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);
//Tex for shadow mask
TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);
//TEX and SP for LPPV
TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
SAMPLER(samplerunity_ProbeVolumeSH);

//reflection map
TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);

#if defined(LIGHTMAP_ON)
	#define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
	#define GI_VARYINGS_DATA float2 lightMapUV : VAR_LIGHT_MAP_UV;
	#define TRANSFER_GI_DATA(input, output) \
			output.lightMapUV = input.lightMapUV * \
			unity_LightmapST.xy + unity_LightmapST.zw;
	#define GI_FRAGMENT_DATA(input) input.lightMapUV //A variable so no ";"
#else
	#define GI_ATTRIBUTE_DATA
	#define GI_VARYINGS_DATA
	#define TRANSFER_GI_DATA(input, output)
	#define GI_FRAGMENT_DATA(input) 0.0
#endif

struct GI
{
	float3 diffuse;
	float3 specular;
	ShadowMask shadowMask;
};

float3 SampleEnvironment(Surface surfaceWS, BRDF brdf)
{
	//uvw is the reflection direction of camera ray
	float3 uvw = reflect(-surfaceWS.viewDirection, surfaceWS.normal);
	//blurred reflection is stired in mip of reflection map, we can use roughness to get it
	float mip = PerceptualRoughnessToMipmapLevel(brdf.perceptualRoughness);
	float4 environment = SAMPLE_TEXTURECUBE_LOD(
		unity_SpecCube0, samplerunity_SpecCube0, uvw , mip);
		
	return DecodeHDREnvironment(environment, unity_SpecCube0_HDR);
}

float4 SampleBakedShadows(float2 lightMapUV, Surface surfaceWS)
{//Only lightmapped obj can have attenuation
#if defined(LIGHTMAP_ON)
	return SAMPLE_TEXTURE2D(
		unity_ShadowMask,
		samplerunity_ShadowMask,
		lightMapUV);
#else
	//use LPPV occlusion or probe occlusion
	if (unity_ProbeVolumeParams.x)
	{
		return SampleProbeOcclusion(
			TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
			surfaceWS.position, unity_ProbeVolumeWorldToObject,
			unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
			unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz
		);
	}
	else {
		return unity_ProbesOcclusion;
	}
#endif
}

float3 SampleLightMap(float2 lightMapUV)
{
	#if defined(LIGHTMAP_ON)
	return SampleSingleLightmap(
		TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),//pass the lighttex and sample as to args
		lightMapUV,//lightmap UV
		float4(1.0,1.0,0.0,0.0),//UV transformation(We have done this in the TRANSFER_GI_DATA)
		#if defined(UNTIY_LIGHTMAP_FULL_HDR)//Is the texture compressed
			false,
		#else
			true,
		#endif
		float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0)
		);
	#else
	return 0.0;
	#endif
}

float3 SampleLightProbe(Surface surfaceWS)
{
	#if defined(LIGHTMAP_ON)
		return 0.0;
	#else
		if (unity_ProbeVolumeParams.x)
		{
			return SampleProbeVolumeSH4(
				TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
				surfaceWS.position, surfaceWS.normal,
				unity_ProbeVolumeWorldToObject,
				unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
				unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz
			);//sample the LPPV 
		}
		float4 coefficient[7];
		coefficient[0] = unity_SHAr;
		coefficient[1] = unity_SHAg;
		coefficient[2] = unity_SHAb;
		coefficient[3] = unity_SHBr;
		coefficient[4] = unity_SHBg;
		coefficient[5] = unity_SHBb;
		coefficient[6] = unity_SHC;
		return max(0.0, SampleSH9(coefficient, surfaceWS.normal));
		//sample the probe based on normalWS 
	#endif
}

GI GetGI(float2 lightMapUV, Surface surfaceWS, BRDF brdf)
{
	GI gi;
	gi.diffuse = SampleLightMap(lightMapUV) + SampleLightProbe(surfaceWS);
	gi.specular = SampleEnvironment(surfaceWS, brdf);

	gi.shadowMask.distance = false;
	gi.shadowMask.shadows = 1.0;
	gi.shadowMask.b_always = false;

	#if defined(_SHADOW_MASK_ALWAYS)
		gi.shadowMask.b_always = true;
		gi.shadowMask.shadows = SampleBakedShadows(lightMapUV, surfaceWS);
	#elif defined(_SHADOW_MASK_DISTANCE)
		gi.shadowMask.distance = true;
		gi.shadowMask.shadows = SampleBakedShadows(lightMapUV, surfaceWS);
	#endif
	return gi;
}


#endif