using UnityEngine;

[DisallowMultipleComponent]
public class PerTileMaterialProperty : MonoBehaviour
{
    public float GridSize =1;
    public Vector4 TransitionParam = new Vector4(1.25f, 1.25f, 0.5f, 0.5f);
    public Vector3 CenterPos = new Vector3(0,0,0);
    public float LODSize = 10;    
    
    static int
        baseColorId = Shader.PropertyToID("_BaseColor"),
        gridSizeId = Shader.PropertyToID("_GridSize"),
        transitionParams = Shader.PropertyToID("_TransitionParam"),
        centerPosId = Shader.PropertyToID("_CenterPos"),
        lodSizeId = Shader.PropertyToID("_LODSize");


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
        //block.SetColor(baseColorId, baseColor);

        GetComponent<Renderer>().SetPropertyBlock(block);
    }

    private void Awake()
    {
        //Onvalidate does not get called in builds, Manually calling
        //OnValidate();
    }
}
