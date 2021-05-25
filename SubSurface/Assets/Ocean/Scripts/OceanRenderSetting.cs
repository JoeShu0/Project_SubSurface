using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Custom Ocean Setting")]
public class OceanRenderSetting : ScriptableObject
{
    [SerializeField]
    public Material TestMaterial;

    [SerializeField]
    public Mesh TestMesh;

    public bool IsInit = false;

    public Shader oceanShader;
    public Material oceanMaterial;

    //Count for LOD rings
    public int LODCount = 8;
    //Min grid size
    public float GridSize = 0.2f;
    //grid count for each tile in standard, have to be the mul of 4 since the snapping requires it
    public int GridCountPerTile = 80;
    //RTSize effect rendertexture size (displace and normal) for each LOD, lower it will effect normalmap quality
    public int RTSize = 512;
    //WaveCount should be mul of 4 Since we are packing it into vectors
    //And We are getting each LOD to compute diff wave length so we fix the WaveCount to 64=8*8
    public int WaveCount = 128;

    public enum TileType
    {
        Interior,
        FatX,
        SlimX,
        FatXSlimZ,
        SlimXFatZ,
        SlimXZ,
        FatXZ,
        FatXOuter,
        FatXZOuter,
        Count
    }

    //All th tile types
    public Mesh[] TileMeshes = new Mesh[(int)TileType.Count];
    //anim wave render texture
    public RenderTexture[] LODDisplaceMaps;
    public RenderTexture[] LODNormalMaps;


    private void Awake()
    {
        InitLODRTs();
        InitTiles();
        oceanShader = Shader.Find("Custom_RP/OceanShader");
        oceanMaterial = new Material(oceanShader);
        IsInit = true;
    }

    void InitLODRTs()
    {
        LODDisplaceMaps = new RenderTexture[LODCount];
        LODNormalMaps = new RenderTexture[LODCount];
        for (int i = 0; i < LODDisplaceMaps.Length; i++)
        {
            if (LODDisplaceMaps[i] != null)
                LODDisplaceMaps[i].Release();
            LODDisplaceMaps[i] = new RenderTexture(RTSize, RTSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            LODDisplaceMaps[i].enableRandomWrite = true;
            LODDisplaceMaps[i].antiAliasing = 1;
            //LODDisplaceMaps[i].bindTextureMS = true;
            LODDisplaceMaps[i].wrapMode = TextureWrapMode.Clamp;
            LODDisplaceMaps[i].filterMode = FilterMode.Trilinear;
            LODDisplaceMaps[i].Create();

            if (LODNormalMaps[i] != null)
                LODNormalMaps[i].Release();
            LODNormalMaps[i] = new RenderTexture(RTSize, RTSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            LODNormalMaps[i].enableRandomWrite = true;
            LODNormalMaps[i].wrapMode = TextureWrapMode.Clamp;
            LODNormalMaps[i].filterMode = FilterMode.Trilinear;
            LODNormalMaps[i].Create();
        }
    }

    void InitTiles()
    {
        //generate all th tile types 
        for (int i = 0; i < (int)TileType.Count; i++)
        {
            TileMeshes[i] = GenerateTile((OceanRenderSetting.TileType)i, GridSize, GridCountPerTile);
        }
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
            case OceanRenderSetting.TileType.Interior:
                break;
            case OceanRenderSetting.TileType.FatX:
                GridCountX++;
                break;
            case OceanRenderSetting.TileType.SlimX:
                GridCountX--;
                break;
            case OceanRenderSetting.TileType.FatXSlimZ:
                GridCountX++;
                GridCountZ--;
                break;
            case OceanRenderSetting.TileType.SlimXFatZ:
                GridCountX--;
                GridCountZ++;
                break;
            case OceanRenderSetting.TileType.SlimXZ:
                GridCountX--;
                GridCountZ--;
                break;
            case OceanRenderSetting.TileType.FatXZ:
                GridCountX++;
                GridCountZ++;
                break;
            case OceanRenderSetting.TileType.FatXOuter:
                GridCountX++;
                bIsOutertile = true;
                break;
            case OceanRenderSetting.TileType.FatXZOuter:
                GridCountX++;
                GridCountZ++;
                bIsOutertile = true;
                break;
            case OceanRenderSetting.TileType.Count:
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
}
