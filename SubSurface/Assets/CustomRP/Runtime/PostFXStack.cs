using UnityEngine;
using UnityEngine.Rendering;
//It makes all constant, static, and type members of a class or struct directly accessible 
//without fully qualifying them.
using static PostFXSettings;

public partial class PostFXStack
{
    const string buffername = "PostFX";

    CommandBuffer buffer = new CommandBuffer
    {
        name = buffername
    };

    ScriptableRenderContext context;
    Camera camera;
    PostFXSettings settings;
    const int maxBloomPyramidLevel = 16;
    int bloomPyramidId;

    bool useHDR;

    bool keepAlpha;

    int colorLUTResolution;
    public bool IsActive => settings != null;

    CameraSettings.FinalBlendMode finalBlendMode;

    Vector2Int bufferSize;

    CameraBufferSettings.BicubicRescalingMode bicubicRescaling;

    CameraBufferSettings.FXAA fxaa;
    enum Pass 
    {
        Copy,
        BloomHorizontal,
        BloomVertical,
        BloomAdd,
        BloomPrefilter,
        BloomPrefilterFireFlies,
        BloomScatter,
        BloomScatterFinal,
        ColorGradingNone,
        ColorGradingACES,
        ColorGradingNeutral,
        ColorGradingReinhard,
        ApplyColorGrading,
        ApplyColorGradingwithLuma,
        FinalRescale,
        FXAA,
        FXAAWithLuma
    }

    int fxSourceId = Shader.PropertyToID("_PostFXSource");
    int fxSource2Id = Shader.PropertyToID("_PostFXSource2");
    int bloomBicubicUpsamplingId = Shader.PropertyToID("_BloomBocubicUpsampling");
    int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
    int bloomThreshold = Shader.PropertyToID("_BloomThreshold");
    int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    int bloomResultId = Shader.PropertyToID("_BloomResult");

    int colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments");
    int colorFilterId = Shader.PropertyToID("_ColorFilter");
    int whiteBalanceId = Shader.PropertyToID("_WhiteBalance");
    int SplitToningShadowsId = Shader.PropertyToID("_SplitToningShadows");
    int SplitToningHightlightsId = Shader.PropertyToID("_SplitToningHightlights");
    int channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed");
    int channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen");
    int channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue");

    int smhShadowsId = Shader.PropertyToID("_SMHShadows");
    int smhMidtonesId = Shader.PropertyToID("_SMHMidtones");
    int smhHighlightsId = Shader.PropertyToID("_SMHHighlights");
    int smhRangeId = Shader.PropertyToID("_SMHRange");

    int colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT");
    int colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters");
    int colorGradingLUTInLogId = Shader.PropertyToID("_ColorGradingLUTInLogC");

    int finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend");
    int finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");
    
    int copyBicubicId = Shader.PropertyToID("_CopyBicubic");

    int colorGradingResultId = Shader.PropertyToID("_ColorGradingResult");
    int finalResultId = Shader.PropertyToID("_FinalResult");

    int fxaaConfigId = Shader.PropertyToID("_FXAAConfig");

    const string
        fxaaQaulityLowKeyword = "FXAA_QUALITY_LOW",
        fxaaQaulityMediumKeyword = "FXAA_QUALITY_MEDIUM";

    public void Setup
        (ScriptableRenderContext context,
        Camera camera, Vector2Int bufferSize,
        PostFXSettings settings, bool keepAlpha,
        bool useHDR,  int colorLUTResolution,
        CameraSettings.FinalBlendMode finalBlendMode,
        CameraBufferSettings.BicubicRescalingMode bicubicRescaling,
        CameraBufferSettings.FXAA fxaa)
    {
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        this.useHDR = useHDR;
        this.colorLUTResolution = colorLUTResolution;
        this.finalBlendMode = finalBlendMode;
        this.bufferSize = bufferSize;
        this.bicubicRescaling = bicubicRescaling;
        this.fxaa = fxaa;
        this.keepAlpha = keepAlpha;
        ApplySceneViewState();
        //Debug.Log(fxaa.enabled);
    }

