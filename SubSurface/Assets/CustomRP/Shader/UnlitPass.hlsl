#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED


struct Attributes
{
	float3 positionOS : POSITION;
	float4 color : COLOR;
#if defined(_FLIPBOOK_BLENDING)
	float4 baseUV : TEXCOORD0;
	float flipbookBlend : TEXCOORD1;
#else
	float2 baseUV : TEXCOORD0;
#endif
	float3 normalOS : NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID//this will store the instance ID
};

struct Varyings
{
	float4 positionCS_SS : SV_POSITION;
#if defined(_VERTEX_COLORS)
	float4 color : VAR_COLOR;
#endif
	float2 baseUV : VAR_BASE_UV;
#if defined(_FLIPBOOK_BLENDING)
	float3 flipbookUVB : VAR_FLIPBOOK;
#endif
	float3 normalWS : VAR_NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input)
{
	Varyings output;
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//transfer instance ID to frag
	UNITY_TRANSFER_INSTANCE_ID(input, output);

	//transform UV based on permaterial ST
	output.baseUV.xy = TransformBaseUV(input.baseUV.xy);
#if defined(_FLIPBOOK_BLENDING)
	output.flipbookUVB.xy = TransformBaseUV(input.baseUV.zw);
	output.flipbookUVB.z = input.flipbookBlend;
#endif

	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS_SS = TransformWorldToHClip(positionWS);//SS = screen space
	//transfer normal
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);

	#if defined(_VERTEX_COLORS)
		output.color = input.color;
	#endif
	return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET 
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);

	//use the new packed config instead of UV
	InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV);

	//return GetBufferColor(config.fragment, 0.05);
	//return float4(config.fragment.bufferDepth.xxx / 20.0, 1.0);

	//use vertex color
	#if defined(_VERTEX_COLORS)
		config.color = input.color;
	#endif
	//flipbook blending
	#if defined(_FLIPBOOK_BLENDING)
		config.flipbookUVB = input.flipbookUVB;
		config.flipbookBlending = true;
	#endif
	//near fade on camera near clip
	#if defined(_NEAR_FADE)
		config.nearFade = true;
	#endif
	//near fade on object (depth buffer fade)
	#if defined(_SOFT_PARTICLES)
		config.softParticles = true;
	#endif

	//get the basemap * basecolor
	float4 base = GetBase(config);

#if defined(_CLIPPING)
	clip(base.a - GetCutoff(config));
#endif
	
#if defined(_DISTORTION)
	float2 distortion = GetDistortion(config) * base.a;
	base.rgb = lerp(GetBufferColor(config.fragment, distortion).rgb, base.rgb, 
		saturate(base.a - GetDistortionBlend(config)));
#endif

	return float4(base.rgb, GetFinalAlpha(base.a));
}

#endif
