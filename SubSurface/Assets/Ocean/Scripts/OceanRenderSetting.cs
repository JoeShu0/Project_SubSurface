using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Custom Ocean Setting")]
public class OceanRenderSetting : ScriptableObject
{

    [Tooltip("Tick this box will regenerate All RTs and Meshtiles based on Settings")]
    public bool IsInit = false;

    //Shader to render ocean
    public Shader oceanShader;
    //Shader to render the displacement and normal maps
    public ComputeShader shapeShader; 

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

    //wind angle causing the waves(degrees)
    public float AnimeWindAngle = 0.0f;
    //Wave dir distribute(degrees)
    public float WaveDirAngleRange = 90.0f;
    //Wave Length range in meters
    public Vector2 WaveLengthRange = new Vector2(128.0f, 0.25f);

    //Foam fade Time in secs
    [Range(0.0f, 3.0f)]
    public float FoamFadeTime = 1.0f;

    //tweaker for wave datas
    [Range(0.0f, 3.0f)]
    public float[] WaveAmplitudeTweak = new float[8];

    public float debug = 11;

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

    [System.Serializable]
    public struct WaveData
    {
        public float WaveLength;
        public float Amplitude;
        public float Speed;
        public Vector2 Direction;
    }

    [Tooltip("Tick this box will regenerate All WaveDatas")]
    public bool RegenerateWaveDatas = true;
    [SerializeField]
    public WaveData[] SpectrumWaves;

    [Tooltip("Tick this box will regenerate All Meshtiles ")]
    public bool RegenerateTileMeshes = true;
    //All th tile types
    public Mesh[] TileMeshes = new Mesh[(int)TileType.Count];

    [Tooltip("Tick this box will regenerate All RT based on settings ")]
    public bool RegenerateRenderTextures = true;
    //anim wave render texture
    public RenderTexture[] LODDisplaceMaps;
    public RenderTexture[] LODNormalMaps;
    public RenderTexture[] LODDerivativeMaps;

    [Tooltip("Tick this box will regenerate All Materials for LODs ")]
    public bool RegenerateMODMaterials = true;
    //All ocean Materials
    public Material[] OceanMats;

    static int displaceTexId = Shader.PropertyToID("_DispTex");
    static int displaceTexNextId = Shader.PropertyToID("_NextDispTex");
    static int normalTexId = Shader.PropertyToID("_NormalTex");
    static int normalTexNextId = Shader.PropertyToID("_NextLODNTex");

    private void Awake()
    {
        ReInitLODRTs();
    }

    void ReInitLODRTs()
    {
        for (int i = 0; i < LODCount; i++)
        {
            LODDisplaceMaps[i].enableRandomWrite = true;
            LODNormalMaps[i].enableRandomWrite = true;
            LODDerivativeMaps[i].enableRandomWrite = true;
        }
    }
#if UNITY_EDITOR
    public void Initialization()
    {
        InitLODRTs();
        InitTiles();
        InitMaterials();

        if (!oceanShader)
        {
            oceanShader = Shader.Find("Custom_RP/OceanShader");
        }
        if (!shapeShader)
        {
            //shapeShader = (ComputeShader)Resources.Load("OceanShapeShader");
        }
        IsInit = true;

        InitOceanWaves();
    }


    private void OnValidate()
    {

        if (!IsInit)
        {
            //Incase we need to regenerate the mesh an the rendertexture
            Initialization();
        }
        if (RegenerateWaveDatas)
        {
            InitOceanWaves();
            RegenerateWaveDatas = false;
        }
        if (RegenerateTileMeshes)
        {
            InitTiles();
            RegenerateTileMeshes = false;
        }
        if (RegenerateRenderTextures)
        {
            InitLODRTs();
            RegenerateRenderTextures = false;
        }
        if (RegenerateMODMaterials)
        {
            InitMaterials();
            RegenerateMODMaterials = false;
        }

    }


    private void InitOceanWaves()
    {
        
        
        SpectrumWaves = new WaveData[WaveCount];
        
        int GroupCount = Mathf.FloorToInt(Mathf.Log(Mathf.FloorToInt(WaveLengthRange.x), 2)) + 1;
        int WavePerGroup = Mathf.FloorToInt(WaveCount / GroupCount);

        int index = 0;

        //float G_MaxWL = Mathf.Pow(2, GroupCount - 1);

        for (int i = 0; i < GroupCount; i++)
        {
            float Max_WaveLength = Mathf.Pow(2, i);
            float Min_WaveLength = i == 0 ? WaveLengthRange.y : Mathf.Pow(2, i - 1);
            for (int n = 0; n < WavePerGroup; n++)
            {
                index = i * WavePerGroup + n;
                //Debug.Log(index);
                if (index < WaveCount)
                {
                    WaveData newWaveData = new WaveData();

                    newWaveData.WaveLength = Mathf.Lerp(Min_WaveLength, Max_WaveLength, UnityEngine.Random.Range(0.1f, 1.0f));
                    newWaveData.Amplitude = newWaveData.WaveLength * 0.005f;
                    float DirAngleDeg = UnityEngine.Random.Range(-1.0f, 1.0f) * WaveDirAngleRange + AnimeWindAngle;
                    newWaveData.Direction = new Vector2(
                        (float)Mathf.Cos(Mathf.Deg2Rad * DirAngleDeg), 
                        (float)Mathf.Sin(Mathf.Deg2Rad * DirAngleDeg)
                        );
                    //DirX[index] = (float)Mathf.Cos(Mathf.Deg2Rad * DirAngleDegs[index]);
                    //DirZ[index] = (float)Mathf.Sin(Mathf.Deg2Rad * DirAngleDegs[index]);
                    newWaveData.Speed = Mathf.Sqrt(9.8f / 2.0f / 3.14159f * newWaveData.WaveLength);
                    SpectrumWaves[index] = newWaveData;
                }

            }
        }

        if (index < WaveCount - 1)
        {
            Debug.Log("waves not filled");
            for (int n = index + 1; n < WaveCount; n++)
            {
                SpectrumWaves[n].WaveLength = Mathf.Lerp(WaveLengthRange.x, WaveLengthRange.x, UnityEngine.Random.Range(0.0f, 1.0f));
                SpectrumWaves[n].Amplitude = 0.0f;
                SpectrumWaves[n].Direction = new Vector2(0.0f,1.0f);
                SpectrumWaves[n].Speed = 0.0f;
            }
        }

        
    }

