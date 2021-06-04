using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class HeightSampler : MonoBehaviour
{
    public OceanRenderSetting ORS;

    public Transform Ocean;

    public ComputeShader GetHeight;

    private bool IsRetrivingGPUData = false;

    //public float[] testdata = new float[16384];

    List<Transform> SamplePointTranfroms = new List<Transform>();
    //public List<Transform> SamplePointTranfroms = new List<Transform>();
    Vector3[] offsets = new Vector3[128];
    float originalheight;


    void Start()
    {
        //Debug.Log("Start:" + Time.frameCount);
        //testdata = new float[16384];
        originalheight = transform.position.y;

        //SamplePointTranfroms.Add(transform);

        for (int i = 0; i < transform.childCount; i++)
        {
            SamplePointTranfroms.Add(transform.GetChild(i));
        }
        //StartCoroutine(sample());
    }

    // Update is called once per frame
    void Update()
    {
        int lod = GetWaveLOD(transform.position);
        
        for (int i = 0; i < transform.childCount; i++)
        {
            Vector3 pos = SamplePointTranfroms[i].position;
            SamplePointTranfroms[i].position = new Vector3(
            pos.x,
            offsets[i].y,
            pos.z
            );
        }
        
        
        //sampleheight(transform.position, lod);
        /*
        Debug.DrawLine(transform.position,
            transform.position + offset.y * new Vector3(0.0f, 1.0f, 0.0f),
            Color.red);
        */
    }

    private void FixedUpdate()
    {
        //sampleheight(transform.position, 0);
        
        
        if (!IsRetrivingGPUData)
        {
            //int lod = GetWaveLOD(transform.position);
            //offsets = new Vector3[SamplePointTranfroms.Count];
            StartCoroutine(sample());
        }
    }

    int GetWaveLOD(Vector3 PositionWS)
    {
        Vector3 PositionRelate = PositionWS - Ocean.position;
        float LOD0Size = ORS.GridSize * ORS.GridCountPerTile * 4;
        float EdgeBuffer = 5.0f;
        int LODx = Mathf.CeilToInt(Mathf.Log(
            (Mathf.Abs(PositionRelate.x)+ EdgeBuffer) / (LOD0Size*0.5f), 
            2));
        int LODy = Mathf.CeilToInt(Mathf.Log(
            (Mathf.Abs(PositionRelate.z)+ EdgeBuffer) / (LOD0Size * 0.5f), 
            2));
        int LOD = Mathf.Max(0,Mathf.Max(LODx, LODy));

        Vector3 UV = PositionRelate * LOD0Size + new Vector3(0.5f, 0.5f, 0.5f);

        //GetDepth

        //Vector4 col = ORS.LODDisplaceMaps[LOD].

        return LOD;
    }

    IEnumerator sample()
    {
        float LOD0Size = ORS.GridSize * ORS.GridCountPerTile * OceanRenderSetting.TilePerLOD;

        //get the positions
        Vector3[] PosWSPoints = new Vector3[SamplePointTranfroms.Count];
        Vector3[] RelDepths = new Vector3[SamplePointTranfroms.Count];
        for (int i = 0; i < SamplePointTranfroms.Count; i++)
        {
            PosWSPoints[i] = SamplePointTranfroms[i].position;
            RelDepths[i] = Vector3.zero;
        }

        ComputeBuffer Positions = new ComputeBuffer(SamplePointTranfroms.Count, 12);
        Positions.SetData(PosWSPoints);
        ComputeBuffer RelativeDepths = new ComputeBuffer(SamplePointTranfroms.Count, 12);
        RelativeDepths.SetData(RelDepths);

        GetHeight.SetBuffer(0, "_Positions", Positions);
        GetHeight.SetBuffer(0, "_ReltiveDepth", RelativeDepths);

        GetHeight.SetTexture(0, "_DisplaceLOD", ORS.LODDisplaceMaps[0]);
        GetHeight.SetVector("_OceanLODParams" , new Vector4
            ( Ocean.position.x, Ocean.position.y, Ocean.position.z, LOD0Size));

        GetHeight.Dispatch(0, 64, 1, 1);

        var request = AsyncGPUReadback.Request(RelativeDepths);

        Positions.Release();
        RelativeDepths.Release();

        //Debug.Log("frame1:" + Time.frameCount);
        IsRetrivingGPUData = true;
        yield return new WaitUntil(() => request.done);
        //Debug.Log("frame2:" + Time.frameCount);
        IsRetrivingGPUData = false;

        RelDepths = request.GetData<Vector3>().ToArray();

        //testbuffer.GetData(testdata);
        

        System.Array.Copy(RelDepths, offsets, SamplePointTranfroms.Count);
        /*
        for (int i = 0; i < SamplePointTranfroms.Count; i++)
        {
            offsets[i] = RelDepths[i];
        }
        */
            
    }

    float calHeight(Vector3 PosWS)
    {
        float PI = 14159265f;
        float _LODWaveAmpMul = 1.0f;
        Vector3 displace = new Vector3(0.0f,0.0f,0.0f);
        for (int i = 0; i < ORS.WaveCount; i++)
        {
            float _WaveLength = ORS.SpectrumWaves[i].WaveLength;
            float k = 2 * PI / _WaveLength;
            float _Amplitude = ORS.SpectrumWaves[i].Amplitude * _LODWaveAmpMul;
            float _Steepness = _Amplitude * k;
            float _Speed = ORS.SpectrumWaves[i].Speed;
            Vector2 _Direction = Vector3.Normalize(ORS.SpectrumWaves[i].Direction);
            float f = k * (Vector3.Dot(PosWS, new Vector3(_Direction.x, 0, _Direction.y)) - Time.time * _Speed);

            float Wx = _Amplitude * Mathf.Cos(f) * _Direction.x;
            float Wz = _Amplitude * Mathf.Cos(f) * _Direction.y;
            float Wy = _Amplitude * Mathf.Sin(f) * 1.0f;

            displace += new Vector3(Wx, Wy, Wz);
        }

        return displace.y;
    }
}