    public void Render(int sourceId)
    {
        //buffer.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        //Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        if (DoBloom(sourceId))
        {
            DoFinal(bloomResultId);
            buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            DoFinal(sourceId);
        }
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 1; i < maxBloomPyramidLevel * 2; i++)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    void Draw(RenderTargetIdentifier from,
        RenderTargetIdentifier to,
        Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(to,
            RenderBufferLoadAction.DontCare,
            RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity,
            settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }
    
    //A dup of draw to set viewport, this is only used for final imge drawing
    //will handle Multicamera blend
    void DrawFinal(RenderTargetIdentifier from, Pass pass)
    {
        //percamera blend mode
        buffer.SetGlobalFloat(finalSrcBlendId, (float)finalBlendMode.source);
        buffer.SetGlobalFloat(finalDstBlendId, (float)finalBlendMode.destination);
        
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,
            // load the previous camer render if MultiCam blend
            finalBlendMode.destination != BlendMode.Zero ? RenderBufferLoadAction.Load : RenderBufferLoadAction.DontCare,
            RenderBufferStoreAction.Store);
        //set the render viewpost to  camera rect
        buffer.SetViewport(camera.pixelRect);
        buffer.DrawProcedural(Matrix4x4.identity,
            settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    bool DoBloom(int SourceId)
    {
        PostFXSettings.BloomSettings bloomSettings = settings.Bloom;

        //if the blomm should based on scaled render buffer size
        int width, height;
        if (bloomSettings.ignoreRenderScale)
        {
            width = camera.pixelWidth / 2;
            height = camera.pixelHeight / 2;
        }
        else
        {
            width = bufferSize.x / 2;
            height = bufferSize.y / 2;
        }

        if (bloomSettings.maxIterations == 0 || bloomSettings.intensity <= 0f ||
            height < bloomSettings.downscaleLimit || width < bloomSettings.downscaleLimit)
        {
            //Draw(SourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            //buffer.EndSample("Bloom");
            return false;
        }

        buffer.BeginSample("Bloom");
        //compute the constant part of bloom threshold knee
        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloomSettings.threshold);
        threshold.y = threshold.x * bloomSettings.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThreshold, threshold);
        //unform format for easier switch HDR
        RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        //We start bloom at half resolution
        buffer.GetTemporaryRT(bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
        Draw(SourceId, bloomPrefilterId, 
            settings.Bloom.Fade_FireFlies? Pass.BloomPrefilterFireFlies : Pass.BloomPrefilter);
        width /= 2;
        height /= 2;
        //We are using 2 pass per bloom level
        int fromId = bloomPrefilterId, toId = bloomPyramidId + 1;

        int i;
        for (i = 0; i < bloomSettings.maxIterations; i++)
        {
            if (width < bloomSettings.downscaleLimit * 2 || height < bloomSettings.downscaleLimit * 2)
            {
                break;
            }
            int midId = toId - 1;
            buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);

            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);

            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }

        buffer.ReleaseTemporaryRT(bloomPrefilterId);
        //Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        //release the mid for last level
        buffer.ReleaseTemporaryRT(fromId - 1);
        //set toId to the mid of one level higher
        toId -= 5;

        // combine bloom
        Pass combinePass, finalPass;
        float finalIntensity;
        if (bloomSettings.mode == PostFXSettings.BloomSettings.Mode.Additive)
        {
            combinePass = finalPass = Pass.BloomAdd;
            buffer.SetGlobalFloat(bloomIntensityId, 1f);
            finalIntensity = bloomSettings.intensity;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomScatterFinal;
            buffer.SetGlobalFloat(bloomIntensityId, bloomSettings.intensity);
            finalIntensity = Mathf.Min(bloomSettings.intensity, 0.95f);
        }

