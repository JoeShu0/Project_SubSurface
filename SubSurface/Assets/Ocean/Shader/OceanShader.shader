﻿Shader "Custom_RP/OceanShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        HLSLINCLUDE
        #include "../../CustomRP/ShaderLib/Common.hlsl"
        //#include "LitInput.hlsl"
        ENDHLSL

        Pass
        {
            Tags
            {
                "LightMode" = "CustomLit"//indicate we are using custom lighting model
            }
            
            //for Alpha blend type We will use One OneMinusSrcAlpha
            Blend One Zero
            ZWrite Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature _PREMULTIPLY_ALPHA

            //make unity complie 2 shader with and without GPU instancing
            #pragma multi_compile_instancing



            //per object lights
            #pragma multi_compile _ _LIGHTS_PER_OBJECT

            #pragma vertex OceanPassVertex
            #pragma fragment OceanPassFragment
            #include "./OceanPass.hlsl"
            ENDHLSL
        }
    }
}
