using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightSampler : MonoBehaviour
{
    public OceanRenderSetting ORS;

    public Transform Ocean;

    public ComputeShader GetDepth;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float height = GetWaveHeight(transform.position);

        transform.position = new Vector3(
            transform.position.x,
            height * 10f,
            transform.position.z
            );
    }

    float GetWaveHeight(Vector3 PositionWS)
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

        return (float)LOD;
    }
}
