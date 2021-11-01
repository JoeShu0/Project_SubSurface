using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
public class Shadows
{
    //int debug = 1;
    const string buffername = "Shadows";

    static string[] directionalFilterKeywords = {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };

    static string[] otherFilterKeywords = {
        "_OTHER_PCF3",
        "_OTHER_PCF5",
        "_OTHER_PCF7",
    };

    static string[] cascadeBlendKeywords = {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER",
    };

    CommandBuffer buffer = new CommandBuffer {
        name = buffername
    };

    ScriptableRenderContext context;
    CullingResults cullingResults;
    ShadowSettings shadowSettings;

    const int
        maxShadowedDirectionalLightCount = 4, maxShadowedOtherLightCount = 16;
    const int maxCascades = 4;

    int ShadowedDirectionalLightCount, ShadowedOtherLightCount;

    static int
        dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
        dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
        otherShadowAtlasId = Shader.PropertyToID("_OtherShadowAltas"),
        otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices"),
        otherShadowTilesId = Shader.PropertyToID("_OtherShadowTiles"),

        cascadeCountId = Shader.PropertyToID("_CascadeCount"),
        cascadeCullingSphereId = Shader.PropertyToID("_CascadeCullingSpheres"),
        cascadeDataId = Shader.PropertyToID("_CascadeData"),

        shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize"),
        shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade"),

        shadowPancakingId = Shader.PropertyToID("_ShadowPancaking");

    static Vector4[]
        cascadeCullingSpheres = new Vector4[maxCascades],
        cascadeData = new Vector4[maxCascades],
        otherShadowTiles = new Vector4[maxShadowedOtherLightCount];

