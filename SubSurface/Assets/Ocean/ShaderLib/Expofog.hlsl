#ifndef CUSTOM_Fog_INCLUDED
#define CUSTOM_Fog_INCLUDED

float GetWaterExpFogBlendValue(float Depth)
{
	return 0.0;
}

float GetWaterTempBlendValue(float DepthDelta)
{
	if(DepthDelta>0.0001)
		return 0.0;
	else
		return 1.0;
	return clamp(-DepthDelta * 10000, 0.0, 1.0);
}

#endif