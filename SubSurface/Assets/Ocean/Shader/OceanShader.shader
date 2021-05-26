Shader "Custom_RP/OceanShader"
{
    Properties
    {
        _BaseColor("Color", Color) = (0.5,0.5,0.5,1.0)
        _GridSize("GridSize", Float) = 1
        //_TransitionParam("GridSize", ) = 1
    }
    SubShader
    {
        HLSLINCLUDE
        #include "../../CustomRP/ShaderLib/Common.hlsl"
        #include "OceanInput.hlsl"
        ENDHLSL
        Tags{"Queue" = "Transparent-250"}

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
