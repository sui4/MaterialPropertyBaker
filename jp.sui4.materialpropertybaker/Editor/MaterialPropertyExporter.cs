using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using File = System.IO.File;
namespace sui4.MaterialPropertyBaker
{
    [EditorWindowTitle(title= "Material Property Exporter")]
    public class MaterialPropertyExporter: EditorWindow
    {
        private MaterialPropertyExporter _window;
        private const string WindowTitle = "Material Property Exporter";
        
        private string _targetShaderName = "";
        
        // private ShaderInfo[] _shaderInfos;

        private Shader _shader;
        private Shader _prevShader;
        
        private MaterialPropertyConfig _materialPropertyConfig;
        private MaterialPropertyConfigEditor _editor;

        private Material _targetMaterial;
        
        private BakedMaterialProperty _bakedMaterialProperty;

        [MenuItem("tools/Material Property Baker/Material Property Exporter")]
        public static void Init()
        {
            var window = (MaterialPropertyExporter)GetWindow(typeof(MaterialPropertyExporter));
            window.titleContent = new GUIContent(WindowTitle);
            window.Show();
        }

        #region GUI

        private void OnGUI()
        {
            
            _targetMaterial = EditorGUILayout.ObjectField("Target Material", _targetMaterial, typeof(Material), false) as Material;
            
            if (_targetMaterial != null)
            {
                _shader = _targetMaterial.shader;
                EditorGUILayout.LabelField("Target Shader", _shader.name);
                if (_shader != _prevShader)
                {
                    GenerateConfig(_shader);
                    GenerateBakedMaterialProperty(_targetMaterial);
                    _prevShader = _shader;
                }
            }
            EditorGUILayout.Separator();
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Material Property Config", EditorStyles.boldLabel);
                ShaderPropertiesGUI(_materialPropertyConfig);
            }
            EditorGUILayout.Separator();
            
            ExportButtonGUI();
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

        private void ShaderPropertiesGUI(MaterialPropertyConfig materialPropertyConfig)
        {
            if (_editor == null)
            {
                if (_materialPropertyConfig == null)
                    return;
                _editor = (MaterialPropertyConfigEditor)Editor.CreateEditor(materialPropertyConfig);
            }
            else if(_editor.target != materialPropertyConfig)
            {
                DestroyImmediate(_editor);
                _editor = null;
                if(_materialPropertyConfig == null)
                    return;
                _editor = (MaterialPropertyConfigEditor)Editor.CreateEditor(materialPropertyConfig);
            }

            if (_editor != null)
            {
                _editor.OnInspectorGUI();
            }
        }

        #endregion

        # region Event
       
        private void OnExportButtonClicked()
        {
            var defaultName = Utils.MakeFileNameSafe(_targetMaterial.name);
            defaultName = $"{defaultName}";
            var folderPath = EditorUtility.SaveFilePanelInProject("Save Config and Properties", defaultName, "asset", "MaterialPropertyBakerData");
            
            // folderPathの.assetを"_config.asset"に置き換える
            var configPath = $"{folderPath.Replace(".asset", "")}_config.asset";
            ExportConfig(_materialPropertyConfig, configPath, out var exportedConfig);

            _bakedMaterialProperty.MaterialPropertyConfig = exportedConfig;
            // folderPathの.assetを"_properties.asset"に置き換える
            var propertyPath = $"{folderPath.Replace(".asset", "")}_properties.asset";
            ExportBakedProperty(_bakedMaterialProperty, propertyPath, out var exportedProperty);
        }
        #endregion // event

        #region Assets
        
        private void GenerateConfig(Shader shader)
        {
            if(_materialPropertyConfig != null)
                DestroyImmediate(_materialPropertyConfig);
            
            _materialPropertyConfig = CreateInstance<MaterialPropertyConfig>();
            _materialPropertyConfig.LoadProperties(shader);
        }

        private void GenerateBakedMaterialProperty(Material targetMaterial)
        {
            if (_bakedMaterialProperty != null)
                DestroyImmediate(_bakedMaterialProperty);
            
            _bakedMaterialProperty = CreateInstance<BakedMaterialProperty>();
            _bakedMaterialProperty.MaterialPropertyConfig = _materialPropertyConfig;
            _bakedMaterialProperty.ShaderName = _shader.name;
            _bakedMaterialProperty.CreatePropsFromMaterial(targetMaterial);
        }
        
        
        // path: Assets以下のパス, ファイル名込み
        private bool ExportConfig(in MaterialPropertyConfig materialPropertyConfig, string path, out MaterialPropertyConfig exported, bool refresh = true)
        {
            exported = null;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"Failed to export ShaderProperties: path is null or empty.");
                return false;
            }

            if (materialPropertyConfig == null)
            {
                Debug.LogError("Failed to export ShaderProperties: shaderProperties is null.");
            }

            exported = Instantiate(materialPropertyConfig);
            EditorUtility.SetDirty(exported);

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

            AssetDatabase.CreateAsset(exported, path);
            if (refresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"Saved : {path}");
            return true;
        }
        
        // path: Assets以下のパス, ファイル名込み
        private bool ExportBakedProperty(in BakedMaterialProperty bakedProperty, string path, out BakedMaterialProperty exported, bool refresh = true)
        {
            exported = null;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"Failed to export ShaderProperties: path is null or empty.");
                return false;
            }

            if (bakedProperty == null)
            {
                Debug.LogError("Failed to export ShaderProperties: shaderProperties is null.");
            }

            exported = Instantiate(bakedProperty);
            EditorUtility.SetDirty(exported);

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

            AssetDatabase.CreateAsset(exported, path);
            if (refresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"Saved : {path}");
            return true;
        }
        #endregion

    }
}