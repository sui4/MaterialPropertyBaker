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
        
        private MaterialPropertyConfig _materialPropertyConfig;
        private MaterialPropertyConfigEditor _editor;
        private bool _useExistingConfig = false;
        private bool _useExistingConfigPrev = false;

        private Material _targetMaterial;
        private Material _targetMaterialPrev;
        
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
                EditorGUILayout.LabelField("Target Shader", _targetMaterial.shader.name);
                if (_targetMaterial != _targetMaterialPrev)
                {
                    GenerateAssets();
                    
                    _useExistingConfig = false;
                    _useExistingConfigPrev = false;
                    _targetMaterialPrev = _targetMaterial;
                }
                if(_useExistingConfig != _useExistingConfigPrev)
                {
                    _useExistingConfigPrev = _useExistingConfig;
                    if (_useExistingConfig == false)
                    {
                        GenerateAssets();
                    }
                    else
                    {
                        DestryAssets();
                    }
                }

            }
            EditorGUILayout.Separator();
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Material Property Config", EditorStyles.boldLabel);
                    _useExistingConfig = EditorGUILayout.ToggleLeft("Use Existing Config", _useExistingConfig);
                }
                if (_useExistingConfig)
                {
                    _materialPropertyConfig = EditorGUILayout.ObjectField("Config", _materialPropertyConfig, typeof(MaterialPropertyConfig), false) as MaterialPropertyConfig;
                    if (_materialPropertyConfig != null && _targetMaterial != null)
                    {
                        if (_materialPropertyConfig.ShaderName != _targetMaterial.shader.name)
                        {
                            EditorGUILayout.HelpBox("Shader is not matched.", MessageType.Error);
                        }
                        else
                        {
                            ConfigGUI(_materialPropertyConfig);
                        }
                    }
                }
                else
                {
                    ConfigGUI(_materialPropertyConfig);
                }
            }
            EditorGUILayout.Separator();
            var isValid = _materialPropertyConfig != null && _bakedMaterialProperty != null;
            isValid = isValid && _materialPropertyConfig.ShaderName == _targetMaterial?.shader.name;
            GUI.enabled = isValid;
            ExportButtonGUI();
            GUI.enabled = true;
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

        private void ConfigGUI(MaterialPropertyConfig materialPropertyConfig)
        {
            if(_editor == null)
            {
                if(_materialPropertyConfig == null)
                {
                    if(_targetMaterial == null)
                    {
                        return;
                    }
                    GenerateAssets();
                }
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
            if (string.IsNullOrEmpty(folderPath)) return;
            
            // folderPathの.assetを"_config.asset"に置き換える
            var configPath = $"{folderPath.Replace(".asset", "")}_config.asset";
            ExportConfig(_materialPropertyConfig, configPath, out var exportedConfig);

            _bakedMaterialProperty.DeleteUnEditableProperties(exportedConfig);
            // folderPathの.assetを"_properties.asset"に置き換える
            var propertyPath = $"{folderPath.Replace(".asset", "")}_properties.asset";
            ExportBakedProperty(_bakedMaterialProperty, propertyPath, out var exportedProperty);
        }
        #endregion // event

        #region Assets

        private void GenerateAssets()
        {
            GenerateConfig(_targetMaterial.shader);
            GenerateBakedMaterialProperty(_targetMaterial);
        }

        private void DestryAssets()
        {
            DestroyConfigIfExist();
            DestroyBakedMaterialPropertyIfExist();
        }
        
        // 直接呼ばない。GenerateAssetsを使う
        private void GenerateConfig(Shader shader)
        {
            DestroyConfigIfExist();

            _materialPropertyConfig = CreateInstance<MaterialPropertyConfig>();
            _materialPropertyConfig.LoadProperties(shader);
        }

        private void DestroyConfigIfExist()
        {
            if (_materialPropertyConfig != null)
            {
                if(!AssetDatabase.IsMainAsset(_materialPropertyConfig))
                {
                    DestroyImmediate(_materialPropertyConfig);
                }
                _materialPropertyConfig = null;
            }
        }

        // 直接呼ばない。GenerateAssetsを使う
        private void GenerateBakedMaterialProperty(Material targetMaterial)
        {
            DestroyBakedMaterialPropertyIfExist();
            
            _bakedMaterialProperty = CreateInstance<BakedMaterialProperty>();
            _bakedMaterialProperty.ShaderName = _targetMaterial.shader.name;
            _bakedMaterialProperty.CreatePropsFromMaterial(targetMaterial);
        }

        private void DestroyBakedMaterialPropertyIfExist()
        {
            if (_bakedMaterialProperty != null)
            {
                if (!AssetDatabase.IsMainAsset(_bakedMaterialProperty))
                {
                    DestroyImmediate(_bakedMaterialProperty);
                }
                _bakedMaterialProperty = null;
            }
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