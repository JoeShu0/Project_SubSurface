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


    //Material Properties
    public Color BaseColor;
    public Color BrightColor;
    public Color DarkColor;
    public Color FoamColor;

    public Highlights highlights = new Highlights 
    { HighLightExp = 1 , HighLightBost  = 0};
}
