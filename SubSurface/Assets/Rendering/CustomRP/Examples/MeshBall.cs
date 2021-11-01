using UnityEngine;
using UnityEngine.Rendering;

public class MeshBall : MonoBehaviour
{
    [SerializeField]
    LightProbeProxyVolume lightProbeVolume = null;

    static int
        baseColorId = Shader.PropertyToID("_BaseColor"),
        metallicId = Shader.PropertyToID("_Metallic"),
        smoothnessId = Shader.PropertyToID("_Smoohness");

    [SerializeField]
    Mesh mesh = default;

    [SerializeField]
    Material material = default;

    Matrix4x4[] matrices = new Matrix4x4[1023];
    Vector4[] baseColors = new Vector4[1023];
    float[] metallics = new float[1023];
    float[] smoothnesses = new float[1023];

    MaterialPropertyBlock block;

    private void Awake()
    {
        for (int i = 0; i < matrices.Length; i++)
        {
            matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10.0f, Quaternion.identity, Vector3.one * Random.Range(0.5f, 1.5f));
            baseColors[i] = new Vector4(Random.value, Random.value, Random.value, Random.Range(0.5f, 1.5f));
            metallics[i] = Random.value < 0.25f ? 0.0f : 1.0f;
            smoothnesses[i] = Random.Range(0.05f, 0.95f);
        }
    }

    private void Update()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(baseColorId, baseColors);
            block.SetFloatArray(metallicId, metallics);
            block.SetFloatArray(smoothnessId, smoothnesses);

            if (!lightProbeVolume)
            {
                var positions = new Vector3[1023];
                for (int i = 0; i < matrices.Length; i++)
                {
                    positions[i] = matrices[i].GetColumn(3);//get the position from matrix
                }
                var lightProbes = new SphericalHarmonicsL2[1023];
                //occlusion probe dara for mesh ball
                var occlusionProbes = new Vector4[1023];
                LightProbes.CalculateInterpolatedLightAndOcclusionProbes(positions, lightProbes, occlusionProbes);
                block.CopySHCoefficientArraysFrom(lightProbes);
                block.CopyProbeOcclusionArrayFrom(occlusionProbes);
            }  
        }
        Graphics.DrawMeshInstanced(mesh, 0, material, matrices, 1023, block, 
            UnityEngine.Rendering.ShadowCastingMode.On, true, 0, null,
            lightProbeVolume ? LightProbeUsage.UseProxyVolume : UnityEngine.Rendering.LightProbeUsage.CustomProvided,
            lightProbeVolume
            );
    }
}
