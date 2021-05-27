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
	float depth01 : VAR_DEPTH01;
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
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	
	//Snap tp 2*unit grid(Should be scaled by whole ocean)
	positionWS = SnapToWorldPosition(positionWS,  1);
	//Transition at the edge of LODs
	positionWS = TransitionLOD(positionWS, 1);


	float3 localPos = mul(unity_WorldToObject, float4(positionWS, 1.0)).xyz;
	//float4 SrcPos = TransformObjectToHClip(localPos);
	float4 SrcPos = TransformWorldToHClip(positionWS);
	float depth01 = TransformWorldToView(positionWS).z * _ProjectionParams.w;

	//output.positionCS_SS = TransformWorldToHClip(output.positionWS);
	output.positionWS = positionWS;
	output.positionCS_SS = SrcPos;
	output.depth01 = depth01;
	return output;
}

float4 OceanDepthPassFragment(Varyings input) : SV_TARGET
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//float depth10 = 1 - ;
		//IsOrthographicCamera() ?
		//OrthographicDepthBufferToLinear(input.positionCS_SS.z): 
		//input.positionCS_SS.w;
	return float4(0.0,0.0,0.0, input.depth01);
	//return depth;
}

float4 OceanPassFragment(Varyings input) : SV_TARGET 
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	float OceanDepth = LOAD_TEXTURE2D(_CameraOceanDepthTexture, input.positionCS_SS.xy).a;

	//?????Dont konw why the OrthographicDepthBufferToLinear case the tile to offsets?????
	//bufferDepth = IsOrthographicCamera() ?
		//OrthographicDepthBufferToLinear(bufferDepth) :
		//LinearEyeDepth(bufferDepth, _ZBufferParams);
	return float4(INPUT_PROP(_BaseColor).rgb, 0.5);
	//return float4(OceanDepth, 0.0, 0.0, 1.0);
}

#endif