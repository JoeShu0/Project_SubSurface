using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class HeightSampler : MonoBehaviour
{
    public OceanRenderSetting ORS;

    public Transform Ocean;

    public ComputeShader GetHeight;

    public float[] testdata = new float[16384];
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start:" + Time.frameCount);
        testdata = new float[16384];

        StartCoroutine(sample());
    }

    // Update is called once per frame
    void Update()
    {
        int lod = GetWaveLOD(transform.position);
        /*
        transform.position = new Vector3(
            transform.position.x,
            height * 10f,
            transform.position.z
            );
        */
        //sampleheight(transform.position, lod);
    }

    private void FixedUpdate()
    {
        //sampleheight(transform.position, 0);
        float height = 0;
        for (int i=1; i<256; i++)
        {
            //height = calHeight(transform.position);
        }
        transform.position = new Vector3(
            transform.position.x,
            height,
            transform.position.z
            );
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
        float A = 0;

        ComputeBuffer testbuffer = new ComputeBuffer(16384, 4);
        testbuffer.SetData(testdata);

        GetHeight.SetBuffer(0, "_Height", testbuffer);

        GetHeight.Dispatch(0, 64, 1, 1);

        var request = AsyncGPUReadback.Request(testbuffer);

        Debug.Log("frame1:" + Time.frameCount);
        yield return new WaitUntil(() => request.done);
        Debug.Log("frame2:" + Time.frameCount);

        testdata = request.GetData<float>().ToArray();

        //testbuffer.GetData(testdata);

        testbuffer.Release();


        A = testdata[20];

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
