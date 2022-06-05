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
        Tags{"Queue" = "Geometry+1"}

        Pass
        {
            
            Tags
            {
                "LightMode" = "CustomSkyBox"
            }

            Blend One Zero
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex SkyboxVertex
            #pragma fragment SkyboxFragment
            #include "./SkyboxPass.hlsl"
            ENDHLSL
        }
    }
}
