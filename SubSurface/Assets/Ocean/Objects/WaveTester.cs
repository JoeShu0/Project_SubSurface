using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveTester : MonoBehaviour
{

    RenderTexture frameA, frameB, Pointframe;
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

        Pointframe = new RenderTexture(512, 512, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
        Pointframe.enableRandomWrite = true;
        Pointframe.Create();

        MeshRenderer MR = GetComponent<MeshRenderer>();
        MR.sharedMaterial.SetTexture("_BaseMap", frameA);
    }
    
    private void FixedUpdate()
    {



        if (i %1 == 0)
        {
            FiniteWater.SetVector("_TimeParams",
                new Vector4(Time.deltaTime, 1.0f / Time.deltaTime, Time.fixedDeltaTime, 1.0f / Time.fixedDeltaTime));
            FiniteWater.SetVector("_OceanLODParams", new Vector4
            (0.0f,0.0f,0.0f, 64));//center pos andLOD0Size here


            //render wave to points 
            Vector4[] PosWSPoints = new Vector4[1024];
            for (int i = 0; i < WavePointTranfroms.Count; i++)
            {
                Vector3 P = WavePointTranfroms[i].position;
                PosWSPoints[i] = new Vector4(P.x, P.y, P.z, 1.0f);
            }
            //Debug.Log(PosWSPoints[0]);
            ComputeBuffer Positions = new ComputeBuffer(1024, 16);
            Positions.SetData(PosWSPoints);
            FiniteWater.SetBuffer(0, "_WavePoints", Positions);
            FiniteWater.SetTexture(0, "frameP", Pointframe);
            FiniteWater.SetTexture(0, "frameA", frameA);
            FiniteWater.Dispatch(0, 16, 16, 1);
            Positions.Release();

            //render point to waves
            FiniteWater.SetTexture(1, "frameP", Pointframe);
            FiniteWater.SetTexture(1, "frameA", frameA);
            //FiniteWater.SetTexture(1, "frameB", frameB);
            FiniteWater.Dispatch(1, 32, 32, 1);

           
            /*
            FiniteWater.SetTexture(1, "frameA", frameA);
            FiniteWater.SetTexture(1, "frameB", frameB);

            FiniteWater.Dispatch(1, 16, 16, 1);
            */
        }

        i++;
    }
}
