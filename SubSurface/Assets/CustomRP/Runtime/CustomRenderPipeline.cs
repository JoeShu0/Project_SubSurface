﻿using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{
    CameraRenderer renderer;// = new CameraRenderer(shader);
    CameraBufferSettings cameraBufferSettings;
    bool useDynameicBatching, useGPUInstancing ,useLightPerObject;

    ShadowSettings shadowsetting;

    PostFXSettings postFXSettings;
    int colorLUTResolution;

    OceanRenderSetting oceanRenderSetting;


    public CustomRenderPipeline(
        CameraBufferSettings cameraBufferSettings,
        bool useDynameicBatching, 
        bool useGPUInstancing, 
        bool useSRPBatcher,
        bool useLightPerObject,
        ShadowSettings shadowsetting,
        PostFXSettings postFXSettings,
        int colorLUTResolution,
        Shader cameraRendererShader,
        OceanRenderSetting oceanRenderSetting)
    {
        this.useDynameicBatching = useDynameicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        this.shadowsetting = shadowsetting;
        this.useLightPerObject = useLightPerObject;
        this.postFXSettings = postFXSettings;
        this.cameraBufferSettings = cameraBufferSettings;
        this.colorLUTResolution = colorLUTResolution;
        this.oceanRenderSetting = oceanRenderSetting;
        renderer = new CameraRenderer(cameraRendererShader);
        //
        InitializeForEditor();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera camera in cameras)
        {
            renderer.Render(context, camera, cameraBufferSettings,
                useDynameicBatching, useGPUInstancing, 
                useLightPerObject, shadowsetting,
                postFXSettings, colorLUTResolution, oceanRenderSetting);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DisposeForEditor();
        renderer.Dispose();
    }
}