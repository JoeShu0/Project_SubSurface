Shader "Custom_RP/Unlit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "White"{}
        [HDR] _BaseColor("Color", Color) = (1.0,1.0,0.0,1.0)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0 //this is a toggle, on shader will have "_Clipping", defined
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
        [KeywordEnum(On, Clip, Dither, Off)] _Shadows("Shadows", Float) = 0
    }
    SubShader
    {
        //since some pass uses same functions and input declares, 
        //We pack then into one Litinput.hlsl
        HLSLINCLUDE
        #include "../ShaderLib/Common.hlsl"
        #include "UnlitInput.hlsl"
        ENDHLSL
            
        Pass
        {
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma shader_feature _CLIPPING//make unity complie 2 shader with and without _CLIPPING define
            #pragma multi_compile_instancing//make unity complie 2 shader with and without GPU instancing
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #include "UnlitPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                 "LightMode" = "ShadowCaster"//add a pass, only shader with this pass is drawn in shadow buffer
            }

            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            //#pragma shader_feature _CLIPPING//make unity complie 2 shader with and without _CLIPPING define
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            #pragma multi_compile_instancing//make unity complie 2 shader with and without GPU instancing
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #include "ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "Meta"//this pass is used for GI caculation
            }

            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment
            #include "MetaPass.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "CustomShaderGUI"
}
