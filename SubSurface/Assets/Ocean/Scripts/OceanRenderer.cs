using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static OceanRenderSetting;

[ExecuteInEditMode]
public class OceanRenderer :MonoBehaviour
{

    //[HideInInspector]
    public OceanRenderSetting ORS;
    //[HideInInspector]
    public OceanShadingSetting OSS;
    //[HideInInspector]
    public OceanHeightSampler OHS;

    [Tooltip("Tick this box will regenerate All LOD meshes and Materials")]
    public bool hasOceanLOD = false;
    public Camera OceanCam;
    private int threadGroupX, threadGroupY;

    private GameObject[] OceanLODS;
    //private Material[] OceanMATS;
    //temp solution
    private int KIndex = 0;

    //private ComputeBuffer WaveParticleBuffer;
    //private ComputeBuffer WaveParticleParamsBuffer;
    RenderTexture temppointframe;

    //*****Ocean Render Shader LOD related*****
    int gridSizeId = Shader.PropertyToID("_GridSize");
    int lodIndexId = Shader.PropertyToID("_LODIndex");
    int transitionParams = Shader.PropertyToID("_TransitionParam");
    int lodSizeId = Shader.PropertyToID("_LODSize");
    int lodDisplaceMapId = Shader.PropertyToID("_DisplaceMap");
    int lodNormalMapId = Shader.PropertyToID("_NormalMap");
    int lodNextDisplaceMapId = Shader.PropertyToID("_NextDisplaceMap");
    int lodNextNormalMapId = Shader.PropertyToID("_NextNormalMap");

    //*****Ocean Render Shading related*****
    int baseColorId = Shader.PropertyToID("_BaseColor");
    int brightColorId = Shader.PropertyToID("_BrightColor");
    int darkColorId = Shader.PropertyToID("_DarkColor");
    int foamColorId = Shader.PropertyToID("_FoamColor");
    int fresnelColorId = Shader.PropertyToID("_FresnelColor");
    int hightLightParamId = Shader.PropertyToID("_HightParams");
    

    //******Ocean RT render Shader******
    int waveCountId = Shader.PropertyToID("_WaveCount");
    int waveBufferId = Shader.PropertyToID("_WavesBuffer");
    //int lodSizeId = Shader.PropertyToID("_LODSize");
    int timeId = Shader.PropertyToID("_Time");
    int deltaTimeId = Shader.PropertyToID("_DeltaTime");
    int foamParamId = Shader.PropertyToID("_FoamParams");
    int lodWaveAmpMulId = Shader.PropertyToID("_LODWaveAmpMul");
    int lodBaseDispMapId = Shader.PropertyToID("_BaseDisplace");
    //int lodBaseNormalMapId = Shader.PropertyToID("_BaseNormal");
    int lodBaseDerivativeMapId = Shader.PropertyToID("_BaseDerivativeMap");
    int lodBaseDerivativeMapId_S = Shader.PropertyToID("_BaseDerivativeMap_Sample");
    int lodParamsId = Shader.PropertyToID("_LODParams");

    //Global params
    int centerPosId = Shader.PropertyToID("_CenterPos");
    //int CameraProjParamsId = Shader.PropertyToID("_CamProjectionParams");



    //*****this class is a singleton*****
    private static OceanRenderer _instance;
    public static OceanRenderer Instance
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
        //ORS.LODDisplaceMaps = new RenderTexture[ORS.LODCount]
        //ORS.Initialization()
        //CreateOceanLODs();

        //init ocean Cam
        OceanCam = Camera.main;

        //init the Ocean sampler
        OHS = new OceanHeightSampler(ORS, transform, ORS.getHeight);
        