    static Matrix4x4[]
        dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades],
        otherShadowMatrices = new Matrix4x4[maxShadowedOtherLightCount];

    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
        public float slopScaleBias;//this will control the depth bias slop value for tweaking per light
        public float nearPlaneOffset;// this will pull the near plane(of light) back a bit, helps reduce long obj shadow deforming    
    }

    ShadowedDirectionalLight[] ShadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    struct ShadowedOtherLight
    {
        public int visibleLightIndex;
        public float slopeScaleBias;
        public float normalBias;
        public bool isPoint;
    };

    ShadowedOtherLight[] shadowedOtherLights = new ShadowedOtherLight[maxShadowedOtherLightCount];

    static string[] shadowMaskKeywords =
        {
        "_SHADOW_MASK_ALWAYS",
        "_SHADOW_MASK_DISTANCE"
    };

    bool useShadowMask;

    Vector4 atlasSizes;

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;
        this.context = context;
        this.shadowSettings = shadowSettings;

        //init ans not use shadowmask
        useShadowMask = false;

        ShadowedDirectionalLightCount = 0;
        ShadowedOtherLightCount = 0;
        /*
        buffer.BeginSample(buffername);
        SetupLights();
        buffer.EndSample(buffername);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
        */
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public Vector4 ReserveDirectioanlShadows(Light light, int visibleLightIndex)
    {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None && light.shadowStrength > 0f //&&
            //cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)
            )
        //the getshadowCasterBound will return false if the light does not effect any oject(can cast shadow) in shadow range
        {
            float maskChannel = -1;
            //we will decide wether to use the shadow mask depend on if the lights are using thi shadow mask
            LightBakingOutput lightBaking = light.bakingOutput;
            if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
               lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel;
            }

            //Inform Shader the light does not affect anything, used for baked light attenuation
            if (!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
            {
                return new Vector4(-light.shadowStrength, 0f, maskChannel);
            }
            
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] =
                new ShadowedDirectionalLight {
                    visibleLightIndex = visibleLightIndex,
                    slopScaleBias = light.shadowBias,
                    nearPlaneOffset = light.shadowNearPlane
                };
            return new Vector4(light.shadowStrength,
                shadowSettings.directional.cascadeCount * ShadowedDirectionalLightCount++, 
                light.shadowNormalBias,
                maskChannel);
        }
        else
        {
            return new Vector4(0f, 0f, 0f, -1f);
        }
    }

    public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
    {
        if (light.shadows == LightShadows.None || light.shadowStrength <= 0f)
        {
            return new Vector4(0f, 0f, 0f, -1f);
        }//Do not reserve lights with no baked and realtime shadow

        float maskChannel = -1f;
        LightBakingOutput lightBaking = light.bakingOutput;
        if (
            lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
            lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask
        ) {
            useShadowMask = true;
            maskChannel = lightBaking.occlusionMaskChannel;
        }

        bool isPoint = light.type == LightType.Point;
        int newLightCount = ShadowedOtherLightCount + (isPoint ? 6 : 1);
        if (newLightCount >= maxShadowedOtherLightCount ||
            !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
        }

        shadowedOtherLights[ShadowedOtherLightCount] = new ShadowedOtherLight{
            visibleLightIndex = visibleLightIndex,
            slopeScaleBias = light.shadowBias,
            normalBias = light.shadowNormalBias,
            isPoint = isPoint};

        Vector4 data = new Vector4(
                light.shadowStrength, ShadowedOtherLightCount,
                isPoint ? 1f : 0f ,
                maskChannel) ;

        ShadowedOtherLightCount = newLightCount;
        return data;
    }
    
    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            //prevent default RT problem in WebGL2.0
            buffer.GetTemporaryRT(dirShadowAtlasId, 1, 1,
            32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }

        if (ShadowedOtherLightCount > 0)
        {
            RenderOtherShadows();
        }
        else
        {
            buffer.SetGlobalTexture(otherShadowAtlasId, dirShadowAtlasId);
        }

        //refresh the shandowmask keyword every frame
        buffer.BeginSample(buffername);
        //Set ShadowMask distance or mask or "-1" disable all
        SetKeywords(shadowMaskKeywords, useShadowMask ? 
            QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : 1);
        
        //This part is moved from RenderDirectional shadow to here to ensure incase of not directional light
        //we still have fade settings for 
        buffer.SetGlobalInt(cascadeCountId,
            ShadowedDirectionalLightCount > 0 ? shadowSettings.directional.cascadeCount : 0);
        //set the shadowdistance to GPU to help reduce shadow strength to 0 outside the distance
        //otherwise the sample is cut based on culling sphere,which is extend beyond the distance
        //also add distance fade and cascade sphere fade, both using (1-d/max)/f, pass the max and f inverted
        //Set this in render to provide Fade for other lights shadows in case there is no directional light
        float f = 1f - shadowSettings.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeId,
            new Vector4(1 / shadowSettings.maxDistance,
            1 / shadowSettings.distanceFade,
            1f / (1f - f * f))
            );

        buffer.SetGlobalVector(shadowAtlasSizeId, atlasSizes);

        buffer.EndSample(buffername);
        ExecuteBuffer();
        
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int)shadowSettings.directional.atlasSize;
        //buffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
        atlasSizes.x = atlasSize;
        atlasSizes.y = 1f / atlasSize;
        //create temp rendertex to save shadow depth tex
        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize,
            32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        //order GPU to render on this RT, Load.dontcare since we goingto write on it, We need oit to store
        buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //clear the buffer for rendering
        buffer.ClearRenderTarget(true, false, Color.clear);
        //Pancaking is only useful in directional shadow rendering
        buffer.SetGlobalFloat(shadowPancakingId, 1f);
        //begin profile
        buffer.BeginSample(buffername);
        ExecuteBuffer();

        //if more than 1 dir light(MAX 4), the split should be 2(sqrt2)
        int tiles = ShadowedDirectionalLightCount * shadowSettings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for (int i = 0; i < ShadowedDirectionalLightCount;i++)
        {
            //call Render DirShadows with spilt and tilesize
            RenderDirectionalShadows(i, split, tileSize);

        }

        //set cascadecount and cascade culling sphere to GPU
        //move the count set to Render
        //buffer.SetGlobalInt(cascadeCountId, shadowSettings.directional.cascadeCount);
        buffer.SetGlobalVectorArray(cascadeCullingSphereId, cascadeCullingSpheres);
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
        
        //set the dir shadow light world to atlas Matrix in global
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        //set the shader keywords
        SetKeywords(directionalFilterKeywords, (int)shadowSettings.directional.filter - 1);
        SetKeywords(cascadeBlendKeywords, (int)shadowSettings.directional.cascadeBlendM - 1);

        //end profile
        buffer.EndSample(buffername);
        ExecuteBuffer();
    }

    void RenderOtherShadows()
    {
        int atlasSize = (int)shadowSettings.other.atlasSize;
        //buffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
        atlasSizes.z = atlasSize;
        atlasSizes.w = 1f / atlasSize;
        //create temp rendertex to save shadow depth tex
        buffer.GetTemporaryRT(otherShadowAtlasId, atlasSize, atlasSize,
            32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        //order GPU to render on this RT, Load.dontcare since we goingto write on it, We need oit to store
        buffer.SetRenderTarget(otherShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //clear the buffer for rendering
        buffer.ClearRenderTarget(true, false, Color.clear);
        //Pancaking is only useful in directional shadow rendering
        buffer.SetGlobalFloat(shadowPancakingId, 0f);
        //begin profile
        buffer.BeginSample(buffername);
        ExecuteBuffer();

        //if more than 1 dir light(MAX 4), the split should be 2(sqrt2)
        int tiles = ShadowedOtherLightCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for (int i = 0; i < ShadowedOtherLightCount; )
        {
            if (shadowedOtherLights[i].isPoint)
            {
                RenderPointShadows(i, split, tileSize);
                i += 6;
            }
            else
            {
                
                RenderSpotShadows(i, split, tileSize);
                i++;
            }
        }

        //set the dir shadow light world to atlas Matrix in global
        buffer.SetGlobalMatrixArray(otherShadowMatricesId, otherShadowMatrices);
        buffer.SetGlobalVectorArray(otherShadowTilesId, otherShadowTiles);
        //set the shader keywords
        SetKeywords(otherFilterKeywords, (int)shadowSettings.other.filter - 1);

        

        

        //end profile
        buffer.EndSample(buffername);
        ExecuteBuffer();
    }

    void SetOtherTileData(int index, Vector2 offset, float scale, float bias)
    {
        float border = atlasSizes.w * 0.5f;
        Vector4 data;
        data.x = offset.x * scale + border;
        data.y = offset.y * scale + border;
        data.z = scale - border - border;
        data.w = bias;
        otherShadowTiles[index] = data;
    }

    void SetKeywords(string[] keywords, int enabledIndex)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i == enabledIndex)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else 
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }
    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];

        //create a shadowdrawsetting based on the light index
        var shadowDSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex)
        { useRenderingLayerMaskTest = true};//make shadow effcted by rendering layer mask

        //take into count that each light will render max 4 cascades
        int cascadeCount = shadowSettings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = shadowSettings.directional.CascadeRatios;
        //cal the cascade culling factor, make it 0.8-fade to make sure casters in transition not being culled
        float cullingfactor = Mathf.Max(0f, 0.8f - shadowSettings.directional.cascadeFade);

        float tileScale = 1f / split;
        //get the lightviewMatrix, lightprojectionMatrix, clip space box for the dirctional lights
        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, i, cascadeCount, ratios, tileSize, light.nearPlaneOffset,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData);
            //make UNITY to cull some shadow caster in large cascade if the smaller cascade is enough
            splitData.shadowCascadeBlendCullingFactor = cullingfactor;
            shadowDSettings.splitData = splitData;
            //assign the cascade culling sphere, all lights uses the same culling spheres and same cascade datas
            if (index == 0)
            {
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }
            int tileIdex = tileOffset + i;
            
            //set the render view port and get the offset for modify the dir shadow matrix
            //set the matrix from wolrd space to shadow Atlas space
            dirShadowMatrices[tileIdex] = ConvertToAtlasMatrix(
                projectionMatrix * viewMatrix,
                SetTileViewport(tileIdex, split, tileSize),
                tileScale
                );
            //set the ViewProjectionMatrices for the buffer
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

            //experimental depth bias
            //buffer.SetGlobalDepthBias(50000f, 0f);
            //using slop bias
            buffer.SetGlobalDepthBias(0f, light.slopScaleBias);
            ExecuteBuffer();
            
            //draw shadow caster on the buffer
            context.DrawShadows(ref shadowDSettings);
            //Debug.Log(shadowDSettings
            buffer.SetGlobalDepthBias(0f, 0f);
        }

    }

    void RenderSpotShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];
        //create a shadowdrawsetting based on the light index
        var shadowDSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex)
        { useRenderingLayerMaskTest = true };//make shadow effcted by rendering layer mask

        //get matrcies
        cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(
            light.visibleLightIndex, out Matrix4x4 viewMatrix,
            out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

        shadowDSettings.splitData = splitData;
        //m00 for projectiona matrix is the projection scale?
        float texelSize = 2f / (tileSize * projectionMatrix.m00);
        float filterSize = texelSize * ((float)shadowSettings.other.filter + 1f);
        float bias = light.normalBias * filterSize * 1.414f;

        float tileScale = 1f / split;
        Vector2 offset = SetTileViewport(index, split, tileSize);
        SetOtherTileData(index, offset, tileScale, bias);

        otherShadowMatrices[index] = ConvertToAtlasMatrix(
            projectionMatrix * viewMatrix,
            offset, tileScale
            );

        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
        ExecuteBuffer();
        context.DrawShadows(ref shadowDSettings);
        buffer.SetGlobalDepthBias(0f, 0f);
    }

    void RenderPointShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];

        //6 face FOV is fixed to 90 degree
        float texelSize = 2f / (tileSize);
        float filterSize = texelSize * ((float)shadowSettings.other.filter + 1f);
        float bias = light.normalBias * filterSize * 1.414f;
        float tileScale = 1f / split;

        //create a shadowdrawsetting based on the light index
        var shadowDSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex)
        { useRenderingLayerMaskTest = true };//make shadow effcted by rendering layer mask

        //FOV bias to extend fov for a bit to get rid of seams 
        float fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;
        
        for (int i = 0; i < 6; i++)
        {
            //get matrcies
            cullingResults.ComputePointShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, (CubemapFace)i, fovBias, out Matrix4x4 viewMatrix,
                out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

            //negate the unity reverse unity render shadow unside down(Stop light leak)
            viewMatrix.m11 = -viewMatrix.m11;
            viewMatrix.m12 = -viewMatrix.m12;
            viewMatrix.m13 = -viewMatrix.m13;

            shadowDSettings.splitData = splitData;

            int tileIndex = index + i;
            //m00 for projectiona matrix is the projection scale?
            
            Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
            SetOtherTileData(tileIndex, offset, tileScale, bias);

            otherShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                projectionMatrix * viewMatrix,
                offset, tileScale
                );

            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            ExecuteBuffer();
            context.DrawShadows(ref shadowDSettings);
            buffer.SetGlobalDepthBias(0f, 0f);
        }
        
    }

    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        //Debug.Log(cullingSphere);
        float texelSize = 2f * cullingSphere.w / tileSize;
        //bigger the filter, larger the normal bias
        float filterSize = texelSize * ((float)shadowSettings.directional.filter + 1f);

        //cascadeData[index].x = 1f / cullingSphere.w;//preinverted R for GPU
        //reduce the spere radius by the filtersize to avoid sample outside the radius
        cullingSphere.w -= filterSize;
        //to reduce the calculation in gpu(square distance), we send in the sphere with radius = R * R
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
        //setting the cascade data x is square invert R,y is the texsize * 1.414(square root of 2)
        cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.414f);
    }

    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale)
    {
        //check if we are using a reversed Z buffer, if so negete
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        
        //CliP space is -1~0~1 but texture depth is 0~1
        //need to bake the scale and offset into the matrix
        //matrix [Scale] * [offset.x,offset.y,0] * [scale0.5, offset0.5] * [m]
        
        //float scale = 1f/ split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        /*
        //this is equal as above but more wasted 0 caculation
        Matrix4x4 m_clipscale = Matrix4x4.Scale(new Vector3(0.5f, 0.5f, 0.5f));
        Matrix4x4 m_cliptranslate = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.5f));
        Matrix4x4 m_AtlasOffset = Matrix4x4.Translate(new Vector3(offset.x, offset.y, 0f));
        Matrix4x4 m_AtlasScale = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        Debug.Log(m_AtlasScale);
        m = m_AtlasScale * m_AtlasOffset * m_cliptranslate * m_clipscale * m;
        */
        return m;
    }

    public void CleanUp()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        if (ShadowedOtherLightCount > 0)
        {
            buffer.ReleaseTemporaryRT(otherShadowAtlasId);
        }
        ExecuteBuffer();
    }

}
