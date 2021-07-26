Shader "Custom_RP/OceanShader"
{
    Properties
    {
        //_BaseColor("Color", Color) = (0.5,0.5,0.5,1.0)
        //_OceanScale("OceanScale", Float) = 1
        _GridSize("GridSize", Float) = 1
        _TransitionParam("TransitionParams", Vector) = (1.0,1.0,1.0,1.0)
        //_CenterPos("LODCenterPosition", Vector) = (0.0,0.0,0.0,0.0)
        _LODSize("GridSize", Float) = 1
        _LODIndex("LODIndex", Float) = 0

        //_DispTex("LODDispTexture", 2D) = "white" {}
        //_NextDispTex("NextLODDispTexture", 2D) = "white" {}

        //_NormalTex("LODNTexture", 2D) = "white" {}
        //_NextLODNTex("NextLODNTexture", 2D) = "white" {}

        _DetailNormalNoise("FFTDetailNormalTex", 2D) = "white" {}

        //_DispTexArray("DisplaceTextureArray", 2D)
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows("ReceiveShadows", Float) = 1
    }
    SubShader
    {
        HLSLINCLUDE
        #include "../../CustomRP/ShaderLib/Common.hlsl"
        #include "OceanInput.hlsl"
        ENDHLSL
        Tags{"Queue" = "Transparent+250"}

        Pass
        {
            Tags
            {
                //"LightMode" = "CustomLit"//indicate we are using custom lighting model
                "LightMode" = "OceanShading"
            }
            
            //for Alpha blend type We will use One OneMinusSrcAlpha
            //Blend SrcAlpha OneMinusSrcAlpha
            Blend One Zero
            Cull Back
            ZWrite Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature _PREMULTIPLY_ALPHA

            //make unity complie 2 shader with and without GPU instancing
            #pragma multi_compile_instancing

            #pragma shader_feature _RECEIVE_SHADOWS

            //per object lights
            #pragma multi_compile _ _LIGHTS_PER_OBJECT

            #pragma vertex OceanPassVertex
            #pragma fragment OceanPassFragment
            #include "./OceanPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags
            {
            //make the back face of water surface not transparent
            //only direct light and Indirect light
            "LightMode" = "OceanShadingBack"
        }

            //for Alpha blend type We will use One OneMinusSrcAlpha
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature _PREMULTIPLY_ALPHA

            //make unity complie 2 shader with and without GPU instancing
            #pragma multi_compile_instancing

            //per object lights
            #pragma multi_compile _ _LIGHTS_PER_OBJECT

            #pragma vertex OceanPassVertex
            #pragma fragment OceanBackPassFragment
            #include "./OceanPass.hlsl"
            ENDHLSL
        }

        Pass//currently this pass
        {
            Tags
            {
                "LightMode" = "OceanDepthShadingFront"//indicate we are using custom lighting model
                //"LightMode" = "OceanShading"
            }

            //for Alpha blend type We will use One OneMinusSrcAlpha
            Blend One Zero
            ZWrite On
            Cull Back
            ZTest Less//the front and back surface are to close, culling may fail
            //Cull Back
            HLSLPROGRAM
            #pragma target 4.0
            #pragma shader_feature _PREMULTIPLY_ALPHA

            //make unity complie 2 shader with and without GPU instancing
            #pragma multi_compile_instancing



            //per object lights
            #pragma multi_compile _ _LIGHTS_PER_OBJECT

            #pragma vertex OceanDepthPassVertexFront
            //#pragma geometry OceanDepthPassGeometry
            #pragma fragment OceanDepthPassFragmentFront
            #include "./OceanPass.hlsl"
            ENDHLSL
        }

        Pass//currently this pass
        {
            Tags
            {
                "LightMode" = "OceanDepthShadingBack"//indicate we are using custom lighting model
                //"LightMode" = "OceanShading"
            }

            //for Alpha blend type We will use One OneMinusSrcAlpha
            Blend One Zero
            ZWrite On
            Cull Front
            ZTest Less//the front and back surface are to close, culling may fail
            //Cull Back
            HLSLPROGRAM
            #pragma target 4.0
            #pragma shader_feature _PREMULTIPLY_ALPHA

            //make unity complie 2 shader with and without GPU instancing
            #pragma multi_compile_instancing



            //per object lights
            #pragma multi_compile _ _LIGHTS_PER_OBJECT

            #pragma vertex OceanDepthPassVertexBack
            //#pragma geometry OceanDepthPassGeometry
            #pragma fragment OceanDepthPassFragmentBack
            #include "./OceanPass.hlsl"
            ENDHLSL
    }
    }
}
