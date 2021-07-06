using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanWaveParticleRender
{
    private OceanRenderSetting ORS;
    private Transform OceanCenter;
    private ComputeShader WaveParticleCompute;

    public OceanWaveParticleRender(
        OceanRenderSetting ORS,
        Transform OceanCenter,
        ComputeShader WaveParticleCompute
        )
    {
        this.ORS = ORS;
        this.OceanCenter = OceanCenter;
        this.WaveParticleCompute = WaveParticleCompute;

        InitWaveParticles();
    }

    RenderTexture temppointframe;

    [System.Serializable]
    public struct WaveParticle
    {
        public float Amplitude;
        public float BirthTime;
        public float DispersionAngle;
        public float Padding;
        public Vector2 Direction;//maybe we should use angle for direction, this makes subdivide easier
        public Vector2 Origin;
    }


    [HideInInspector]
    public WaveParticle[] WaveParticles;


    private void InitWaveParticles()
    {
        WaveParticles = new WaveParticle[ORS.WaveParticleCount];

        for (int i = 0; i < ORS.WaveParticleCount; i++)
        {
            WaveParticles[i].Amplitude = 0.5f;
            //WaveParticles[i].Radius = 4.0f;
            WaveParticles[i].BirthTime = 0.0f;

            WaveParticles[i].DispersionAngle = 0;
            WaveParticles[i].Direction = new Vector2(-1.0f, 1.0f);
            WaveParticles[i].Origin = new Vector2(1.0f * i, 0.0f * i);
        }

        ORS.WaveParticleEnd = 0;

        //Init temp frame
        temppointframe = new RenderTexture(512, 512, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
        temppointframe.enableRandomWrite = true;
        temppointframe.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        temppointframe.volumeDepth = 8;
        temppointframe.Create();
    }

    public void RenderWaveParticlesForLODs()
    {

        ComputeBuffer shapeWaveParticleBuffer = new ComputeBuffer(ORS.WaveParticleCount, 32);
        ORS.shapeWaveParticleShader.SetInt("_WaveParticleCount", ORS.WaveParticleCount);
        shapeWaveParticleBuffer.SetData(WaveParticles);
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

        ORS.shapeWaveParticleShader.SetFloats("_CenterPos", new float[]
            { OceanCenter.position.x, OceanCenter.position.y, OceanCenter.position.z}
            );


        int WaveParticleLODs = ORS.LODCount / 2;

        float CurrentLODSize_L = ORS.GridSize * ORS.GridCountPerTile * 4 * ORS.CurPastOceanScale[0].y;
        ORS.shapeWaveParticleShader.SetVector("_LODParams",
                    new Vector4(ORS.LODCount, 0, CurrentLODSize_L, 0.0f));
        ORS.shapeWaveParticleShader.Dispatch(0, ORS.WaveParticleCount / 128, 1, WaveParticleLODs);
        ORS.shapeWaveParticleShader.Dispatch(1, 16, 16, WaveParticleLODs);
        ORS.shapeWaveParticleShader.Dispatch(2, 16, 16, WaveParticleLODs);
        ORS.shapeWaveParticleShader.Dispatch(3, 16, 16, WaveParticleLODs);

        ORS.shapeWaveParticleShader.Dispatch(4, 16, 16, WaveParticleLODs);


        shapeWaveParticleBuffer.Release();



    }

    public void SpawnWaveParticles(WaveParticle newWP)
    {
        if (ORS.WaveParticleEnd >= ORS.WaveParticleCount)
        {
            ORS.WaveParticleEnd = ORS.WaveParticleEnd % ORS.WaveParticleCount;
        }
        WaveParticles[ORS.WaveParticleEnd] = newWP;
        ORS.WaveParticleEnd++;
        //Debug.Log("Add WP!");


    }

    public void UpdateWaveParticles()
    {
        for (int i = 0; i < Mathf.Min(ORS.WaveParticleCount, ORS.WaveParticleEnd); i++)
        {
            float LifeTime = Time.time - WaveParticles[i].BirthTime;
            float Span = WaveParticles[i].DispersionAngle * Mathf.PI / 180.0f * ORS.WaveParticleSpeed * LifeTime;
            if (Span >= ORS.WaveParticleRadius * 0.5f)
            {
                float newDispersionAngle = WaveParticles[i].DispersionAngle / 3.0f;
                float newAmplitude = WaveParticles[i].Amplitude / 3.0f;

                Quaternion RotationLeft = Quaternion.Euler(0, newDispersionAngle, 0);
                Quaternion RotationRight = Quaternion.Euler(0, -newDispersionAngle, 0);
                Vector2 originalDirection = WaveParticles[i].Direction;
                Vector3 leftDirection3 = RotationLeft * new Vector3(originalDirection.x, 0, originalDirection.y);
                Vector3 rightDirection3 = RotationRight * new Vector3(originalDirection.x, 0, originalDirection.y);


                WaveParticle leftnewWP = new WaveParticle();
                leftnewWP.Amplitude = newAmplitude;
                leftnewWP.BirthTime = WaveParticles[i].BirthTime;
                leftnewWP.DispersionAngle = newDispersionAngle;
                leftnewWP.Origin = WaveParticles[i].Origin;
                leftnewWP.Direction = new Vector2(leftDirection3.x, leftDirection3.z);

                WaveParticle rightnewWP = new WaveParticle();
                rightnewWP.Amplitude = newAmplitude;
                rightnewWP.BirthTime = WaveParticles[i].BirthTime;
                rightnewWP.DispersionAngle = newDispersionAngle;
                rightnewWP.Origin = WaveParticles[i].Origin;
                rightnewWP.Direction = new Vector2(rightDirection3.x, rightDirection3.z);

                WaveParticles[i].Amplitude = newAmplitude;
                WaveParticles[i].DispersionAngle = newDispersionAngle;
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


    }

}
