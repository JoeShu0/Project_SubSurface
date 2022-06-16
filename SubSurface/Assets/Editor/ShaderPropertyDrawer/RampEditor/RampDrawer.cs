/** 
描述：
    1.工具来自GitHub项目：https://github.com/JasonMa0012/JasonMaToonRenderPipeline
**/

using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Rendering.TCdemo
{
    public class RampDrawer : MaterialPropertyDrawer
    {
        AssetImporter assetImporter;
        string defaultFileName = "RampMap";

        static GUIContent _iconAdd, _iconEdit;

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (_iconAdd == null || _iconEdit == null)
            {
                _iconAdd = EditorGUIUtility.IconContent("d_Toolbar Plus");
                _iconEdit = EditorGUIUtility.IconContent("editicon.sml");
            }

            //Label
            //var position = EditorGUILayout.GetControlRect();
            var labelRect = position;
            labelRect.height = EditorGUIUtility.singleLineHeight;
            var space = labelRect.height + 4;
            position.y += space - 3;
            position.height -= space;
            EditorGUI.PrefixLabel(labelRect, new GUIContent(label));

            //Texture object field
            var w = EditorGUIUtility.labelWidth;
            var indentLevel = EditorGUI.indentLevel;
            editor.SetDefaultGUIWidths();
            var buttonRect = MaterialEditor.GetRectAfterLabelWidth(labelRect);

            EditorGUIUtility.labelWidth = 0;
            EditorGUI.indentLevel = 0;
            var textureRect = MaterialEditor.GetRectAfterLabelWidth(labelRect);
            textureRect.xMax -= buttonRect.width;
            var newTexture = (Texture) EditorGUI.ObjectField(textureRect, prop.textureValue, typeof(Texture2D), false);
            EditorGUIUtility.labelWidth = w;
            EditorGUI.indentLevel = indentLevel;
            if (newTexture != prop.textureValue)
            {
                prop.textureValue = newTexture;
                assetImporter = null;
            }

            //Preview texture override (larger preview, hides texture name)
            var previewRect = new Rect(textureRect.x + 1, textureRect.y + 1, textureRect.width - 19, textureRect.height - 2);
            if (prop.hasMixedValue)
            {
                EditorGUI.DrawPreviewTexture(previewRect, Texture2D.grayTexture);
                GUI.Label(new Rect(previewRect.x + previewRect.width * 0.5f - 10, previewRect.y, previewRect.width * 0.5f, previewRect.height), "―");
            }
            else if (prop.textureValue != null)
                EditorGUI.DrawPreviewTexture(previewRect, prop.textureValue);

            if (prop.textureValue != null)
            {
                assetImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(prop.textureValue));
            }

            var buttonRectL = new Rect(buttonRect.x, buttonRect.y, buttonRect.width * 0.5f, buttonRect.height);
            var buttonRectR = new Rect(buttonRectL.xMax, buttonRect.y, buttonRect.width * 0.5f, buttonRect.height);
            bool needCreat = false;
            if (GUI.Button(buttonRectL, _iconEdit))
            {
                if ((assetImporter != null) && (assetImporter.userData.StartsWith("GRADIENT") || assetImporter.userData.StartsWith("gradient:")) && !prop.hasMixedValue)
                {
                    RampGenerator.OpenForEditing((Texture2D) prop.textureValue, editor.targets, true, false);
                }
                else
                {
                    needCreat = true;
                }
            }

            if (GUI.Button(buttonRectR, _iconAdd) || needCreat)
            {
                var lastSavePath = GradientManager.LAST_SAVE_PATH;
                if (!lastSavePath.Contains(Application.dataPath))
                    lastSavePath = Application.dataPath;

                var path = EditorUtility.SaveFilePanel("Create New Ramp Texture", lastSavePath, defaultFileName, "png");
                if (!string.IsNullOrEmpty(path))
                {
                    bool overwriteExistingFile = File.Exists(path);

                    GradientManager.LAST_SAVE_PATH = Path.GetDirectoryName(path);

                    //Create texture and save PNG
                    var projectPath = path.Replace(Application.dataPath, "Assets");
                    GradientManager.CreateAndSaveNewGradientTexture(256, projectPath);

                    //Load created texture
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(projectPath);
                    assetImporter = AssetImporter.GetAtPath(projectPath);

                    //Assign to material(s)
                    foreach (var item in prop.targets)
                    {
                        ((Material) item).SetTexture(prop.name, texture);
                    }

                    //Open for editing
                    RampGenerator.OpenForEditing(texture, editor.targets, true, !overwriteExistingFile);
                }
            }
        }
    }
}