    void InitMaterials()
    {
        OceanMats = new Material[LODCount];

        for(int i=0;i< LODCount; i++)
        {
            OceanMats[i] = new Material(oceanShader);
            string MatPath = string.Format("Assets/Ocean/OceanAssets/Material_LOD{0}.mat", i);
            OceanMats[i].enableInstancing = true;
            OceanMats[i].SetTexture(displaceTexId, LODDisplaceMaps[i]);
            OceanMats[i].SetTexture(displaceTexNextId, LODDisplaceMaps[Mathf.Min(i+1, LODCount-1)]);
            OceanMats[i].SetTexture(normalTexId, LODNormalMaps[i]);
            OceanMats[i].SetTexture(normalTexNextId, LODNormalMaps[Mathf.Min(i + 1, LODCount - 1)]);
            AssetDatabase.DeleteAsset(MatPath);
            AssetDatabase.CreateAsset(OceanMats[i], MatPath);
        }
    }

    void InitLODRTs()
    {
        LODDisplaceMaps = new RenderTexture[LODCount];
        LODNormalMaps = new RenderTexture[LODCount];
        LODDerivativeMaps = new RenderTexture[LODCount];
        string RTPath;
        for (int i = 0; i < LODDisplaceMaps.Length; i++)
        {
            RTPath = string.Format("Assets/Ocean/OceanAssets/DisplacementMap_LOD{0}.renderTexture", i);
            AssetDatabase.DeleteAsset(RTPath);

            RenderTexture RT = new RenderTexture(RTSize, RTSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            
            //RT = (RenderTexture)AssetDatabase.LoadAssetAtPath(RTPath,typeof(RenderTexture));
            LODDisplaceMaps[i] = RT;
            LODDisplaceMaps[i].enableRandomWrite = true;
            LODDisplaceMaps[i].antiAliasing = 1;
            //LODDisplaceMaps[i].bindTextureMS = true;
            LODDisplaceMaps[i].wrapMode = TextureWrapMode.Clamp;
            LODDisplaceMaps[i].filterMode = FilterMode.Trilinear;
            LODDisplaceMaps[i].Create();
            AssetDatabase.CreateAsset(RT, RTPath);

            RTPath = string.Format("Assets/Ocean/OceanAssets/NormalMap_LOD{0}.renderTexture", i);
            AssetDatabase.DeleteAsset(RTPath);

            RT = new RenderTexture(RTSize, RTSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            
            //RT = (RenderTexture)AssetDatabase.LoadAssetAtPath(RTPath, typeof(RenderTexture));
            LODNormalMaps[i] = RT;
            LODNormalMaps[i].enableRandomWrite = true;
            LODNormalMaps[i].antiAliasing = 1;
            LODNormalMaps[i].wrapMode = TextureWrapMode.Clamp;
            LODNormalMaps[i].filterMode = FilterMode.Trilinear;
            LODNormalMaps[i].Create();
            AssetDatabase.CreateAsset(RT, RTPath);

            RTPath = string.Format("Assets/Ocean/OceanAssets/DerivativeMap_LOD{0}.renderTexture", i);
            AssetDatabase.DeleteAsset(RTPath);

            RT = new RenderTexture(RTSize, RTSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

            //RT = (RenderTexture)AssetDatabase.LoadAssetAtPath(RTPath, typeof(RenderTexture));
            LODDerivativeMaps[i] = RT;
            LODDerivativeMaps[i].enableRandomWrite = true;
            LODDerivativeMaps[i].antiAliasing = 1;
            LODDerivativeMaps[i].wrapMode = TextureWrapMode.Clamp;
            LODDerivativeMaps[i].filterMode = FilterMode.Trilinear;
            LODDerivativeMaps[i].Create();
            AssetDatabase.CreateAsset(RT, RTPath);
        }
    }

    //this is for restore some of the settings for RTs


    void InitTiles()
    {
        //generate all th tile types 
        for (int i = 0; i < (int)TileType.Count; i++)
        {
            Mesh TileMesh = GenerateTile((OceanRenderSetting.TileType)i, GridSize, GridCountPerTile);
            string MeshPath = string.Format("Assets/Ocean/OceanAssets/TileMesh_{0}.asset", ((OceanRenderSetting.TileType)i).ToString());
            AssetDatabase.DeleteAsset(MeshPath);
            AssetDatabase.CreateAsset(TileMesh, MeshPath);

            TileMeshes[i] = (Mesh)AssetDatabase.LoadAssetAtPath(MeshPath, typeof(Mesh));
            //Debug.Log(((OceanRenderSetting.TileType)i).ToString());
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
#endif
}
