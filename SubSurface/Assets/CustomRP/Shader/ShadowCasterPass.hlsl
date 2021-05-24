#ifndef CUSTOM_SHADOW_CASTER_PASS_INCLUDED
#define CUSTOM_SHADOW_CASTER_PASS_INCLUDED


struct Attributes
{
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID//this will store the instance ID
};

struct Varyings
{
	float4 positionCS_SS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

bool _ShadowPancaking;

Varyings ShadowCasterPassVertex(Attributes input)
{
	Varyings output;
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//transfer instance ID to frag
	UNITY_TRANSFER_INSTANCE_ID(input, output);

	//transform UV based on permaterial ST
	output.baseUV = TransformBaseUV(input.baseUV);

	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS_SS = TransformWorldToHClip(positionWS);
//flatten the geo to the near plane(not solving all problem)
	
	if (_ShadowPancaking) {
		#if UNITY_REVERSED_Z
			output.positionCS_SS.z = min(output.positionCS_SS.z, output.positionCS_SS.w * UNITY_NEAR_CLIP_VALUE);
		#else
			output.positionCS_SS.z = max(output.positionCS_SS.z, output.positionCS_SS.w * UNITY_NEAR_CLIP_VALUE);
		#endif
	}
	return output;
}

float4 ShadowCasterPassFragment(Varyings input) : SV_TARGET
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);

	
	
	//use the new packed config instead of UV
	InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV);
	//LOD fade, the fade factor is in x com of unity_LODFade
	ClipLOD(config.fragment, unity_LODFade.x);
	//get the basemap * basecolor
	float4 base = GetBase(config);
	
#if defined(_SHADOWS_CLIP)
	clip(base.a - GetCutoff(config));
#elif defined(_SHADOWS_DITHER)
	float dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
	clip(base.a - dither);
#endif

	return base;
}

#endif
