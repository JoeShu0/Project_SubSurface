using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//this script will have a list of reference of all the points needed for ocean height sampling
//And will sample the ocean Height form GPU in async
public class OceanHeightSampler
{
    private ComputeShader GetHeight;
    public bool IsRetrivingGPUData = false;

    List<Transform> SamplePointTranfroms = new List<Transform>();
    Vector4[] OceanDatas = new Vector4[128];

    OceanRenderSetting ORS;
    Transform OceanCenter;
    public OceanHeightSampler(
        OceanRenderSetting ORS,
        Transform OceanCenter,
        ComputeShader GetHeight
        )
    {
        this.ORS = ORS;
        this.OceanCenter = OceanCenter;
        this.GetHeight = GetHeight;
    }

    public int RegisterOceanSamplePoint(Transform SamplePoint)
    {
        if (!SamplePointTranfroms.Contains(SamplePoint))
        {
            SamplePointTranfroms.Add(SamplePoint);
            ValidateSamplePoints();
            return SamplePointTranfroms.Count - 1;
        }
        else 
        {
            ValidateSamplePoints();
            int BPIndex = SamplePointTranfroms.FindIndex(d=>d == SamplePoint);//???
            return BPIndex;
        }
    }
    public void RemoveOceanSamplePoint(Transform SamplePoint)
    {
        if (SamplePointTranfroms.Contains(SamplePoint))
        {
            SamplePointTranfroms.Remove(SamplePoint);
        }
        ValidateSamplePoints();
    }
    public void ValidateSamplePoints()
    {
        //Debug.Log(offsets.Length.ToString() + ":" + SamplePointTranfroms.Count.ToString());
        
        for (int i = 0; i < SamplePointTranfroms.Count; i++)
        {
            if (!SamplePointTranfroms[i])
            {
                SamplePointTranfroms.RemoveAt(i);
            }
        }
        if (OceanDatas.Length <= SamplePointTranfroms.Count||
            OceanDatas.Length > SamplePointTranfroms.Count + 20)
        {
            Vector4[] newOffsets = new Vector4[(int)(SamplePointTranfroms.Count + 20)];
            System.Array.Copy(OceanDatas, newOffsets, SamplePointTranfroms.Count);
            OceanDatas = newOffsets;
        }
    }

    public IEnumerator GetRelativeDepth()
    {
        float LOD0Size = ORS.GridSize * ORS.GridCountPerTile * OceanRenderSetting.TilePerLOD;

        //get the positions
        Vector3[] PosWSPoints = new Vector3[SamplePointTranfroms.Count];
        Vector4[] OceanDataBack = new Vector4[SamplePointTranfroms.Count];
        for (int i = 0; i < SamplePointTranfroms.Count; i++)
        {
            PosWSPoints[i] = SamplePointTranfroms[i].position;
            OceanDataBack[i] = Vector4.zero;
        }

        ComputeBuffer Positions = new ComputeBuffer(SamplePointTranfroms.Count, 12);
        Positions.SetData(PosWSPoints);
        ComputeBuffer OceanDataBuffer = new ComputeBuffer(SamplePointTranfroms.Count, 16);
        //RelativeDepths.SetData(OceanDataBack);

        //GetHeight.SetTexture

        GetHeight.SetBuffer(0, "_Positions", Positions);
        GetHeight.SetBuffer(0, "_NormalDepth", OceanDataBuffer);

        //GetHeight.SetTexture(0, "_DisplaceLOD", ORS.LODDisplaceMaps[0]);
        GetHeight.SetTexture(0, "_DisplaceArray", ORS.LODDisplaceMapsArray);
        GetHeight.SetTexture(0, "_NormalArray", ORS.LODNormalMapsArray);

        GetHeight.SetVector("_OceanLODParams", new Vector4
            (OceanCenter.position.x, OceanCenter.position.y, OceanCenter.position.z, LOD0Size));

        GetHeight.Dispatch(0, 64, 1, 1);

        var request = AsyncGPUReadback.Request(OceanDataBuffer);

        Positions.Release();
        OceanDataBuffer.Release();

        //Debug.Log("frame1:" + Time.frameCount);
        IsRetrivingGPUData = true;
        yield return new WaitUntil(() => request.done);
        //Debug.Log("frame2:" + Time.frameCount);
        IsRetrivingGPUData = false;

        OceanDataBack = request.GetData<Vector4>().ToArray();

        //testbuffer.GetData(testdata);


        System.Array.Copy(OceanDataBack, OceanDatas, OceanDataBack.Length);
    }

    public Vector4 GetRelativeDepthByIndex(int BPIndex)
    {
        return OceanDatas[BPIndex];
    }
}
