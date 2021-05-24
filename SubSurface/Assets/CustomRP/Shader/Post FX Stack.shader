Shader "Hidden/Custom_RP/Post FX Stack"
{
    SubShader
    {
        Cull off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "../ShaderLib/Common.hlsl"
        #include "PostFXStackPasses.hlsl"
        ENDHLSL

        Pass
        {
            Name "Copy"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment CopyPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Bloom Horizontal"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomHorizontalPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Bloom Vertical"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomVerticalPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Bloom Add"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomAddPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Bloom Prefilter"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomPrefilterPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Bloom Prefilter FireFlies"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomPrefilterFireFliesPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Bloom Scatter"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomScatterPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Bloom Scatter Final"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomScatterFinalPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "ColorGrading None"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ColorGradingNonePassFragment
            ENDHLSL
        }
        Pass
        {
            Name "ColorGrading ACES"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ColorGradingACESPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "ColorGrading Neutral"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ColorGradingNeutralPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "ColorGrading Reinhard"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ColorGradingReinhardPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Apply ColorGrading"

            //make PostFX produce final image with AlphaBlend
            //Can be used for MultiCam blend,support percamera blend mode
            //BLend One OneMinusSrcAlpha
            Blend [_FinalSrcBlend] [_FinalDstBlend]

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ApplyColorgradingPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Apply ColorGrading with Luma"

            Blend[_FinalSrcBlend][_FinalDstBlend]

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ApplyColorgradingWithLumaPassFragment
            ENDHLSL
        }
        Pass {
			Name "Final Rescale"

			Blend [_FinalSrcBlend] [_FinalDstBlend]
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment FinalPassFragmentRescale
			ENDHLSL
		}
        Pass{
            Name "FXAA"

            Blend[_FinalSrcBlend][_FinalDstBlend]

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment FXAAPassFragment
                #pragma multi_compile _ FXAA_QUALITY_MEDIUM FXAA_QUALITY_LOW
                #include "FXAAPass.hlsl"
            ENDHLSL
        }
        Pass{
            Name "FXAA With Luma"

            Blend[_FinalSrcBlend][_FinalDstBlend]

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment FXAAPassFragment
                #pragma multi_compile _ FXAA_QUALITY_MEDIUM FXAA_QUALITY_LOW
                //#define FXAA_QUALITY_LOW
                #define FXAA_ALPHA_CONTAINS_LUMA
                #include "FXAAPass.hlsl"
            ENDHLSL
        }
        
    }
}
