using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public partial class CustomRenderPipelineAsset : RenderPipelineAsset 
{
    [SerializeField]
    bool useDynameicBatching = true, 
        useGPUInstancing = true, 
        useSRPBatcher = true,
        useLightPerObject = true;

    [SerializeField]
    ShadowSettings shadows = default;

    [SerializeField]
    //bool allowHDR = true;
    CameraBufferSettings cameraBuffer = new CameraBufferSettings
    {
        allowHDR = true,
        renderScale = 1f,
        fxaa = new CameraBufferSettings.FXAA { 
        fixedThreshold = 0.0833f,
        relativeThreshold = 0.166f,
        subpixelBlending = 0.75f}
    };

    [SerializeField]
    PostFXSettings postFXSettings = default;

    public enum ColorLUTResolution { _16 = 16, _32 = 32, _64 = 64}
    [SerializeField]
    ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;
    [SerializeField]
    Shader cameraRendererShader = default;

    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(cameraBuffer,
            useDynameicBatching, useGPUInstancing, 
            useSRPBatcher, useLightPerObject, 
            shadows, postFXSettings, (int)colorLUTResolution,
            cameraRendererShader);
        //throw new System.NotImplementedException();
    }
}