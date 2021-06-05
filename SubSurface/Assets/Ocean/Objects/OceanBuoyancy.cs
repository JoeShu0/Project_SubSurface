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
    public float MaxWaterDrag = 5;
    public float MaxWaterADrag = 5;
    public Vector3 COMoffset = Vector3.zero;
    [SerializeField]
    public BuoyancyPoint[] BuoyancyPoints;
    
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
                    Vector3 Reldepth = OceanRenderer.Instance.OHS.GetRelativeDepthByIndex(BuoyancyPoints[i].BPindex);
                    Debug.DrawLine(BuoyancyPoints[i].transform.position,
                        BuoyancyPoints[i].transform.position - new Vector3(0.0f, Reldepth.x, 0.0f),
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
            Vector3 Reldepth = OceanRenderer.Instance.OHS.GetRelativeDepthByIndex(BuoyancyPoints[i].BPindex);
            float ValidDepth = Mathf.Clamp(-Reldepth.x,
                0, BuoyancyPoints[i].buoyancyHeight);
            Vector3 BuoyancyDirection = new Vector3(0.0f, 1.0f, 0.0f);
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
            Vector3 Reldepth = OceanRenderer.Instance.OHS.GetRelativeDepthByIndex(BuoyancyPoints[i].BPindex);
            float ValidDepth = Mathf.Clamp(-Reldepth.x,
                0, BuoyancyPoints[i].buoyancyHeight);
            if (ValidDepth != 0)
            {
                submergedBPCount++;
            }

        }

        RB.drag = MaxWaterDrag * ((float)submergedBPCount / (float)BuoyancyPoints.Length);
        RB.angularDrag = MaxWaterADrag * ((float)submergedBPCount / (float)BuoyancyPoints.Length);
    }
}
