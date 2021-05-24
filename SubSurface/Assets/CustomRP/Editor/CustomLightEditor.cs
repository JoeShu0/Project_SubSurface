using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light), typeof(CustomRenderPipelineAsset))]
public class CustomLightEditor : LightEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        //DrawRenderingLayerMask();
        RenderingLayerMaskDrawer.Draw(
            settings.renderingLayerMask, renderingLayerMaskLabel    
        );

        if (!settings.lightType.hasMultipleDifferentValues &&
            (LightType)settings.lightType.enumValueIndex == LightType.Spot
        )
        {
            settings.DrawInnerAndOuterSpotAngle();
        }

        var light = target as Light;
        if (light.cullingMask != -1)
        {
            EditorGUILayout.HelpBox(
                light.type == LightType.Directional ?
                "Culling Mask only affect shadows." : 
                "Culling Mask only affect shadow unless Use Lights Per Object Is On",
                MessageType.Warning);
        }

        settings.ApplyModifiedProperties();
    }

    static GUIContent renderingLayerMaskLabel =
        new GUIContent("Rendering Layer Mask", "Functional version of above property.");
    //Make the rendering layer selection function for lights
    void DrawRenderingLayerMask()
    {
        SerializedProperty property = settings.renderingLayerMask;
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        int mask = property.intValue;
        //special case for everything
        //display as -1 but store as int.max
        if (mask == int.MaxValue)
        {
            mask = -1;//means everything mask
        }
        mask = EditorGUILayout.MaskField(
            renderingLayerMaskLabel, mask,
            GraphicsSettings.currentRenderPipeline.renderingLayerMaskNames
        );
        if (EditorGUI.EndChangeCheck())
        {
            property.intValue = mask == -1 ? int.MaxValue : mask;
        }

        EditorGUI.showMixedValue = false;
    }
}
