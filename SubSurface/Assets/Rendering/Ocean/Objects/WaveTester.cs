using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveTester : MonoBehaviour
{

    RenderTexture frameA, frameB, PointframeA, PointframeB;
    private int i = 0;
    public ComputeShader FiniteWater;

    List<Transform> WavePointTranfroms = new List<Transform>();

    Transform OceanCenter;

    //*****this class is a singleton*****
    private static WaveTester _instance;
    public static WaveTester Instance
    {
        get { return _instance; }
    }
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogError("OceanRender Singleton pattern breaks");
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
        //*****this class is a singleton*****
    }

    public void RegisterOceanSamplePoint(Transform SamplePoint)
    {
        
        if (!WavePointTranfroms.Contains(SamplePoint))
        {
            WavePointTranfroms.Add(SamplePoint);
        }
    }

    void Start()
    {
        frameA = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        frameA.enableRandomWrite = true;
        frameA.Create();

        frameB = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        frameB.enableRandomWrite = true;
        frameB.Create();

        PointframeA = new RenderTexture(512, 512, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
        PointframeA.enableRandomWrite = true;
        PointframeA.Create();

        PointframeB = new RenderTexture(512, 512, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
        PointframeB.enableRandomWrite = true;
        PointframeB.Create();

        MeshRenderer MR = GetComponent<MeshRenderer>();
        MR.sharedMaterial.SetTexture("_BaseMap", frameA);
    }
    
    private void FixedUpdate()
    {



        if (i %1 == 0)
        {
            Vector2[] CurAndPastPos = OceanRenderer.Instance.ORS.CurAndPastPos;

            FiniteWater.SetVector("_TimeParams",
                new Vector4(Time.deltaTime, 1.0f / Time.deltaTime, Time.fixedDeltaTime, 1.0f / Time.fixedDeltaTime));
            FiniteWater.SetVector("_OceanLODParams", new Vector4
            (0.0f, 0.0f, 0.0f, 64));//center pos andLOD0Size here

            FiniteWater.SetVector("_CurAndPastPos", 
                new Vector4(CurAndPastPos[0].x, CurAndPastPos[0].y, CurAndPastPos[1].x, CurAndPastPos[1].y));//center pos andLOD0Size here

            //clear framP to Black
            FiniteWater.SetTexture(0, "framePA", PointframeA);
            FiniteWater.SetTexture(0, "framePB", PointframeB);
            FiniteWater.SetTexture(0, "frameA", frameA);
            FiniteWater.SetTexture(0, "frameB", frameB);
            FiniteWater.Dispatch(0, 16, 16, 1);

            //render wave to points 
            Vector4[] PosWSPoints = new Vector4[1024];
            for (int i = 0; i < WavePointTranfroms.Count; i++)
            {
                Vector3 P = WavePointTranfroms[i].position;
                PosWSPoints[i] = new Vector4(P.x, P.y, P.z, 1.0f);
            }
            ComputeBuffer Positions = new ComputeBuffer(1024, 16);
            Positions.SetData(PosWSPoints);
            FiniteWater.SetBuffer(1, "_WavePoints", Positions);
            FiniteWater.SetTexture(1, "framePA", PointframeA);
            FiniteWater.Dispatch(1, 16, 1, 1);
            Positions.Release();
            
            //render point to wavesX
            FiniteWater.SetTexture(2, "framePA", PointframeA);
            FiniteWater.SetTexture(2, "framePB", PointframeB);
            FiniteWater.Dispatch(2, 16, 16, 1);

            //render point to wavesY
            FiniteWater.SetTexture(3, "framePB", PointframeB);
            FiniteWater.SetTexture(3, "frameA", frameA);
            FiniteWater.Dispatch(3, 16, 16, 1);

            FiniteWater.SetTexture(4, "frameA", frameA);
            FiniteWater.SetTexture(4, "frameB", frameB);
            FiniteWater.Dispatch(4, 16, 16, 1);
            
        }

        i++;
    }
}
