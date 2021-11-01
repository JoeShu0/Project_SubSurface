#ifndef CUSTOM_Fog_INCLUDED
#define CUSTOM_Fog_INCLUDED

float GetWaterExpFogBlendValue(float Depth)
{
	return 0.0;
}

float GetWaterTempBlendValue(float DepthDelta)
{
	if(DepthDelta==0.0)
		return 0.0;
	else if(DepthDelta > 0.0)
		return 0.0;
	else 
		return 1.0;
	return clamp(-DepthDelta * 10000, 0.0, 1.0);
}

float GetWaterTempBlendValue(float2 ScreenUV, float PixelDepth01, float3 viewDirection)
{
	float4 OceanDepthBuffer = LOAD_TEXTURE2D(_CameraOceanDepthTexture, ScreenUV);
	float OceanDepth01 = OceanDepthBuffer.a;
	//float OceanFacing = OceanDepthBuffer.b;
	float NormalY = OceanDepthBuffer.y;
	
	float OceanDepthDelta = 0;
	if (OceanDepth01 == 0.0)//BG is inifinte far or sky
	{
		OceanDepthDelta = (-viewDirection.y >= 0) ? 0.0 : 1.0;
		//OceanDepthDelta = 1.0;
	}
	else
	{
		OceanDepthDelta = (NormalY >= 0.0) ? OceanDepth01 - PixelDepth01 : PixelDepth01;
	}
	/*
	if (OceanFacing => 0.0)
	{
		OceanDepthDelta = OceanDepth01 - PixelDepth01;
	}
	else
	{
		OceanDepthDelta = PixelDepth01;
	}
	float OceanDepthDelta = PixelDepth01 - OceanDepth10;
	*/


	return clamp(OceanDepthDelta * 10000, 0.0, 1.0);
}

#endif