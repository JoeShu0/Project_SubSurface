using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

public class OceanRenderer
{
    const string buffername = "Ocean";

    CommandBuffer buffer = new CommandBuffer
    {
        name = buffername
    };

    Camera camera;
    ScriptableRenderContext context;

    Mesh TestMesh;
    public Mesh ReplaceMesh;

    public Material testMaterial;
    /*
    private void Awake()
    {
        
        TestMesh = BuildMesh();

        //TestMesh = ReplaceMesh;


        
    }
    */

    Mesh BuildMesh()
    {
        Mesh tilemesh = new Mesh();
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-1.0f, 0, 0);
        vertices[1] = new Vector3(0, 0, -1.0f);
        vertices[2] = new Vector3(1, 0, 0);
        vertices[3] = new Vector3(0, 0, 1);

        int[] triangles = new int[6];

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;

        triangles[3] = 2;
        triangles[4] = 3;
        triangles[5] = 0;

        tilemesh.vertices = vertices;
        tilemesh.triangles = triangles;

        return tilemesh;
    }

    public void Setup(
        ScriptableRenderContext context,
        Camera camera, Material material)
    {
        this.context = context;
        this.camera = camera;
        this.testMaterial = material;
        TestMesh = BuildMesh();
    }

    public void Render()
    {
        //buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,
            //RenderBufferLoadAction.Load,
            //RenderBufferStoreAction.Store);
        buffer.DrawMesh(TestMesh, Matrix4x4.identity, testMaterial, 0, 0);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public void CleanUp()
    {

    }
}
