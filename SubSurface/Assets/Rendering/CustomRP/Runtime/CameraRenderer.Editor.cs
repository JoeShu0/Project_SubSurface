using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
partial class CameraRenderer
{
    partial void DrawUnsupportedShaders();
    partial void PrepareForSceneView();
    partial void PrepareBuffer();
    partial void DrawGizmosBeforeFX();
    partial void DrawGizmosAfterFX();

#if UNITY_EDITOR

    string SampleName { get; set; }
    //get all the shadertagid for the legacy shaders
    static ShaderTagId[] LegacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    static Material errorMaterial;

    partial void DrawGizmosBeforeFX()
    {
        if (Handles.ShouldRenderGizmos())
        {
            if (useIntermediateBuffer)//If depth is available fade gizmo with depth
            {
                Draw(depthAttachmentId, BuiltinRenderTextureType.CameraTarget, true);
                ExecuteBuffer();
            }
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            //context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void DrawGizmosAfterFX()
    {
        if (Handles.ShouldRenderGizmos())
        {
            //context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void DrawUnsupportedShaders()
    {
        //create Error material
        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        //Draw Legacy shaders using error shader
        var drawingSettings = new DrawingSettings(LegacyShaderTagIds[0], new SortingSettings(camera)) { overrideMaterial = errorMaterial };
        for (int i = 1; i < LegacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, LegacyShaderTagIds[i]);
        }

        var filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings);
    }

    partial void PrepareForSceneView()
    {
        //Add the UI as geo to render in SceneView camera. for betterUP
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            useScaledRendering = false;//no need for scaled rendering for editor window
        }
    }

    partial void PrepareBuffer()
    {
        //this mark the allocation of mem for SampleName clear in profiler 
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }

#else
    //In Build, the buffer name will be fixed, All camera draws are nested into the same name
    //and less MEM alloc
    const string SampleName = buffername;

#endif
}