        //the render RT part should be moved into a separate class
        this.threadGroupX = threadGroupY = Mathf.CeilToInt(ORS.RTSize / 32.0f);
    }

    private void OnEnable()
    {
        //SetWaveParticlesBuffer();
        //print("ORR Enabled!");
        temppointframe =  new RenderTexture(512, 512, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
        temppointframe.enableRandomWrite = true;
        temppointframe.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        temppointframe.volumeDepth = 8;
        temppointframe.Create();
    }

    private void Start()
    {
        ORS.WaveParticleEnd = 0;
        /*
        WaveParticle nWP = new WaveParticle();
        nWP.BirthTime = Time.time;
        nWP.Amplitude = 10.0f;
        nWP.DispersionAngle = 180.0f;
        nWP.Origin = new Vector2(0.0f, 0.0f);
        nWP.Direction = new Vector2(1.0f, 0.0f);
        SpawnWaveParticles(nWP);

        nWP.Amplitude = -10.0f;
        nWP.Direction = new Vector2(-1.0f, 0.0f);
        SpawnWaveParticles(nWP);
        */
    }

    private void OnDisable()
    {
        //print("ORR disabled!");
        //ReleaseWaveParticlesBuffer();
    }

    private void OnDrawGizmos()
    {
        
        for (int i = 0; i < ORS.WaveParticleCount; i++)
        {
            Vector2 position = ORS.WaveParticles[i].Origin + 
                ORS.WaveParticles[i].Direction * OceanRenderSetting.WaveParticleSpeed * 
                (Time.time - ORS.WaveParticles[i].BirthTime);

            Gizmos.DrawSphere(new Vector3(position.x,0.0f, position.y), 0.1f);
        }
        
    }
    private void Update()
    {
#if UNITY_EDITOR


        if(!hasOceanLOD)
            CreateOceanLODs();
        if (!Application.isPlaying)
        {
            UpdateOceantransform();
            RenderDisAndNormalMapsForLODs();
            //RenderWaveParticlesForLODs();
            RenderNormalForLODs();

            
        }
        
#endif
        //RenderDisAndNormalMapsForLODs();
        
        UpdateShaderGlobalParams();

       
    }



    private void FixedUpdate()
    {
        UpdateOceantransform();
        RenderDisAndNormalMapsForLODs();
        RenderWaveParticlesForLODs();
        RenderNormalForLODs();

        UpdateWaveParticles();
        // replave corotine with asnc await later
        if (!OHS.IsRetrivingGPUData)
        {
            StartCoroutine(OHS.GetRelativeDepth());
        }
    }

    private void LateUpdate()
    {
        
        //Debug.Log(ORS.CurPastOceanScale[0]);
    }

    void UpdateOceantransform()//update in fixed update
    {
        //calculate ocean scale and position based on the camera
        Vector3 CameraFacing = OceanCam.transform.forward;
        Vector3 CameraPosition = OceanCam.transform.position;
        Vector3 OceanPostion = CameraPosition + CameraFacing * ORS.OceanCamExtend;
        int heightStage = Mathf.CeilToInt(Mathf.Max(Mathf.Log(Mathf.Abs(CameraPosition.y) / ORS.OceanCamHeightStage, 2), 0.1f) ) -1;

       
        //record last frame pos and scale(pay attention to time step(normally in fixed update))
        ORS.CurPastOceanScale[1] = ORS.CurPastOceanScale[0];
        ORS.CurPastOceanScale[0] = new Vector2(heightStage, (int)Mathf.Pow(2, heightStage));
        ORS.CurAndPastPos[1] = ORS.CurAndPastPos[0];
        ORS.CurAndPastPos[0] = new Vector2(OceanPostion.x, OceanPostion.z);

        //setOceanScale transition
        float ScaleTransition = (Mathf.Abs(CameraPosition.y) - ORS.CurPastOceanScale[0].y * ORS.OceanCamHeightStage) / (ORS.CurPastOceanScale[0].y * ORS.OceanCamHeightStage);
        ORS.OceanScaleTransition = ScaleTransition;

        //update Ocean position
        gameObject.transform.position = new Vector3(OceanPostion.x, gameObject.transform.position.y, OceanPostion.z);
        //update ocean gameobject scale
        gameObject.transform.localScale = new Vector3(ORS.CurPastOceanScale[0].y, ORS.CurPastOceanScale[0].y, ORS.CurPastOceanScale[0].y);
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

        //LODMat.SetTexture("_DispTexArray", ORS.LODDisplaceMapsArray);

        //LODMat.SetTexture(lodDisplaceMapId, ORS.LODDisplaceMaps[LODIndex]);
        //LODMat.SetTexture(lodNormalMapId, ORS.LODNormalMaps[LODIndex]);
        //LODMat.SetTexture(lodNextDisplaceMapId, ORS.LODDisplaceMaps[Mathf.Min(LODIndex, ORS.LODCount)]);
        //LODMat.SetTexture(lodNextNormalMapId, ORS.LODNormalMaps[Mathf.Min(LODIndex, ORS.LODCount)]);

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
            CTile.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            //CTile.GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }

        LOD.transform.localScale = new Vector3(LODScale, 1, LODScale);
        return LOD;
    }

 

    void UpdateShaderGlobalParams()
    {
        //update ocean rendering Global Material properties
        //center position for the ocean(following camerafront)
        Shader.SetGlobalVector("_OceanScaleParams", new Vector4(ORS.CurPastOceanScale[0].x, ORS.CurPastOceanScale[0].y, ORS.OceanScaleTransition, 0.0f));
        //Shader.SetGlobalFloat("_LODCount", ORS.OceanScaleTransition);

        Shader.SetGlobalVector(centerPosId, transform.position);
        //Ocean shanding stuff
        Shader.SetGlobalVector(hightLightParamId, 
            new Vector4(OSS.highlights.HighLightExp, 
            OSS.highlights.HighLightBost, 0.0f,0.0f));
        
        Shader.SetGlobalColor(baseColorId, OSS.Base.color);
        Shader.SetGlobalColor(brightColorId, OSS.Bright.color);
        Shader.SetGlobalColor(darkColorId, OSS.Dark.color);
        Shader.SetGlobalColor(foamColorId, OSS.Foam.color);
        Shader.SetGlobalColor(fresnelColorId, OSS.Fresnel.color);
        Shader.SetGlobalColor("_SSSColor", OSS.SSS.color);
        //Shader.SetGlobalVectorArray
        Shader.SetGlobalVector("_BandingOffsetPow", new Vector4(
            OSS.Base.bandingOffset,
            OSS.Base.bandingPower,
            OSS.Bright.bandingOffset,
            OSS.Bright.bandingPower
            ));
        Shader.SetGlobalVector("_FoamFresnelOffsetPow", new Vector4(
            OSS.Foam.bandingOffset,
            OSS.Foam.bandingPower,
            OSS.Fresnel.bandingOffset,
            OSS.Fresnel.bandingPower
            ));

        Shader.SetGlobalVector("_SSSOffsetPow", new Vector4(
            OSS.SSS.bandingOffset,
            OSS.SSS.bandingPower,
            0.0f,0.0f
            ));

        Shader.SetGlobalTexture("_DispTexArray", ORS.LODDisplaceMapsArray);
        Shader.SetGlobalTexture("_NormalTexArray", ORS.LODNormalMapsArray);
        
        Shader.SetGlobalTexture("_FoamTrailTexture", OSS.FoamTrailTex);

        //Shader.SetGlobalTexture(detailNormalId, ORS.OceanDetailNoise);
    }


    void RenderDisAndNormalMapsForLODs()//update in fixed update
    {
        if (!ORS.shapeGerstnerShader)
        {
            Debug.LogError("No Shapeshader in Ocean rendeing settings!!");
            return;
        }

        

        //new ComputeBuffer with the stride is 20 ??
        ComputeBuffer shapeWaveBufer = new ComputeBuffer(ORS.WaveCount, 20);

        ORS.shapeGerstnerShader.SetFloats(centerPosId, new float[]
            { transform.position.x, transform.position.y, transform.position.z}
            );

        ORS.shapeGerstnerShader.SetFloat(timeId, Time.time);
        ORS.shapeGerstnerShader.SetFloat(deltaTimeId, Time.deltaTime);
        //this function should be call in fixed update
        float inverstime = 1 / ORS.foamParams.FadeTime * Time.fixedDeltaTime;
        ORS.shapeGerstnerShader.SetVector(foamParamId, 
            new Vector4(inverstime, ORS.foamParams.BandOffset,ORS.foamParams.BandPower, 0.0f));
        
        ORS.shapeGerstnerShader.SetVector("_CurPastPos", 
            new Vector4(
                ORS.CurAndPastPos[0].x, ORS.CurAndPastPos[0].y,
                ORS.CurAndPastPos[1].x, ORS.CurAndPastPos[1].y));

        ORS.shapeGerstnerShader.SetVector("_CurPastScale",
            new Vector4(
                ORS.CurPastOceanScale[0].x, ORS.CurPastOceanScale[0].y,
                ORS.CurPastOceanScale[1].x, ORS.CurPastOceanScale[1].y));

        ORS.shapeGerstnerShader.SetTexture(KIndex, "_DisplaceArray", ORS.LODDisplaceMapsArray);
        ORS.shapeGerstnerShader.SetTexture(KIndex, "_DerivativeArray", ORS.LODDerivativeMapsArray);
        ORS.shapeGerstnerShader.SetTexture(KIndex, "_NormalArray", ORS.LODNormalMapsArray);
        ORS.shapeGerstnerShader.SetTexture(KIndex, "_VelocityArray", ORS.LODVelocityMapsArray);

        ORS.shapeGerstnerShader.SetInt(waveCountId, ORS.WaveCount);
        shapeWaveBufer.SetData(ORS.SpectrumWaves);
        //this buffer is persistent on GPU and no modification needed
        //should change this to only set and release ONCE
        ORS.shapeGerstnerShader.SetBuffer(KIndex, waveBufferId, shapeWaveBufer);
        for (int i = ORS.LODCount-1; i>=0; i--)
        {
            //Graphics.CopyTexture(ORS.LODDisplaceMapsArray, Temp);
            //each LOD now only calculate  WaveCount/LODCcount of waves
            //ORS.shapeShader.SetInt(waveCountId, WavePerLOD);
            //ORS.shapeShader.SetInt(waveCountId, ORS.WaveCount-i* WavePerLOD);
            //WaveData[] WaveSubsets = ORS.SpectrumWaves.Skip(WavePerLOD * i).ToArray();
            //shapeWaveBufer.SetData(ORS.SpectrumWaves);
            //int SkipNum = Mathf.Min(WavePerLOD * i + Mathf.CeilToInt(Mathf.Log(ORS.OceanScale, 2)) * WavePerLOD, ORS.WaveCount - WavePerLOD);
            

            float CurrentLODSize = ORS.GridSize * ORS.GridCountPerTile * 4 * Mathf.Pow(2, i) * ORS.CurPastOceanScale[0].y;//times ocean scale
            //ORS.shapeShader.SetFloat(lodSizeId, CurrentLODSize);
            //ORS.shapeShader.SetInt(lodIndexId, i);
            ORS.shapeGerstnerShader.SetVector(lodParamsId,
                new Vector4(ORS.LODCount, i, CurrentLODSize, 0.0f));

            ORS.shapeGerstnerShader.SetFloat(lodWaveAmpMulId, ORS.WaveAmplitudeTweak[i]);

            

            //ORS.shapeShader.SetTexture(KIndex, lodBaseDispMapId, ORS.LODDisplaceMaps[Mathf.Min(i + 1, ORS.LODCount - 1)]);
            //ORS.shapeShader.SetTexture(KIndex, lodBaseNormalMapId, ORS.LODNormalMaps[Mathf.Min(i + 1, ORS.LODCount - 1)]);

            //ORS.shapeShader.SetTexture(KIndex, lodDisplaceMapId, ORS.LODDisplaceMaps[i]);
            //ORS.shapeShader.SetTexture(KIndex, lodNormalMapId, ORS.LODNormalMaps[i]);

            //ORS.shapeShader.SetTexture(KIndex, lodBaseDerivativeMapId, ORS.LODDerivativeMaps[i]);
            //ORS.shapeShader.SetTexture(KIndex, lodBaseDerivativeMapId_S, ORS.LODDerivativeMaps[Mathf.Min(i + 1, ORS.LODCount - 1)]);

            //ORS.shapeShader.SetTexture(KIndex, "NoiseFoam", WaterFoamNoise);

            ORS.shapeGerstnerShader.Dispatch(KIndex, threadGroupX, threadGroupY, 1);
        }

        shapeWaveBufer.Release();
        //DerivativeMap.Release();
        //BaseDerivativeMap.Release();

        
    }
    /*
    void SetWaveParticlesBuffer()
    {
        //this buffer is persistent on GPU and no modification needed
        //should change this to only set and release ONCE
        WaveParticleBuffer = new ComputeBuffer(ORS.WaveParticleCount, 32);
        WaveParticleBuffer.SetData(ORS.WaveParticles);
        ORS.shapeWaveParticleShader.SetInt("_WaveParticleCount", ORS.WaveParticleCount);
        ORS.shapeWaveParticleShader.SetBuffer(0, "_WaveParticleBuffer", WaveParticleBuffer);

        WaveParticleParamsBuffer = new ComputeBuffer(1, 8);
        WaveParticleParamsBuffer.SetData( new Vector2[] { new Vector2(0.0f, 1.0f) });
        ORS.shapeWaveParticleShader.SetBuffer(0, "_WaveParticleParamsBuffer", WaveParticleParamsBuffer);
    }

    void ReleaseWaveParticlesBuffer()
    {
        WaveParticleBuffer.Release();
        WaveParticleParamsBuffer.Release();
    }
    */
    void RenderWaveParticlesForLODs()
    {
        
        ComputeBuffer shapeWaveParticleBuffer = new ComputeBuffer(ORS.WaveParticleCount, 32);
        ORS.shapeWaveParticleShader.SetInt("_WaveParticleCount", ORS.WaveParticleCount);
        shapeWaveParticleBuffer.SetData(ORS.WaveParticles);
        ORS.shapeWaveParticleShader.SetBuffer(0, "_WaveParticleBuffer", shapeWaveParticleBuffer);

        ORS.shapeWaveParticleShader.SetVector("_TimeParams", new Vector4(Time.time, Time.fixedDeltaTime, 0.0f, 0.0f));

        ORS.shapeWaveParticleShader.SetTexture(0, "_DisplaceArray", ORS.LODDisplaceMapsArray);
        ORS.shapeWaveParticleShader.SetTexture(0, "_PointFrame", temppointframe);

        ORS.shapeWaveParticleShader.SetTexture(1, "_DisplaceArray", ORS.LODDisplaceMapsArray);
        ORS.shapeWaveParticleShader.SetTexture(1, "_PointFrame", temppointframe);
        ORS.shapeWaveParticleShader.SetTexture(1, "_WaveParticleArray", ORS.LODWaveParticleMapsArray);

        ORS.shapeWaveParticleShader.SetTexture(2, "_DisplaceArray", ORS.LODDisplaceMapsArray);
        ORS.shapeWaveParticleShader.SetTexture(2, "_WaveParticleArray", ORS.LODWaveParticleMapsArray);

        ORS.shapeWaveParticleShader.SetTexture(3, "_DisplaceArray", ORS.LODDisplaceMapsArray);
        ORS.shapeWaveParticleShader.SetTexture(3, "_WaveParticleArray", ORS.LODWaveParticleMapsArray);
        ORS.shapeWaveParticleShader.SetTexture(3, "_DerivativeArray", ORS.LODDerivativeMapsArray);
        ORS.shapeWaveParticleShader.SetTexture(3, "_VelocityArray", ORS.LODVelocityMapsArray);

        ORS.shapeWaveParticleShader.SetTexture(4, "_PointFrame", temppointframe);

        ORS.shapeWaveParticleShader.SetFloats(centerPosId, new float[]
            { transform.position.x, transform.position.y, transform.position.z}
            );

        /*
        for (int i = 0; i < ORS.LODCount/ 2; i++)
        {

        }*/

        int WaveParticleLODs = ORS.LODCount / 2;

        float CurrentLODSize_L = ORS.GridSize * ORS.GridCountPerTile * 4 * ORS.CurPastOceanScale[0].y;
        ORS.shapeWaveParticleShader.SetVector(lodParamsId,
                    new Vector4(ORS.LODCount, 0, CurrentLODSize_L, 0.0f));
        ORS.shapeWaveParticleShader.Dispatch(0, ORS.WaveParticleCount / 128, 1, WaveParticleLODs);
        ORS.shapeWaveParticleShader.Dispatch(1, threadGroupX, threadGroupY, WaveParticleLODs);
        ORS.shapeWaveParticleShader.Dispatch(2, threadGroupX, threadGroupY, WaveParticleLODs);
        ORS.shapeWaveParticleShader.Dispatch(3, threadGroupX, threadGroupY, WaveParticleLODs);

        ORS.shapeWaveParticleShader.Dispatch(4, threadGroupX, threadGroupY, WaveParticleLODs);


        shapeWaveParticleBuffer.Release();


        
    }

    void RenderNormalForLODs()
    {
        ORS.shapeNormalShader.SetTexture(0, "_DisplaceArray", ORS.LODDisplaceMapsArray);
        ORS.shapeNormalShader.SetTexture(0, "_NormalArray", ORS.LODNormalMapsArray);
        ORS.shapeNormalShader.SetTexture(0, "_DerivativeArray", ORS.LODDerivativeMapsArray);
        ORS.shapeNormalShader.SetTexture(0, "_VelocityArray", ORS.LODVelocityMapsArray);

        float inverstime = 1 / ORS.foamParams.FadeTime * Time.fixedDeltaTime;
        ORS.shapeNormalShader.SetVector(foamParamId,
            new Vector4(inverstime, ORS.foamParams.BandOffset, ORS.foamParams.BandPower, 0.0f));

        ORS.shapeNormalShader.SetVector("_CurPastPos",
            new Vector4(
                ORS.CurAndPastPos[0].x, ORS.CurAndPastPos[0].y,
                ORS.CurAndPastPos[1].x, ORS.CurAndPastPos[1].y));

        ORS.shapeNormalShader.SetVector("_CurPastScale",
            new Vector4(
                ORS.CurPastOceanScale[0].x, ORS.CurPastOceanScale[0].y,
                ORS.CurPastOceanScale[1].x, ORS.CurPastOceanScale[1].y));

        float CurrentLOD0Size_L = ORS.GridSize * ORS.GridCountPerTile * 4  * ORS.CurPastOceanScale[0].y;
        ORS.shapeNormalShader.SetVector(lodParamsId,
            new Vector4(ORS.LODCount, 0, CurrentLOD0Size_L, 0.0f));

        /*
        for (int i = 0; i < ORS.LODCount; i++)
        {
            
            
        }*/

        ORS.shapeNormalShader.Dispatch(0, threadGroupX, threadGroupX, 8);

    }

    public void SpawnWaveParticles(WaveParticle newWP)
    {
        if (ORS.WaveParticleEnd >= ORS.WaveParticleCount)
        {
            ORS.WaveParticleEnd = ORS.WaveParticleEnd % ORS.WaveParticleCount;
        }
        ORS.WaveParticles[ORS.WaveParticleEnd] = newWP;
        ORS.WaveParticleEnd++;
        //Debug.Log("Add WP!");


    }

    void UpdateWaveParticles()
    {
        for (int i = 0; i < Mathf.Min(ORS.WaveParticleCount, ORS.WaveParticleEnd); i++)
        {
            float LifeTime = Time.time - ORS.WaveParticles[i].BirthTime;
            float Span = ORS.WaveParticles[i].DispersionAngle * Mathf.PI / 180.0f * OceanRenderSetting.WaveParticleSpeed * LifeTime;
            if (Span >= OceanRenderSetting.WaveParticleRadius * 0.5f)
            {
                float newDispersionAngle = ORS.WaveParticles[i].DispersionAngle / 3.0f;
                float newAmplitude = ORS.WaveParticles[i].Amplitude / 3.0f;

                Quaternion RotationLeft = Quaternion.Euler(0, newDispersionAngle, 0);
                Quaternion RotationRight = Quaternion.Euler(0, -newDispersionAngle, 0);
                Vector2 originalDirection = ORS.WaveParticles[i].Direction;
                Vector3 leftDirection3 = RotationLeft * new Vector3(originalDirection.x, 0, originalDirection.y);
                Vector3 rightDirection3 = RotationRight * new Vector3(originalDirection.x, 0, originalDirection.y);


                OceanRenderSetting.WaveParticle leftnewWP = new WaveParticle();
                leftnewWP.Amplitude = newAmplitude;
                leftnewWP.BirthTime = ORS.WaveParticles[i].BirthTime;
                leftnewWP.DispersionAngle = newDispersionAngle;
                leftnewWP.Origin = ORS.WaveParticles[i].Origin;
                leftnewWP.Direction = new Vector2(leftDirection3.x, leftDirection3.z);

                OceanRenderSetting.WaveParticle rightnewWP = new WaveParticle();
                rightnewWP.Amplitude = newAmplitude;
                rightnewWP.BirthTime = ORS.WaveParticles[i].BirthTime;
                rightnewWP.DispersionAngle = newDispersionAngle;
                rightnewWP.Origin = ORS.WaveParticles[i].Origin;
                rightnewWP.Direction = new Vector2(rightDirection3.x, rightDirection3.z);

                ORS.WaveParticles[i].Amplitude = newAmplitude;
                ORS.WaveParticles[i].DispersionAngle = newDispersionAngle;
                //ORS.WaveParticles[i].BirthTime = Time.time;

                /*
                ORS.WaveParticles[ORS.WaveParticleEnd] = leftnewWP;
                ORS.WaveParticles[ORS.WaveParticleEnd] = rightnewWP;

                ORS.WaveParticleEnd += 2;
                */
                SpawnWaveParticles(leftnewWP);
                SpawnWaveParticles(rightnewWP);

                if (ORS.WaveParticleEnd >= ORS.WaveParticleCount)
                {
                    ORS.WaveParticleEnd = ORS.WaveParticleEnd % ORS.WaveParticleCount;
                }
            }

            
        }

        //Debug.Log("WaveParticle Count: " + ORS.WaveParticleEnd);
    }
}
