using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Custom Ocean Shading Setting")]
public class OceanShadingSetting : ScriptableObject
{
    [System.Serializable]
    public struct Highlights
    {
        [Range(0f, 150f)]
        public float HighLightExp;
        [Range(0f, 10f)]
        public float HighLightBost;
    };

    [System.Serializable]
    public struct ColorBanding
    {
        [ColorUsage(true, true)]
        public Color color;
        public float bandingOffset;
        public float bandingPower;
        public float MultiPlier;
    }

    //Material Properties
    
    public ColorBanding Bright;
    public ColorBanding Base;
    public ColorBanding Dark;
    public ColorBanding Foam;
    public ColorBanding Fresnel;
    public ColorBanding SSS;




    public Highlights highlights = new Highlights 
    { HighLightExp = 1 , HighLightBost  = 0};


    public Texture FoamCapTex;

    public Texture FoamTrailTex;
}
