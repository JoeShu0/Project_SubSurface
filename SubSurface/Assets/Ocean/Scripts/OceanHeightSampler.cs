using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this script will have a list of reference of all the points needed for ocean height sampling
//And will sample the ocean Height form GPU in async
public class OceanHeightSampler
{
    private ComputeShader GetHeight;
    private bool IsRetrivingGPUData = false;

    List<Transform> SamplePointTranfroms = new List<Transform>();
    Vector3[] offsets = new Vector3[128];

    OceanRenderSetting ORS;
    Transform OceanCenter;
    public OceanHeightSampler(
        OceanRenderSetting ORS,
        Transform OceanCenter
        )
    {
        this.ORS = ORS;
        this.OceanCenter = OceanCenter;
    }

    //public void RegisterOceanSamplePoint()
}
