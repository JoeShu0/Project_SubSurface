using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static OceanRenderSetting;

[ExecuteInEditMode]
public class OceanRenderer :MonoBehaviour
{
    public OceanRenderSetting ORS;


    private void Awake()
    {
        //ORS.LODDisplaceMaps = new RenderTexture[ORS.LODCount];

        if (ORS)
        {
            Debug.Log("Awake and ORS is here");
            
            //Generate all LODs and Material for these LODs 
            GameObject[] LODS = new GameObject[ORS.LODCount];
            for (int i = 0; i < ORS.LODCount - 1; i++)
            {
                LODS[i] = BuildLOD(ORS.TileMeshes, ORS.GridSize, ORS.GridCountPerTile, i, ORS.oceanMaterial, gameObject);
            }
            //Gen the outer LOD
            int lastLODIndex = ORS.LODCount - 1;
            LODS[lastLODIndex] = BuildLOD(ORS.TileMeshes, ORS.GridSize, ORS.GridCountPerTile, lastLODIndex, ORS.oceanMaterial, gameObject, true);
      
            

            ORS.IsInit = true;
        }

        
    }

    private void OnEnable()
    {
        if (ORS)
        {
            Debug.Log("Awake and ORS is here");

            //Generate all LODs and Material for these LODs 
            GameObject[] LODS = new GameObject[ORS.LODCount];
            for (int i = 0; i < ORS.LODCount - 1; i++)
            {
                LODS[i] = BuildLOD(ORS.TileMeshes, ORS.GridSize, ORS.GridCountPerTile, i, ORS.oceanMaterial, gameObject);
            }
            //Gen the outer LOD
            int lastLODIndex = ORS.LODCount - 1;
            LODS[lastLODIndex] = BuildLOD(ORS.TileMeshes, ORS.GridSize, ORS.GridCountPerTile, lastLODIndex, ORS.oceanMaterial, gameObject, true);



            ORS.IsInit = true;
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (ORS && !ORS.IsInit)
        {
            Debug.Log("");
            Awake();
        }
#endif
    }

    Mesh GenerateTile(OceanRenderSetting.TileType type, float GridSize, int GridCount)
    {
        //make sure the grid size is fixed bwteen tiles si that the snap works
        //the generated mesh should have the pivot on the center of the interior type. and consistent through out all tiles
        //So when placing the tiles we can just use symmetry~

        //try build a interior tile
        Mesh tilemesh = new Mesh();
        tilemesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        int GridCountX = GridCount;
        int GridCountZ = GridCount;
        float TileSizeX = GridSize * GridCountX;
        float TileSizeZ = GridSize * GridCountZ;

        bool bIsOutertile = false;
        switch (type)
        {
            case TileType.Interior:
                break;
            case TileType.FatX:
                GridCountX++;
                break;
            case TileType.SlimX:
                GridCountX--;
                break;
            case TileType.FatXSlimZ:
                GridCountX++;
                GridCountZ--;
                break;
            case TileType.SlimXFatZ:
                GridCountX--;
                GridCountZ++;
                break;
            case TileType.SlimXZ:
                GridCountX--;
                GridCountZ--;
                break;
            case TileType.FatXZ:
                GridCountX++;
                GridCountZ++;
                break;
            case TileType.FatXOuter:
                GridCountX++;
                bIsOutertile = true;
                break;
            case TileType.FatXZOuter:
                GridCountX++;
                GridCountZ++;
                bIsOutertile = true;
                break;
            case TileType.Count:
                Debug.LogError("Invalide TileType!");
                return null;
        }

        Vector3[] vertices = new Vector3[(GridCountX + 1) * (GridCountZ + 1)];
        //float incrementX = tileSizeX / (float)(tileXPCount - 1);
        //float incrementZ = tileSizeZ / (float)(tileZPCount - 1);
        for (int x = 0; x < GridCountX + 1; x++)
        {
            for (int z = 0; z < GridCountZ + 1; z++)
            {
                vertices[x * (GridCountZ + 1) + z] = new Vector3(GridSize * x, 0.0f, GridSize * z)
                    + new Vector3(-0.5f * TileSizeX, 0, -0.5f * TileSizeX);
            }
        }
        if (bIsOutertile)
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].x - 0.5f * TileSizeX > 0.1f)
                    vertices[i].x *= 50.0f;
                if (vertices[i].z - 0.5f * TileSizeZ > 0.1f)
                    vertices[i].z *= 50.0f;
            }
        tilemesh.vertices = vertices;

        int[] triangles = new int[(GridCountX) * (GridCountZ) * 6];
        int QuadOrient = 0;
        for (int x = 0; x < GridCountX; x++)
        {
            for (int z = 0; z < GridCountZ; z++)
            {
                //quad num x*(HeightMapSize-1) + z
                int QuadNum = x * (GridCountZ) + z;
                int TLPont = x * (GridCountZ + 1) + z;
                if (QuadOrient % 2 == 0)
                {
                    triangles[QuadNum * 6 + 0] = TLPont;
                    triangles[QuadNum * 6 + 1] = TLPont + 1;
                    triangles[QuadNum * 6 + 2] = TLPont + GridCountZ + 2;
                    triangles[QuadNum * 6 + 3] = TLPont;
                    triangles[QuadNum * 6 + 4] = TLPont + GridCountZ + 2;
                    triangles[QuadNum * 6 + 5] = TLPont + GridCountZ + 1;
                }
                else
                {
                    triangles[QuadNum * 6 + 0] = TLPont;
                    triangles[QuadNum * 6 + 1] = TLPont + 1;
                    triangles[QuadNum * 6 + 2] = TLPont + GridCountZ + 1;
                    triangles[QuadNum * 6 + 3] = TLPont + GridCountZ + 1;
                    triangles[QuadNum * 6 + 4] = TLPont + 1;
                    triangles[QuadNum * 6 + 5] = TLPont + GridCountZ + 2;

                }
                QuadOrient++;


            }
            QuadOrient += GridCountZ % 2 + 1;
        }
        tilemesh.triangles = triangles;

        Vector2[] UVs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            UVs[i] = new Vector2(vertices[i].z / (float)TileSizeX, vertices[i].x / (float)TileSizeZ);
        }

        tilemesh.uv = UVs;

        //Intentionly make larger bound to avoid fructrum culling when moving vertex~
        tilemesh.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(TileSizeX * 1.8f, 20.0f, TileSizeZ * 1.8f));
        //Debug.Log(type);
        return tilemesh;
    }
    private GameObject BuildLOD(Mesh[] in_TileMeshes, float GridSize, int GridCount,
                                int LODIndex, Material oceanMaterial, GameObject parent,
                                bool bIsLastLOD = false)
    {
        //Build th LOD gameobject using tiles, each LOD have 4 tile along XZ axis
        //1st LOD is solid, other LODs are just rings, the Last LOD has the skrit 
        GameObject LOD = new GameObject("LOD_" + LODIndex.ToString());
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
        /*
        TileMat.SetFloat("_GridSize", GridSize * LODScale);
        TileMat.SetVector("_TransitionParam", new Vector4(TileSize * 1.25f, TileSize * 1.25f, 0.5f * TileSize, 0.5f * TileSize) * LODScale);
        TileMat.SetVector("_CenterPos", LOD.transform.position);
        TileMat.SetFloat("_LODSize", LODSize);
        */

        for (int i = 0; i < TileCount; i++)
        {
            GameObject CTile = new GameObject(TilesType[i].ToString() + "_" + i.ToString());
            CTile.transform.parent = LOD.transform;
            CTile.AddComponent<MeshFilter>();
            CTile.AddComponent<MeshRenderer>();
            CTile.GetComponent<MeshFilter>().sharedMesh = in_TileMeshes[(int)TilesType[i]];
            CTile.transform.localPosition = new Vector3(TilesOffsets[i].x * TileSize, 0, TilesOffsets[i].y * TileSize);
            CTile.transform.localRotation = Quaternion.Euler(0, TilesRotate[i], 0);


            CTile.GetComponent<MeshRenderer>().sharedMaterial = oceanMaterial;
        }

        LOD.transform.localScale = new Vector3(LODScale, 1, LODScale);
        return LOD;
    }

}
