Shader "Sky/SkyBox"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {

        HLSLINCLUDE
        #include "../../CustomRP/ShaderLib/Common.hlsl"
        #include "SkyboxInput.hlsl"
        ENDHLSL
        Tags { "RenderType" = "Opaque" }

        Pass
        {

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex SkyboxVertex
            #pragma fragment SkyboxFragment
            #include "./SkyboxPass.hlsl"
            ENDHLSL
        }
    }
}
