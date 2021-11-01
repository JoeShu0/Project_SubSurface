#ifndef CUSTOM_POST_FX_PASSES_INCLUDED
#define CUSTOM_POST_FX_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"


TEXTURE2D(_PostFXSource);
TEXTURE2D(_PostFXSource2);
//SAMPLER(sampler_Linear_clamp);//sampler is in common.hlsl

//float _ProjectionParams;
float4 _PostFXSource_TexelSize;
bool _BloomBocubicUpsampling;
float4 _BloomThreshold;
float _BloomIntensity;

float4 _ColorAdjustments;
float4 _ColorFilter;
float4 _WhiteBalance;
float4 _SplitToningShadows, _SplitToningHightlights;
float4 _ChannelMixerRed, _ChannelMixerGreen, _ChannelMixerBlue;
float4 _SMHShadows, _SMHMidtones, _SMHHighlights, _SMHRange;

TEXTURE2D(_ColorGradingLUT);
float4 _ColorGradingLUTParameters;
bool _ColorGradingLUTInLogC;

//percamera blend option
float _FinalSrcBlend, _FinalDstBlend;

float4 GetSourceTexelSize()
{
	return _PostFXSource_TexelSize;
}

float4 GetSource(float2 screenUV)
{
	//we don't have mip for this, use LOD to save some perf
	return SAMPLE_TEXTURE2D_LOD(_PostFXSource, sampler_linear_clamp, screenUV, 0);
}

float4 GetSource2(float2 screenUV)
{
	//we don't have mip for this, use LOD to save some perf
	return SAMPLE_TEXTURE2D_LOD(_PostFXSource2, sampler_linear_clamp, screenUV, 0);
}

float4 GetSourceBicubic(float2 screenUV)
{
	return SampleTexture2DBicubic(
		TEXTURE2D_ARGS(_PostFXSource, sampler_linear_clamp), screenUV,
		_PostFXSource_TexelSize.zwxy , 1.0, 0.0
	);
}

float3 ApplyBloomThreshold(float3 color)
{
	float brightness = Max3(color.r, color.g, color.b);
	float soft = brightness + _BloomThreshold.y;
	soft = clamp(soft, 0.0, _BloomThreshold.z);
	soft = soft * soft * _BloomThreshold.w;
	float contribution = max(soft, brightness - _BloomThreshold.x);
	contribution /= max(brightness, 0.00001);
	return color * contribution;
}

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

	return GetSource(input.screenUV);
}

float4 BloomHorizontalPassFragment(Varyings input) : SV_TARGET
{
	float3 color = 0.0;
	float offsets[] = {
		-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0
	};
	float weights[] = {
		0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
		0.19459459, 0.12162162, 0.05405405, 0.01621622
	};
	for (int i = 0; i < 9; i++)
	{
		float offset = offsets[i] * 2.0 * GetSourceTexelSize().x;
		color += GetSource(input.screenUV + float2(offset, 0.0)).rgb * weights[i];
	}
	return float4(color, 1.0);
}

float4 BloomVerticalPassFragment(Varyings input) : SV_TARGET
{
	float3 color = 0.0;
	//We use bilinear sampler to reduce samples
	float offsets[] = {
		-3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
	};
	float weights[] = {
		0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
	};
	for (int i = 0; i < 5; i++)
	{
		float offset = offsets[i] * GetSourceTexelSize().y;
		color += GetSource(input.screenUV + float2(0.0, offset)).rgb * weights[i];
	}
	return float4(color, 1.0);
}



float4 BloomAddPassFragment(Varyings input) : SV_TARGET
{
	float3 lowRes;
	if (_BloomBocubicUpsampling)
	{
		lowRes = GetSourceBicubic(input.screenUV).rgb;
	}
	else
	{
		lowRes = GetSource(input.screenUV).rgb;
	}
	
	//preserve Alpha after bloom
	float4 highRes = GetSource2(input.screenUV);
	return float4(lowRes * _BloomIntensity + highRes.rgb, highRes.a);
}

float4 BloomPrefilterPassFragment(Varyings input) : SV_TARGET
{
	float3 color = ApplyBloomThreshold(GetSource(input.screenUV).rgb);
	return float4(color, 1.0);
}


