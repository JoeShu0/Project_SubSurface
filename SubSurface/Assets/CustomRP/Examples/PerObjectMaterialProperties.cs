using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    static int
        baseColorId = Shader.PropertyToID("_BaseColor"),
        CutoffId = Shader.PropertyToID("_Cutoff"),
        metallicId = Shader.PropertyToID("_Metallic"),
        smoothnessId = Shader.PropertyToID("_Smoothness"),
        emissionColorId = Shader.PropertyToID("_EmissionColor");
    

    [SerializeField]
    Color baseColor = Color.white;

    [SerializeField, ColorUsage(false, true)]//Useage of Alpha and HDR
    Color emissionColor = Color.black;

    [SerializeField, Range(0f, 1f)]
    float cutoff = 0.5f, metallic = 0f, smoothness = 0.5f;


    //this is the material property block per object, 
    //you can set properties on this instead of setting on material(Save materials, like instance) 
    //and set this block to material
    static MaterialPropertyBlock block;

    private void OnValidate()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        //block.Clear();//Clear and set the MPB will also release the effect on materials 
        block.SetColor(baseColorId, baseColor);
        block.SetFloat(CutoffId, cutoff);
        block.SetFloat(metallicId, metallic);
        block.SetFloat(smoothnessId, smoothness);

        block.SetColor(emissionColorId, emissionColor);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }

    private void Awake()
    {
        //Onvalidate does not get called in builds, Manually calling
        //OnValidate();
    }
}
