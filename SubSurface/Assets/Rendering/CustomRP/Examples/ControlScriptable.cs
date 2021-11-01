using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ControlScriptable : MonoBehaviour
{
    public PostFXSettings PFXS;
    PostFXSettings.BloomSettings BS;
    private void Awake()
    {

        BS = PFXS.Bloom;
        BS.maxIterations = 0;
        BS.intensity = 0.0f;
        PFXS.Bloom = BS;
    }
    private void FixedUpdate()
    {
        BS = PFXS.Bloom;
        BS.maxIterations = 8;
        BS.intensity = 0.0f + Time.time;
        PFXS.Bloom = BS;
    }
}
