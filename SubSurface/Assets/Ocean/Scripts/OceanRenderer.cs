using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static OceanRenderSetting;

[ExecuteInEditMode]
public class OceanRenderer :MonoBehaviour
{
    public OceanRenderSetting ORS;
    [Tooltip("Tick this box will regenerate All LOD meshes and Materials")]
    public bool hasOceanLOD = false;

    private int threadGroupX, threadGroupY;

    private GameObject[] OceanLODS;
    //private Material[] OceanMATS;
    //temp solution
    private int KIndex = 0;

    int baseColorId = Shader.PropertyToID("_BaseColor");

    int gridSizeId = Shader.PropertyToID("_GridSize");
    int lodIndexId = Shader.PropertyToID("_LODIndex");
    int transitionParams = Shader.PropertyToID("_TransitionParam");
    int lodSizeId = Shader.PropertyToID("_LODSize");

    int lodDisplaceMapId = Shader.PropertyToID("_DisplaceMap");
    int lodNormalMapId = Shader.PropertyToID("_NormalMap");
    int lodNextDisplaceMapId = Shader.PropertyToID("_NextDisplaceMap");
    int lodNextNormalMapId = Shader.PropertyToID("_NextNormalMap");

    //Global params
    int centerPosId = Shader.PropertyToID("_CenterPos");
    //int CameraProjParamsId = Shader.PropertyToID("_CamProjectionParams");

    private void Awake()
    {
        //ORS.LODDisplaceMaps = new RenderTexture[ORS.LODCount]
        //ORS.Initialization()
        //CreateOceanLODs();

        threadGroupX = threadGroupY = Mathf.CeilToInt(ORS.RTSize / 32.0f);
    }

    private void OnEnable()
    {
    }

    private void Update()
    {
#if UNITY_EDITOR
        /*
        if (ORS && !ORS.IsInit)
        {
            Debug.Log("");
            Awake();
        }
        */


        if(!hasOceanLOD)
            CreateOceanLODs();
        if(!Application.isPlaying)
            RenderDisAndNormalMapsForLODs();
#endif

        UpdateShaderGlobalParams();
    }

    private void FixedUpdate()
    {
        RenderDisAndNormalMapsForLODs();
    }

    void CreateOceanLODs()
    {
        if (hasOceanLOD)
        {
            return;
        }
        else
        {
            //delete all children
            GameObject[] children = new GameObject[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                children[i] = transform.GetChild(i).gameObject;
            }
            foreach (GameObject go in children)
            {
                Object.DestroyImmediate(go);
            }

            OceanLODS = new GameObject[ORS.LODCount];

            for (int i = 0; i < ORS.LODCount - 1; i++)
            {
                OceanLODS[i] = BuildLOD(ORS.TileMeshes,
                    ORS.GridSize, ORS.GridCountPerTile, i,
                     ref ORS.OceanMats[i], gameObject);
                //OceanMATS.Add(LODMat);
            }
            //Gen the outer LOD
            int lastLODIndex = ORS.LODCount - 1;
            OceanLODS[lastLODIndex] = BuildLOD(ORS.TileMeshes,
                ORS.GridSize, ORS.GridCountPerTile,
                lastLODIndex,
                 ref ORS.OceanMats[lastLODIndex], gameObject, true);

            //We can set global init params in here
            //Shader.SetGlobalVector(baseColorId, new Vector4(0f, 0.25f, 0.25f, 1.0f));

            hasOceanLOD = true;
        }

    }

