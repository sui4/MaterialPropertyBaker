using System;
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

        private event Action<MaterialPropertyConfig> actionOnExported;

        [MenuItem("MaterialPropertyBaker/Material Property Exporter")]
        public static void Init()
        {
            var window = (MaterialPropertyExporter)GetWindow(typeof(MaterialPropertyExporter));
            window.titleContent = new GUIContent(WindowTitle);
            window.Show();
        }

        public static void Init(Material target, Action<MaterialPropertyConfig> onExported)
        {
            var window = (MaterialPropertyExporter)GetWindow(typeof(MaterialPropertyExporter));
            window.titleContent = new GUIContent(WindowTitle);
            window._targetMaterial = target;
            window.actionOnExported = onExported;
            window.GenerateConfig(target.shader);
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
            EditorUtils.ExportScriptableObject(config, configPath, out var exportedConfig, GetType());

            GenerateBakedMaterialProperty(_targetMaterial, exportedConfig as MaterialPropertyConfig);
            // folderPathの.assetを"_properties.asset"に置き換える
            var propertyPath = $"{folderPath.Replace(".asset", "")}_properties.asset";
            EditorUtils.ExportScriptableObject(_bakedMaterialProperty, propertyPath, out var _, GetType());

            if (actionOnExported != null)
            {
                actionOnExported?.Invoke(exportedConfig as MaterialPropertyConfig);
                actionOnExported = null;
                this.Close();
            }
        }

        #endregion // event

        #region Assets

        private void GenerateConfig(Shader shader)
        {
            EditorUtils.DestroyScriptableObjectIfExist(ref _materialPropertyConfig);
            _materialPropertyConfig = CreateInstance<MaterialPropertyConfig>();
            _materialPropertyConfig.LoadProperties(shader);
        }

        private void GenerateBakedMaterialProperty(Material targetMaterial, MaterialPropertyConfig config)
        {
            EditorUtils.DestroyScriptableObjectIfExist(ref _bakedMaterialProperty);
            _bakedMaterialProperty = CreateInstance<BakedMaterialProperty>();
            _bakedMaterialProperty.ShaderName = _targetMaterial.shader.name;
            _bakedMaterialProperty.Config = config;
            _bakedMaterialProperty.CreatePropsFrom(targetMaterial);
            _bakedMaterialProperty.DeleteUnEditableProperties(config);
        }

        #endregion
    }
}