        buffer.SetGlobalFloat(bloomBicubicUpsamplingId, bloomSettings.bicubicUpsampling ? 1f : 0f);
        
        if (i > 1)
        {
            //stop at the 1st level, special case for level before last level
            //just in-order to reuse RTs
            for (i -= 1; i > 0; i--)
            {
                buffer.SetGlobalTexture(fxSource2Id, toId + 1);
                Draw(fromId, toId, combinePass);
                buffer.ReleaseTemporaryRT(fromId);
                buffer.ReleaseTemporaryRT(toId + 1);
                fromId = toId;
                toId -= 2;
            }
        }
        else 
        {
            buffer.ReleaseTemporaryRT(bloomPyramidId);
        }

        buffer.SetGlobalFloat(bloomIntensityId, finalIntensity);
        buffer.SetGlobalTexture(fxSource2Id, SourceId);
        buffer.GetTemporaryRT(bloomResultId, bufferSize.x, bufferSize.y, 0,
            FilterMode.Bilinear, format);
        //Draw the bloom result on a RT for tone mapping
        Draw(fromId, bloomResultId, finalPass);
        buffer.ReleaseTemporaryRT(fromId);

        buffer.EndSample("Bloom");
        return true;
    }

    void ConfigureColorAdjustments()
    {
        ColorAdjustmentsSettings colorAdjustments = settings.ColorAdjustments;
        Vector4 ColorAdjustmentsV = new Vector4(
            Mathf.Pow(2f, colorAdjustments.postExposure),
            colorAdjustments.contrast * 0.01f + 1f,
            colorAdjustments.hueShift * (1f / 360f),
            colorAdjustments.saturation * 0.01f + 1f
        );
        buffer.SetGlobalVector(colorAdjustmentsId, ColorAdjustmentsV);
        buffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter);
    }

    void ConfigureWhiteBalance()
    {
        WhiteBalanceSettings whiteBalance = settings.WhiteBalance;
        buffer.SetGlobalVector(whiteBalanceId,
            ColorUtils.ColorBalanceToLMSCoeffs(whiteBalance.temperature, whiteBalance.tint));
    }

    void ConfigureSplitToning()
    {
        SplitToneSettings splitTone = settings.SplitTone;
        Color splitColor = splitTone.shadows;
        splitColor.a = splitTone.balance * 0.01f;
        buffer.SetGlobalColor(SplitToningShadowsId, splitColor);
        buffer.SetGlobalColor(SplitToningHightlightsId, splitTone.highlights);
    }

    void configureChannelMixer()
    {
        ChannelMixerSettings channelMixer = settings.ChannelMixer;
        buffer.SetGlobalVector(channelMixerRedId, channelMixer.red);
        buffer.SetGlobalVector(channelMixerGreenId, channelMixer.green);
        buffer.SetGlobalVector(channelMixerBlueId, channelMixer.blue);
    }
    void ConfigureShadowsMidtonesHighlights()
    {
        ShadowsMidtonesHighlightsSettings smh = settings.ShadowsMidtonesHighlights;
        buffer.SetGlobalColor(smhShadowsId, smh.shadows.linear);
        buffer.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
        buffer.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
        buffer.SetGlobalVector(smhRangeId, new Vector4(
            smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highLightsEnd
        ));
    }

    void ConfigureFXAA()
    {
        buffer.SetGlobalVector(fxaaConfigId, new Vector4(
                fxaa.fixedThreshold, fxaa.relativeThreshold, fxaa.subpixelBlending, 0.0f));

        if (fxaa.quailty == CameraBufferSettings.FXAA.Quailty.Low)
        {
            //Shader.EnableKeyword(fxaaQaulityLowKeyword);
            //Shader.DisableKeyword(fxaaQaulityMediumKeyword);
            buffer.EnableShaderKeyword(fxaaQaulityLowKeyword);
            buffer.DisableShaderKeyword(fxaaQaulityMediumKeyword);
            //Debug.Log(fxaa.quailty);
        }
        else if (fxaa.quailty == CameraBufferSettings.FXAA.Quailty.Medium)
        {
            buffer.DisableShaderKeyword(fxaaQaulityLowKeyword);
            buffer.EnableShaderKeyword(fxaaQaulityMediumKeyword);
        }
        else 
        {
            buffer.DisableShaderKeyword(fxaaQaulityLowKeyword);
            buffer.DisableShaderKeyword(fxaaQaulityMediumKeyword);
        }

        
    }

    void DoFinal(int sourceId)
    {
        ConfigureColorAdjustments();
        ConfigureWhiteBalance();
        ConfigureSplitToning();
        configureChannelMixer();
        ConfigureShadowsMidtonesHighlights();

        //Get the rt for LUT
        int lutHeight = colorLUTResolution;
        int lutWidth = lutHeight * lutHeight;
        buffer.GetTemporaryRT(colorGradingLUTId, lutWidth, lutHeight, 0,
            FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(
            lutHeight, 0.5f/lutWidth, 0.5f/lutHeight, lutHeight/(lutHeight-1f)   
        ));

        PostFXSettings.ToneMappingSettings.Mode mode = settings.ToneMapping.mode;
        Pass pass = Pass.ColorGradingNone + (int)mode;
        buffer.SetGlobalFloat(
            colorGradingLUTInLogId, useHDR && pass!= Pass.ColorGradingNone ? 1f:0f
        );
        //Debug.Log(useHDR);
        //draw color grading to LUT
        Draw(sourceId, colorGradingLUTId, pass);

        buffer.SetGlobalVector(colorGradingLUTParametersId,
            new Vector4(1f/ lutWidth, 1f/ lutHeight, lutHeight -1f)
        );

        //Set Final blend mode to zero one, for multi cam blending
        buffer.SetGlobalFloat(finalSrcBlendId, 1f);
        buffer.SetGlobalFloat(finalDstBlendId, 0f);
        //check if we are using FXAA, if so add one more layer buffer
        if (fxaa.enabled)
        {
            ConfigureFXAA();
            
            buffer.GetTemporaryRT(
                colorGradingResultId, bufferSize.x, bufferSize.y, 0,
                FilterMode.Bilinear, RenderTextureFormat.Default);
            Draw(sourceId, colorGradingResultId, 
                keepAlpha ? Pass.ApplyColorGrading : Pass.ApplyColorGradingwithLuma);
        }

        //Check if we are using scaled rendering
        if (bufferSize.x == camera.pixelWidth)
        {
            if (fxaa.enabled)
            {
                DrawFinal(colorGradingResultId,
                    keepAlpha ? Pass.FXAA : Pass.FXAAWithLuma);
                buffer.ReleaseTemporaryRT(colorGradingResultId);
            }
            else 
            {
                DrawFinal(sourceId, Pass.ApplyColorGrading);
            }
                
        }
        else 
        {
            
            buffer.GetTemporaryRT(
                finalResultId, bufferSize.x, bufferSize.y, 0,
                FilterMode.Bilinear, RenderTextureFormat.Default
            );
            if (fxaa.enabled)
            {
                Draw(colorGradingResultId, finalResultId,
                    keepAlpha ? Pass.FXAA : Pass.FXAAWithLuma);
                buffer.ReleaseTemporaryRT(colorGradingResultId);
            }
            else
            {
                Draw(sourceId, finalResultId, Pass.ApplyColorGrading);
            }
                
            //addition scale draw
            bool bicubicSampling =
                bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpOnly &&
                bufferSize.x < camera.pixelWidth;
            buffer.SetGlobalFloat(copyBicubicId, bicubicSampling ? 1f : 0f);
            DrawFinal(finalResultId, Pass.FinalRescale);
            buffer.ReleaseTemporaryRT(finalResultId);
        }
        buffer.ReleaseTemporaryRT(colorGradingLUTId);
    }
}
