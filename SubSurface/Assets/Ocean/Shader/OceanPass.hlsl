#ifndef CUSTOM_OCEAN_PASS_INCLUDED
#define CUSTOM_OCEAN_PASS_INCLUDED


struct Attributes
{
	float3 positionOS : POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID//this will store the instance ID
};

struct Varyings
{
	float4 positionCS_SS : SV_POSITION;
	float3 positionWS : VAR_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings OceanPassVertex(Attributes input)
{
	Varyings output;
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//transfer instance ID to frag
	UNITY_TRANSFER_INSTANCE_ID(input, output);

	//Get World and ScreenSpace position
	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS_SS = TransformWorldToHClip(output.positionWS);

	//float3 SnapedPosition= SnapToWorldPosition(output.positionWS);

	return output;
}

float4 OceanPassFragment(Varyings input) : SV_TARGET 
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);

	return float4(1.0,0.5,0.5,1.0);
}

#endif