float4 BloomPrefilterFireFliesPassFragment(Varyings input) : SV_TARGET
{
	// In order to get rid of fire flies, we are going to blurr the image 
	// 3x3 above the 2x2 bilinear, make it 6x6
	//we also use weighted average based on luminance
	float3 color = 0.0;
	float weightSum = 0.0;
	//because we are goingto gaussian blur after this we can reduce the sample down to 5
	float2 offsets[] = {
		float2(0.0, 0.0),float2(-1.0, -1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(1.0, 1.0)
		//,float2(-1.0, 0.0), float2(1.0, 0.0), float2(0.0, -1.0), float2(0.0, 1.0)
	};
	for(int i = 0; i < 5 ;i++)
	{
		float3 c = 
			GetSource(input.screenUV + offsets[i] * GetSourceTexelSize().xy *2.0).rgb;
		
		c = ApplyBloomThreshold(c);
		//weighted average
		float w = 1.0/ (Luminance(c) + 1.0);
		color += c * w;
		weightSum += w;
	}

	color /= weightSum;
	return float4(color, 1.0);
}

float4 BloomScatterPassFragment(Varyings input) : SV_TARGET
{
	float3 lowRes;
	if (_BloomBocubicUpsampling)
	{
		lowRes = GetSourceBicubic(input.screenUV).rgb;
	}
	else
	{
		lowRes = GetSource(input.screenUV).rgb;
	}

	float3 highRes = GetSource2(input.screenUV).rgb;
	return float4(lerp(highRes, lowRes, _BloomIntensity), 1.0);
}

float4 BloomScatterFinalPassFragment(Varyings input) : SV_TARGET
{
	float3 lowRes;
	if (_BloomBocubicUpsampling)
	{
		lowRes = GetSourceBicubic(input.screenUV).rgb;
	}
	else
	{
		lowRes = GetSource(input.screenUV).rgb;
	}

	//preserve Alpha after bloom
	float4 highRes = GetSource2(input.screenUV);
	lowRes += highRes.rgb - ApplyBloomThreshold(highRes.rgb);
	return float4(lerp(highRes.rgb, lowRes, _BloomIntensity), highRes.a);
}

//Color grading
float Luminance (float3 color, bool useACES) {
	return useACES ? AcesLuminance(color) : Luminance(color);
}

float3 ColorGradePostExposure(float3 color)
{
	color = LinearToLogC(color);
	color = (color - ACEScc_MIDGRAY) * _ColorAdjustments.x + ACEScc_MIDGRAY;
	color = LogCToLinear(color);
	return color;
}

float3 ColorGradeContrast(float3 color, bool useACES)
{
	color = useACES ? ACES_to_ACEScc(unity_to_ACES(color)) : LinearToLogC(color);
	color = (color - ACEScc_MIDGRAY) * _ColorAdjustments.y + ACEScc_MIDGRAY;
	return useACES ? ACES_to_ACEScg(ACEScc_to_ACES(color)) : LogCToLinear(color);
}

float3 ColorGradeColorFilter(float3 color)
{
	return color * _ColorFilter.rgb;
}

float3 ColorGradeHueShift(float3 color)
{
	color = RgbToHsv(color);
	float Hue = color.x + _ColorAdjustments.z;
	color.x = RotateHue(Hue, 0.0, 1.0);
	return HsvToRgb(color);
}

float3 ColorGradeSaturation(float3 color, bool useACES)
{
	float luminance = Luminance(color, useACES);
	color = (color - luminance) * _ColorAdjustments.w + luminance;
	return color;
}

float3 ColorGradeWhiteBalance(float3 color)
{
	color = LinearToLMS(color);
	color *= _WhiteBalance.rgb;
	color = LMSToLinear(color);
	return color;
}

float3 ColorGradeSplitTone(float3 color, bool useACES)
{
	color = PositivePow(color, 1.0/2.2);
	float t = saturate(Luminance(saturate(color), useACES) + _SplitToningShadows.w);
	float3 shadows = lerp(0.5, _SplitToningShadows.rgb, 1.0 - t);
	float3 highlights = lerp(0.5, _SplitToningHightlights.rgb, t);
	color = SoftLight(color, shadows);
	color = SoftLight(color, highlights);
	color = PositivePow(color, 2.2);
	return color;
}

float3 ColorGradeChannelMixer(float3 color)
{
	return mul(
		float3x3(_ChannelMixerRed.rgb, _ChannelMixerGreen.rgb, _ChannelMixerBlue.rgb),
		color
	);
}

float3 ColorGradingShadowsMidtonesHighlights (float3 color, bool useACES) 
{
	float luminance = Luminance(color, useACES);
	float shadowsWeight = 1.0 - smoothstep(_SMHRange.x, _SMHRange.y, luminance);
	float highlightsWeight = smoothstep(_SMHRange.z, _SMHRange.w, luminance);
	float midtonesWeight = 1.0 - shadowsWeight - highlightsWeight;
	return
		color * _SMHShadows.rgb * shadowsWeight +
		color * _SMHMidtones.rgb * midtonesWeight +
		color * _SMHHighlights.rgb * highlightsWeight;
}




//do color grading before tone mapping
float3 ColorGrading(float3 color, bool useACES = false)
{
	//color = min(color, 60);
	color = ColorGradePostExposure(color);
	color = ColorGradeWhiteBalance(color);

	color = ColorGradeContrast(color, useACES);
	color = ColorGradeColorFilter(color);
	color = max(color, 0.0f);
	color = ColorGradeSplitTone(color,useACES);
	color = ColorGradeChannelMixer(color);
	color = max(color, 0.0f);
	color = ColorGradingShadowsMidtonesHighlights(color,useACES);
	color = ColorGradeHueShift(color);
	color = ColorGradeSaturation(color,useACES);
	return max(useACES ? ACEScg_to_ACES(color) : color, 0.0);
}

float3 GetColorGradedLUT(float2 uv, bool useACES = false)
{
	//Get the LUT Original Color (0~1)
	float3 color = GetLutStripValue(uv, _ColorGradingLUTParameters);
	//interprete the Original Color as in LogC that will extend it to 0~59, match the HDR
	return ColorGrading(_ColorGradingLUTInLogC ? LogCToLinear(color) : color , useACES);
}

//Tone mapping from HDR to LDR
float4 ColorGradingNonePassFragment(Varyings input) : SV_TARGET
{
	//isteand of doing this for all pixel, we use a LUT
	//float4 color = GetSource(input.screenUV);
	//color.rgb = ColorGrading(color.rgb);
	float3 color = GetColorGradedLUT(input.screenUV, true);
	return float4(color,1.0f);
}

float4 ColorGradingReinhardPassFragment(Varyings input) : SV_TARGET
{
	//float4 color = GetSource(input.screenUV);
	//color.rgb = ColorGrading(color.rgb);
	float3 color = GetColorGradedLUT(input.screenUV, true);
	color.rgb /= color.rgb + 1.0;
	return float4(color,1.0f);
}

float4 ColorGradingNeutralPassFragment(Varyings input) : SV_TARGET
{
	//float4 color = GetSource(input.screenUV);
	//color.rgb = ColorGrading(color.rgb);
	float3 color = GetColorGradedLUT(input.screenUV, true);
	color.rgb = NeutralTonemap(color.rgb);
	return float4(color,1.0f);
}

float4 ColorGradingACESPassFragment(Varyings input) : SV_TARGET
{
	//float4 color = GetSource(input.screenUV);
	//use color grading in ACES color space
	//color.rgb = ColorGrading(color.rgb, true);
	float3 color = GetColorGradedLUT(input.screenUV, true);
	color.rgb = AcesTonemap(color.rgb);
	return float4(color,1.0f);
}

float3 ApplyColorGradingLUT(float3 color)
{
	color = ApplyLut2D(
		TEXTURE2D_ARGS(_ColorGradingLUT, sampler_linear_clamp),
		saturate(_ColorGradingLUTInLogC ? LinearToLogC(color) : color),
		_ColorGradingLUTParameters.xyz
	);
	return color;
}

float4 ApplyColorgradingPassFragment(Varyings input) : SV_TARGET
{
	float4 color = GetSource(input.screenUV);
	color.rgb = ApplyColorGradingLUT(color.rgb);
	return color;
}

float4 ApplyColorgradingWithLumaPassFragment(Varyings input) : SV_TARGET
{
	float4 color = GetSource(input.screenUV);
	color.rgb = ApplyColorGradingLUT(color.rgb);
	color.a = sqrt(Luminance(color.rgb));
	return color;
}

bool _CopyBicubic;

float4 FinalPassFragmentRescale (Varyings input) : SV_TARGET 
{
	if (_CopyBicubic) {
		return GetSourceBicubic(input.screenUV);
	}
	else {
		return GetSource(input.screenUV);
	}
}

#endif