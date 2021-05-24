using System;
using UnityEngine;


[CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]
public class PostFXSettings : ScriptableObject
{
    [SerializeField]
    Shader shader = default;

    [System.NonSerialized]
    Material material;

    [System.Serializable]
    public struct BloomSettings
    {
        public bool ignoreRenderScale;
        [Range(0f, 16f)]
        public int maxIterations;
        [Min(1f)]
        public int downscaleLimit;
        public bool bicubicUpsampling;
        [Min(0f)]
        public float threshold;
        [Range(0f, 1f)]
        public float thresholdKnee;
        [Min(0f)]
        public float intensity;
        public bool Fade_FireFlies;
        public enum Mode { Additive, Scattering };
        public Mode mode;
        [Range(0.05f, 0.95f)]
        public float scatter;
    };

    [System.Serializable]
    public struct ToneMappingSettings
    {
        public enum Mode { 
            None,
            ACES,
            Netural,
            Reinhard
        };
        public Mode mode;
    }

    [Serializable]
    public struct ColorAdjustmentsSettings 
    {
        public float postExposure;

        [Range(-100f, 100f)]
        public float contrast;

        [ColorUsage(false, true)]
        public Color colorFilter;

        [Range(-180f, 180f)]
        public float hueShift;

        [Range(-100f, 100f)]
        public float saturation;
    }
    [Serializable]
    public struct WhiteBalanceSettings
    {
        [Range(-100f, 100f)]
        public float temperature, tint;
    }
    [Serializable]
    public struct SplitToneSettings
    {
        [ColorUsage(false)]
        public Color shadows, highlights;
        [Range(-100f, 100f)]
        public float balance;
    }
    [Serializable]
    public struct ChannelMixerSettings
    {
        public Vector3 red, green, blue;
    }
    [Serializable]
    public struct ShadowsMidtonesHighlightsSettings
    {
        [ColorUsage(false, true)]
        public Color shadows, midtones, highlights;
        [Range(0f, 2f)]
        public float shadowsStart, shadowsEnd, highlightsStart, highLightsEnd;
    }


    //*****************************************
    [SerializeField]
    BloomSettings bloom = new BloomSettings 
    {   scatter = 0.7f };

    [SerializeField]
    ColorAdjustmentsSettings colorAdjustments = new ColorAdjustmentsSettings 
    {   colorFilter = Color.white };
    [SerializeField]
    WhiteBalanceSettings whiteBalance = default;
    [SerializeField]
    SplitToneSettings splitTone = new SplitToneSettings
    {   shadows = Color.gray, highlights = Color.gray };
    [SerializeField]
    ChannelMixerSettings channelMixer = new ChannelMixerSettings
    {   red = Vector3.right, green = Vector3.up, blue = Vector3.forward};
    [SerializeField]
    ShadowsMidtonesHighlightsSettings
        shadowsMidtonesHighlights = new ShadowsMidtonesHighlightsSettings
        {
            shadows = Color.white,
            midtones = Color.white,
            highlights = Color.white,
            shadowsEnd = 0.3f,
            highlightsStart = 0.55f,
            highLightsEnd = 1f
        };

    [SerializeField]
    ToneMappingSettings toneMapping = default;
    
    

    public BloomSettings Bloom => bloom;
    public ColorAdjustmentsSettings ColorAdjustments => colorAdjustments;
    public WhiteBalanceSettings WhiteBalance => whiteBalance;
    public SplitToneSettings SplitTone => splitTone;
    public ChannelMixerSettings ChannelMixer => channelMixer;
    public ShadowsMidtonesHighlightsSettings ShadowsMidtonesHighlights =>
        shadowsMidtonesHighlights;
    public ToneMappingSettings ToneMapping => toneMapping;
    

    //****************************************
    public Material Material
    {
        get {
            if (material == null && shader != null)
            {
                material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }
            return material;
        }
    }
}