    //this function should later be move to ORS, There is nothing depending on the render(beside regen trigger)
    private GameObject BuildLOD(Mesh[] in_TileMeshes, float GridSize, int GridCount,
                                int LODIndex, ref Material LODMat, GameObject parent,
                                bool bIsLastLOD = false)
    {
        //Build th LOD gameobject using tiles, each LOD have 4 tile along XZ axis
        //1st LOD is solid, other LODs are just rings, the Last LOD has the skrit 
        GameObject LOD = new GameObject("LOD_" + LODIndex.ToString());
        LOD.transform.position = parent.transform.position;
        LOD.transform.parent = parent.transform;
        float LODScale = Mathf.Pow(2.0f, LODIndex);

        float TileSize = GridSize * (float)GridCount;
        float LODSize = TileSize * 4.0f * Mathf.Pow(2, LODIndex);
        int TileCount = 0;
        Vector2[] TilesOffsets;
        TileType[] TilesType;
        int[] TilesRotate;
        if (LODIndex == 0)
        {
            TileCount = 16;
            TilesOffsets = new[] {new Vector2(-1.5f, 1.5f), new Vector2(-0.5f, 1.5f), new Vector2(0.5f, 1.5f), new Vector2(1.5f, 1.5f),
                                  new Vector2(-1.5f, 0.5f), new Vector2(-0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1.5f, 0.5f),
                                  new Vector2(-1.5f, -0.5f), new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), new Vector2(1.5f, -0.5f),
                                  new Vector2(-1.5f, -1.5f), new Vector2(-0.5f, -1.5f), new Vector2(0.5f, -1.5f), new Vector2(1.5f, -1.5f) };
            TilesType = new[] {TileType.SlimXFatZ,    TileType.SlimX,    TileType.SlimX,    TileType.SlimXZ,
                               TileType.FatX,         TileType.Interior, TileType.Interior, TileType.SlimX,
                               TileType.FatX,         TileType.Interior, TileType.Interior, TileType.SlimX,
                               TileType.FatXZ,        TileType.FatX,     TileType.FatX,     TileType.FatXSlimZ };
            TilesRotate = new[] { -90,    -90,    -90,    0,
                                  180,    0,      0,      0,
                                  180,    0,      0,      0,
                                  180,    90,     90,     90};
        }
        else
        {
            TileCount = 12;
            TilesOffsets = new[] {new Vector2(-1.5f, 1.5f), new Vector2(-0.5f, 1.5f), new Vector2(0.5f, 1.5f), new Vector2(1.5f, 1.5f),
                                  new Vector2(-1.5f, 0.5f),                                                      new Vector2(1.5f, 0.5f),
                                  new Vector2(-1.5f, -0.5f),                                                     new Vector2(1.5f, -0.5f),
                                  new Vector2(-1.5f, -1.5f), new Vector2(-0.5f, -1.5f), new Vector2(0.5f, -1.5f), new Vector2(1.5f, -1.5f) };

            if (!bIsLastLOD)
                TilesType = new[] {TileType.SlimXFatZ,    TileType.SlimX,    TileType.SlimX,    TileType.SlimXZ,
                               TileType.FatX,                                                TileType.SlimX,
                               TileType.FatX,                                                TileType.SlimX,
                               TileType.FatXZ,        TileType.FatX,     TileType.FatX,     TileType.FatXSlimZ };
            else
                TilesType = new[] {TileType.FatXZOuter,    TileType.FatXOuter,    TileType.FatXOuter,    TileType.FatXZOuter,
                               TileType.FatXOuter,                                                      TileType.FatXOuter,
                               TileType.FatXOuter,                                                      TileType.FatXOuter,
                               TileType.FatXZOuter,        TileType.FatXOuter,     TileType.FatXOuter,     TileType.FatXZOuter };

            TilesRotate = new[] {   -90,    -90,    -90,    0,
                                    180,                    0,
                                    180,                    0,
                                    180,    90,     90,     90 };

        }
        

        //create material for LOD
        //LODMat = new Material(oceanShader);

        //Set the persistent properties(this part should later be moved to )
        LODMat.SetFloat(gridSizeId, GridSize * LODScale);
        LODMat.SetVector(transitionParams, new Vector4(TileSize * 1.7f, TileSize * 1.7f, 0.2f * TileSize, 0.2f * TileSize) * LODScale);
        //LODMat.SetVector(centerPosId, LOD.transform.position);
        LODMat.SetFloat(lodSizeId, LODSize);
        LODMat.SetFloat(lodIndexId, LODIndex);
        LODMat.SetColor(baseColorId, new Vector4(0.25f, 0.25f, 0.25f, 1.0f) * LODIndex);
        LODMat.SetTexture(lodDisplaceMapId, ORS.LODDisplaceMaps[LODIndex]);
        LODMat.SetTexture(lodNormalMapId, ORS.LODNormalMaps[LODIndex]);
        LODMat.SetTexture(lodNextDisplaceMapId, ORS.LODDisplaceMaps[Mathf.Min(LODIndex, ORS.LODCount)]);
        LODMat.SetTexture(lodNextNormalMapId, ORS.LODNormalMaps[Mathf.Min(LODIndex, ORS.LODCount)]);

