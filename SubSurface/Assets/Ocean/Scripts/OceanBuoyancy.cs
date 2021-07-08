using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, RequireComponent(typeof(Rigidbody))]
public class OceanBuoyancy : MonoBehaviour
{
    private Rigidbody RB;
    
    [System.Serializable]
    public struct BuoyancyPoint
    {
        public Transform transform;
        [Range(0, 10)]
        public float buoyancyHeight;
        [Range(0, 100)]
        public float maxBuoyancyForce;
        [System.NonSerialized]
        public int BPindex;
           
    }
    public Vector2 WaterDrag = new Vector2(0.5f,2);
    public Vector2 WaterADrag = new Vector2(0.5f,2);
    public Vector3 COMoffset = Vector3.zero;
    [SerializeField]
    public BuoyancyPoint[] BuoyancyPoints;

    float timer = 0;

    // Start is called before the first frame update
    private void Awake()
    {
        TrackAllBuoyPointsInChildren();
    }

    void Start()
    {
        RB = transform.gameObject.GetComponent<Rigidbody>();
        RB.centerOfMass = COMoffset;
        RegisterBuoyPoints();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            for (int i = 0; i < BuoyancyPoints.Length; i++)
            {
                if (BuoyancyPoints[i].BPindex >= 0)
                {
                    Vector4 OceanData = OceanRenderer.Instance.OHS.GetRelativeDepthByIndex(BuoyancyPoints[i].BPindex);
                    Debug.DrawLine(BuoyancyPoints[i].transform.position,
                        BuoyancyPoints[i].transform.position - new Vector3(0.0f, OceanData.w, 0.0f),
                        Color.green
                        );
                }
  
            }
  
        }
        for (int i = 0; i < BuoyancyPoints.Length; i++)
        {

            Debug.DrawLine(BuoyancyPoints[i].transform.position,
                BuoyancyPoints[i].transform.position + new Vector3(0.0f, BuoyancyPoints[i].buoyancyHeight, 0.0f),
                Color.red
                );
        }
#endif
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position + transform.up * COMoffset.y, 0.1f);
    }

    private void FixedUpdate()
    {
        ApplyBuoyancy();
        ApplyDrag();

        RB.AddForceAtPosition(new Vector3(75.0f, 0.0f, 75.0f), transform.position+RB.centerOfMass);

        timer += Time.fixedDeltaTime;
        if (timer > 0.05f)
        {
            timer = 0.0f;
            TestSpawnWaveParticles();
        }
        
    }

    private void OnDestroy()
    {
        //UnRegisterBuoyPoints();
    }

    void TrackAllBuoyPointsInChildren()
    {
        if (BuoyancyPoints.Length == 0)
        {
            //int BPCount = 0;
            List<BuoyancyPoint> buoyancyPointsTemp = new List<BuoyancyPoint>();
            Component[] Children = gameObject.GetComponentsInChildren(typeof(Transform));
            for (int i = 0; i < Children.Length; i++)
            {
                if (Children[i].gameObject.name.Contains("BuoyPoints_"))
                {
                    buoyancyPointsTemp.Add(
                        new BuoyancyPoint()
                        {
                            transform = Children[i].gameObject.transform,
                            buoyancyHeight = 1,
                            maxBuoyancyForce = 1,
                            BPindex = -1
                        });
                }
            }
            //copy the BP list to array for better access
            BuoyancyPoints = new BuoyancyPoint[buoyancyPointsTemp.Count];
            for (int i = 0; i < buoyancyPointsTemp.Count; i++)
            {
                BuoyancyPoints[i] = buoyancyPointsTemp[i];
            }
        }
    }

    void RegisterBuoyPoints()
    {
        for (int i =0;i< BuoyancyPoints.Length;i++)
        {
            
            BuoyancyPoints[i].BPindex = OceanRenderer.Instance.OHS.RegisterOceanSamplePoint(BuoyancyPoints[i].transform);
            //Debug.Log(BuoyancyPoints[i].BPindex);

            //WaveTester.Instance.RegisterOceanSamplePoint(BuoyancyPoints[i].transform);
        }
    }

    void UnRegisterBuoyPoints()
    {
        foreach (BuoyancyPoint BP in BuoyancyPoints)
        {
            OceanRenderer.Instance.OHS.RemoveOceanSamplePoint(BP.transform);
        }
    }

    void ApplyBuoyancy()
    {
        for (int i = 0; i < BuoyancyPoints.Length; i++)
        {
            Vector4 OceanData = OceanRenderer.Instance.OHS.GetRelativeDepthByIndex(BuoyancyPoints[i].BPindex);
            float ValidDepth = Mathf.Clamp(-OceanData.w,
                0, BuoyancyPoints[i].buoyancyHeight);
            Vector3 BuoyancyDirection = new Vector3(0.0f, 1.0f, 0.0f);//Vector3.Normalize(new Vector3(OceanData.x, OceanData.y, OceanData.z));
            if (ValidDepth != 0)
            {
                Vector3 buoyancyforce =
                    BuoyancyDirection * 
                    BuoyancyPoints[i].maxBuoyancyForce * 
                    (ValidDepth / BuoyancyPoints[i].buoyancyHeight);
                Vector3 buoyancyApplyPoint =
                    BuoyancyPoints[i].transform.position +
                    BuoyancyPoints[i].transform.up * ValidDepth * 0.5f;
                RB.AddForceAtPosition(buoyancyforce, BuoyancyPoints[i].transform.position, ForceMode.Force);
                
                Debug.DrawLine(buoyancyApplyPoint, buoyancyApplyPoint + buoyancyforce, Color.cyan);
            }
            
        }
    }
    void ApplyDrag()
    {
        int submergedBPCount = 0;
        for (int i = 0; i < BuoyancyPoints.Length; i++)
        {
            Vector4 OceanData = OceanRenderer.Instance.OHS.GetRelativeDepthByIndex(BuoyancyPoints[i].BPindex);
            float ValidDepth = Mathf.Clamp(-OceanData.z,
                0, BuoyancyPoints[i].buoyancyHeight);
            if (ValidDepth != 0)
            {
                submergedBPCount++;
            }

        }

        //basic drag system
        RB.drag = WaterDrag.y * ((float)submergedBPCount / (float)BuoyancyPoints.Length) + WaterDrag.x;
        RB.angularDrag = WaterADrag.y * ((float)submergedBPCount / (float)BuoyancyPoints.Length) + WaterADrag.x;
    }

    void TestSpawnWaveParticles()
    {
        Vector3 relativeVel = Vector3.Normalize(new Vector3 (RB.velocity.x, 0, RB.velocity.z));

        if (relativeVel.magnitude > 0.1f)
        {
            int subd = 24;
            float subAngle = 360.0f / (float)subd;

            for (int i = 0; i < subd; i++)
            {
                float angle = subAngle * i;
                Quaternion RotationLeft = Quaternion.Euler(0, angle, 0);
                Vector3 direction = Vector3.Normalize(RotationLeft * relativeVel);
                float amp = Mathf.Sign(Vector3.Dot(relativeVel, direction)) * 0.25f * relativeVel.magnitude;
                //direction *= Mathf.Sign(amp);
                OceanWaveParticleRender.WaveParticle NewWPs = new OceanWaveParticleRender.WaveParticle();
                //if (amp < 0)
                    //continue;

                NewWPs.Amplitude = amp*0.125f;
                NewWPs.BirthTime = Time.time-1.0f;
                NewWPs.DispersionAngle = subAngle;
                NewWPs.Direction = new Vector2(direction.x, direction.z);
                NewWPs.Origin = new Vector2(transform.position.x, transform.position.z);


                OceanRenderer.Instance.OWPR.SpawnWaveParticles(NewWPs);
            }

            
        }


    }
}
