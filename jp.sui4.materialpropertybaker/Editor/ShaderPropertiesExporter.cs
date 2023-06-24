using System;
using UnityEditor;
using UnityEngine;
using File = System.IO.File;
namespace sui4.MaterialPropertyBaker
{
    [EditorWindowTitle(title= "Shader Properties Exporter")]
    public class ShaderPropertiesExporter: EditorWindow
    {
        private ShaderPropertiesExporter _window;
        private const string WindowTitle = "Shader Properties Exporter";
        
        private string _targetShaderName = "";
        
        // private ShaderInfo[] _shaderInfos;

        private Shader _shader;
        
        private ShaderProperties _shaderProperties;
        private ShaderPropertiesEditor _editor;

        [MenuItem("tools/Material Property Baker/Shader Properties Exporter")]
        public static void Init()
        {
            var window = (ShaderPropertiesExporter)GetWindow(typeof(ShaderPropertiesExporter));
            window.titleContent = new GUIContent(WindowTitle);
            window.Show();
        }

        #region GUI

        private void OnGUI()
        {
            FindShaderGUI();
            
            GenerateButtonGUI();

            EditorGUILayout.Separator();
            ShaderPropertiesGUI(_shaderProperties);
            EditorGUILayout.Separator();
            
            ExportButtonGUI();
        }

        private void FindShaderGUI()
        {
            _targetShaderName = EditorGUILayout.TextField("Target Shader Name", _targetShaderName);

            if(GUILayout.Button("Find Shader"))
                OnFindShaderClicked(_targetShaderName);

            if (_shader != null)
            {
                EditorGUILayout.LabelField("Target Shader", AssetDatabase.GetAssetPath(_shader));
            }
        }
        private void GenerateButtonGUI()
        {
            var tmp = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Generate Shader Properties"))
            {
                OnGenerateClicked(_shader);
            }
            GUI.backgroundColor = tmp;
        }
        private void ExportButtonGUI()
        {            
            var tmp = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Export"))
            {
                OnExportButtonClicked();
            }
            GUI.backgroundColor = tmp;
        }

        private void ShaderPropertiesGUI(ShaderProperties shaderProperties)
        {
            if (_editor == null)
            {
                if (_shaderProperties == null)
                    return;
                _editor = (ShaderPropertiesEditor)Editor.CreateEditor(shaderProperties);
            }
            else if(_editor.target != shaderProperties)
            {
                DestroyImmediate(_editor);
                _editor = null;
                if(_shaderProperties == null)
                    return;
                _editor = (ShaderPropertiesEditor)Editor.CreateEditor(shaderProperties);
            }

            if (_editor != null)
            {
                _editor.OnInspectorGUI();
            }
        }

        #endregion

        # region Event

        private void OnFindShaderClicked(string shaderName)
        {
            _shader = Shader.Find(shaderName);
            if (_shader == null)
                Debug.LogError($"Failed to find shader: {shaderName}");
            
        }
        private void OnSearchShaderClicked()
        {
            throw new NotImplementedException();
            //_shaderInfos = ShaderUtil.GetAllShaderInfo();
        }
        
        private void OnGenerateClicked(Shader shader)
        {
            if(_shaderProperties != null)
                DestroyImmediate(_shaderProperties);
            
            _shaderProperties = CreateInstance<ShaderProperties>();
            _shaderProperties.LoadProperties(shader);
        }
        
        private void OnExportButtonClicked()
        {
            var defaultName = Utils.MakeFileNameSafe(_shaderProperties.ShaderName);
            defaultName = $"ShaderProperties_{defaultName}.asset";
            var path = EditorUtility.SaveFilePanelInProject("Save Shader Properties", defaultName, "asset", "Save Shader Properties");

            Export(_shaderProperties, path);
        }

        #endregion // event
        
        
        // path: Assets以下のパス, ファイル名込み
        private bool Export(in ShaderProperties shaderProperties, string path, bool refresh = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"Failed to export ShaderProperties: path is null or empty.");
                return false;
            }

            if (shaderProperties == null)
            {
                Debug.LogError("Failed to export ShaderProperties: shaderProperties is null.");
            }

            var assetToSave = Instantiate(shaderProperties);
            EditorUtility.SetDirty(assetToSave);

            if (File.Exists(path))
            {
                Debug.Log($"{GetType()}: delete existing: {path}");
                var sucess = AssetDatabase.DeleteAsset(path);
                if (!sucess)
                {
                    Debug.LogError($"{GetType()}: failed to delete existing: {path}");
                    return false;
                }
            }

            AssetDatabase.CreateAsset(assetToSave, path);
            if (refresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"Saved : {path}");
            return true;
        }
    }
}