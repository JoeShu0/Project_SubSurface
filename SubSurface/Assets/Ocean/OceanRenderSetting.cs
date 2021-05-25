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
}
