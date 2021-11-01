using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class CameraSettings
{
    [Serializable]
    public struct FinalBlendMode
    {
        public BlendMode source, destination;
    }

    [RenderingLayerMaskField]//this will enable a custom drawer for this attribute
    public int RenderingLayerMask = -1;

    //optional mask light per camera
    public bool maskLights = false;

    public bool overridePostFX = false;
    public PostFXSettings postFXSettings = default;

    public FinalBlendMode finalBlendMode = new FinalBlendMode
    {
        source = BlendMode.One,
        destination = BlendMode.Zero
    };

    public bool copyColor = true, copyDepth = true;
    public enum RenderScaleMode { Inherit, Multiply, Override }

    public RenderScaleMode renderScaleMode = RenderScaleMode.Inherit;

    [Range(0.1f, 2f)]
    public float renderScale = 1f;

    public bool allowFXAA = false;
    public bool keepAlpha = false;

    public float GetRenderScale (float scale) {
		return
			renderScaleMode == RenderScaleMode.Inherit ? scale :
			renderScaleMode == RenderScaleMode.Override ? renderScale :
			scale * renderScale;
	}
}
