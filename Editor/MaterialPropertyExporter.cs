using System.IO;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [EditorWindowTitle(title = "Material Property Exporter")]
    public class MaterialPropertyExporter : EditorWindow
    {
        private MaterialPropertyExporter _window;
        private const string WindowTitle = "Material Property Exporter";

        private MaterialPropertyConfig _materialPropertyConfig;
        private MaterialPropertyConfig _existingConfigAsset;
        private MaterialPropertyConfigEditor _editor;
        private bool _useExistingConfig = false;

        private Material _targetMaterial;

        private BakedMaterialProperty _bakedMaterialProperty;

        [MenuItem("MaterialPropertyBaker/Material Property Exporter")]
        public static void Init()
        {
            var window = (MaterialPropertyExporter)GetWindow(typeof(MaterialPropertyExporter));
            window.titleContent = new GUIContent(WindowTitle);
            window.Show();
        }

        #region GUI

        private void OnGUI()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                _targetMaterial =
                    EditorGUILayout.ObjectField("Target Material", _targetMaterial, typeof(Material),
                        false) as Material;
                if (change.changed && _targetMaterial != null)
                {
                    GenerateConfig(_targetMaterial.shader);
                }
            }

            var isTargetMaterialExist = _targetMaterial != null;
            if (isTargetMaterialExist)
            {
                EditorGUILayout.LabelField("Target Shader", _targetMaterial.shader.name);
            }

            EditorGUILayout.Separator();

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Material Property Config", EditorStyles.boldLabel);
                        _useExistingConfig = EditorGUILayout.ToggleLeft("Use Existing Config", _useExistingConfig);
                    }

                    if (change.changed && !_useExistingConfig)
                    {
                        _existingConfigAsset = null;
                    }
                }

                if (_useExistingConfig)
                {
                    _existingConfigAsset =
                        EditorGUILayout.ObjectField("Config", _existingConfigAsset, typeof(MaterialPropertyConfig),
                            false) as MaterialPropertyConfig;
                    if (isTargetMaterialExist && _existingConfigAsset != null &&
                        _existingConfigAsset.ShaderName != _targetMaterial.shader.name)
                    {
                        EditorGUILayout.HelpBox("Shader is not matched.", MessageType.Error);
                    }
                }

                ConfigGUI(_useExistingConfig ? _existingConfigAsset : _materialPropertyConfig);
            }

            EditorGUILayout.Separator();

            using (new EditorGUI.DisabledScope(!IsValid()))
            {
                ExportButtonGUI();
            }
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
            if (materialPropertyConfig == null) return;
            if (_editor == null)
            {
                _editor = (MaterialPropertyConfigEditor)Editor.CreateEditor(materialPropertyConfig);
            }
            else if (_editor.target != materialPropertyConfig)
            {
                DestroyImmediate(_editor);
                _editor = null;
                _editor = (MaterialPropertyConfigEditor)Editor.CreateEditor(materialPropertyConfig);
            }

            if (_editor != null)
            {
                _editor.OnInspectorGUI();
            }
        }

        #endregion

        private bool IsValid()
        {
            var config = _useExistingConfig ? _existingConfigAsset : _materialPropertyConfig;
            return _targetMaterial != null &&
                   config != null &&
                   config.ShaderName == _targetMaterial.shader.name;
        }

        # region Event

        private void OnExportButtonClicked()
        {
            var defaultName = Utils.MakeFileNameSafe(_targetMaterial.name);
            var folderPath = EditorUtility.SaveFilePanelInProject("Save Config and Properties", defaultName, "asset",
                "MaterialPropertyBakerData");
            if (string.IsNullOrEmpty(folderPath)) return;

            // folderPathの.assetを"_config.asset"に置き換える
            var configPath = $"{folderPath.Replace(".asset", "")}_config.asset";
            var config = _useExistingConfig ? _existingConfigAsset : _materialPropertyConfig;
            ExportScriptableObject(config, configPath, out var exportedConfig);

            GenerateBakedMaterialProperty(_targetMaterial);
            _bakedMaterialProperty.DeleteUnEditableProperties(exportedConfig as MaterialPropertyConfig);
            // folderPathの.assetを"_properties.asset"に置き換える
            var propertyPath = $"{folderPath.Replace(".asset", "")}_properties.asset";
            ExportScriptableObject(_bakedMaterialProperty, propertyPath, out var exportedProperty);
        }

        #endregion // event

        #region Assets

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
                if (!AssetDatabase.IsMainAsset(_materialPropertyConfig))
                {
                    DestroyImmediate(_materialPropertyConfig);
                }

                _materialPropertyConfig = null;
            }
        }

        private void GenerateBakedMaterialProperty(Material targetMaterial)
        {
            DestroyBakedMaterialPropertyIfExist();

            _bakedMaterialProperty = CreateInstance<BakedMaterialProperty>();
            _bakedMaterialProperty.ShaderName = _targetMaterial.shader.name;
            _bakedMaterialProperty.CreatePropsFrom(targetMaterial);
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
        private bool ExportScriptableObject(in ScriptableObject scriptableObject, string path,
            out ScriptableObject exported, bool refresh = true)
        {
            exported = null;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"Failed to export : path is null or empty.");
                return false;
            }

            if (scriptableObject == null)
            {
                Debug.LogError("Failed to export : target object is null.");
            }

            exported = Instantiate(scriptableObject);
            EditorUtility.SetDirty(exported);

            if (File.Exists(path))
            {
                Debug.Log($"{GetType()}: delete existing: {path}");
                var success = AssetDatabase.DeleteAsset(path);
                if (!success)
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