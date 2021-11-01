#ifndef CAMERA_RENDERER_PASSES_INCLUDED
#define CAMERA_RENDERER_PASSES_INCLUDED

TEXTURE2D(_SourceTexture);
//SAMPLER(sampler_Linear_clamp);//sampler is in common.hlsl

struct Varyings {
	float4 positionCS : SV_POSITION;
	float2 screenUV : VAR_SCREEN_UV;
};

Varyings DefaultPassVertex(uint vertexID : SV_VertexID)
{
	Varyings output;
	output.positionCS = float4(
		vertexID <= 1 ? -1.0 : 3.0,
		vertexID == 1 ? 3.0 : -1.0,
		0.0, 1.0
	);
	output.screenUV = float2(
		vertexID <= 1 ? 0.0 : 2.0,
		vertexID == 1 ? 2.0 : 0.0
	);
	//some case the image is flipped, unity tell us if need flip in _ProjectionParams
	if (_ProjectionParams.x < 0.0)
	{
		output.screenUV.y = 1.0 - output.screenUV.y;
	}

	return output;
}

float4 CopyPassFragment(Varyings input) : SV_TARGET
{
	//return float4(input.screenUV, 0.0, 1.0);

	//return GetSource(input.screenUV);

	return SAMPLE_TEXTURE2D_LOD(_SourceTexture, sampler_linear_clamp, input.screenUV, 0);
}

float4 CopyDepthPassFragment(Varyings input) : SV_TARGET
{
	//return float4(input.screenUV, 0.0, 1.0);

	//return GetSource(input.screenUV);

	return SAMPLE_DEPTH_TEXTURE_LOD(_SourceTexture, sampler_point_clamp, input.screenUV, 0);
}


#endif