        //RT assets are refernce and mantained in ORS
        //string MatPath = string.Format("Assets/Ocean/OceanAssets/Material_LOD{0}.asset", LODIndex);
        //LODMat.enableInstancing = true;
        //AssetDatabase.DeleteAsset(MatPath);
        //AssetDatabase.CreateAsset(LODMat, MatPath);
        //block.SetColor(baseColorId, new Vector4(0.25f, 0.25f, 0.25f, 1.0f) * LODIndex);

        for (int i = 0; i < TileCount; i++)
        {
            GameObject CTile = new GameObject(TilesType[i].ToString() + "_" + i.ToString());
            CTile.transform.parent = LOD.transform;
            CTile.AddComponent<MeshFilter>();
            CTile.AddComponent<MeshRenderer>();
            CTile.GetComponent<MeshFilter>().sharedMesh = in_TileMeshes[(int)TilesType[i]];
            CTile.transform.localPosition = new Vector3(TilesOffsets[i].x * TileSize, 0, TilesOffsets[i].y * TileSize);
            CTile.transform.localRotation = Quaternion.Euler(0, TilesRotate[i], 0);


            CTile.GetComponent<MeshRenderer>().sharedMaterial = LODMat;
            //CTile.GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }

        LOD.transform.localScale = new Vector3(LODScale, 1, LODScale);
        return LOD;
    }

 

    void UpdateShaderGlobalParams()
    {
        Shader.SetGlobalVector(centerPosId, transform.position);
        //Shader.SetGlobalVector(CameraProjParamsId, new Vector4(1.0, ))
        /*
        for (int i = 0; i < ORS.OceanMats.Length; i++)
        {
            ORS.OceanMats[i].SetVector(centerPosId, transform.position);
        }
        */
    }


    void RenderDisAndNormalMapsForLODs()
    {
        if (!ORS.shapeShader)
        {
            Debug.LogError("No Shapeshader in Ocean rendeing settings!!");
            return;
        }

        //new ComputeBuffer with the stride is 20 ??
        ComputeBuffer shapeWaveBufer = new ComputeBuffer(ORS.WaveCount, 20);

        ORS.shapeShader.SetFloats("CenterPos", new float[]
            { transform.position.x, transform.position.y, transform.position.z}
            );

        for (int i = ORS.LODCount-1; i>=0; i--)
        {

            ORS.shapeShader.SetInt("WaveCount", ORS.WaveCount);
            //WaveBuffer.SetData(WDs);
            shapeWaveBufer.SetData(ORS.SpectrumWaves);
            ORS.shapeShader.SetBuffer(KIndex, "WavesBuffer", shapeWaveBufer);

            ORS.shapeShader.SetFloat("LODSize", ORS.GridSize * ORS.GridCountPerTile * 4 * Mathf.Pow(2, i) * 1);//times ocean scale
            ORS.shapeShader.SetInt("LODIndex", i);
            ORS.shapeShader.SetFloat("_Time", Time.time);
            ORS.shapeShader.SetFloat("_deltaTime", Time.deltaTime);
            ORS.shapeShader.SetFloat("_foamFadePow", ORS.FoamFadePow);
            /*
            if (i != ORS.LODCount - 1)
            {
                ORS.shapeShader.SetTexture(KIndex, "BaseDisplace", ORS.LODDisplaceMaps[i + 1]);
                ORS.shapeShader.SetTexture(KIndex, "BaseNormal", ORS.LODNormalMaps[i + 1]);
            }
            else
            {
                ORS.shapeShader.SetTexture(KIndex, "BaseDisplace", ORS.LODDisplaceMaps[i]);
                ORS.shapeShader.SetTexture(KIndex, "BaseNormal", ORS.LODNormalMaps[i]);
            }*/
            RenderTexture Temp = new RenderTexture(ORS.RTSize, ORS.RTSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            //Temp.enableRandomWrite = true;
            Temp.antiAliasing = 1;
            Temp.wrapMode = TextureWrapMode.Clamp;
            Temp.filterMode = FilterMode.Trilinear;

            //Debug.Log(ORS.LODDisplaceMaps[i].enableRandomWrite);
            ORS.shapeShader.SetTexture(KIndex, "Displace", ORS.LODDisplaceMaps[i]);
            ORS.shapeShader.SetTexture(KIndex, "Normal", ORS.LODNormalMaps[i]);

            //ORS.shapeShader.SetTexture(KIndex, "NoiseFoam", WaterFoamNoise);

            ORS.shapeShader.Dispatch(KIndex, threadGroupX, threadGroupY, 1);
        }

        shapeWaveBufer.Release();
    }
}
