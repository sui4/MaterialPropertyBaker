using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using File = System.IO.File;

namespace sui4.MaterialPropertyBaker.Timeline
{
    [CustomEditor(typeof(MaterialPropSwitcherClip))]
    [CanEditMultipleObjects]
    public class MaterialPropSwitcherClipInspectorEditor : Editor
    {
        private SerializedProperty _presetRef;
        private SerializedProperty _bakedMaterialProperty;
        
        private BakedMaterialProperty _presetPrev;

        private BakedMaterialPropertiesEditor _editor;

        private MaterialPropSwitcherClip _targetClip;

        private bool _editable;

        private void OnEnable()
        {
            if(target == null)
                return;
            _targetClip = (MaterialPropSwitcherClip)target;

            _presetRef = serializedObject.FindProperty("_presetRef");
            _bakedMaterialProperty = serializedObject.FindProperty("_bakedMaterialProperty");
        }

        #region GUI

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            if (target == null)
            {
                return;
            }

            serializedObject.Update();
            var clip = _targetClip;

            EditorGUILayout.PropertyField(_bakedMaterialProperty);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                PresetGUI();
            }
            
            EditorGUILayout.Separator();

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUI.DisabledScope(_editable))
                {
                    BakedPropertiesGUI();
                    
                    EditorGUILayout.Separator();
            
                    // Export button
                    using (var h = new EditorGUILayout.HorizontalScope())
                    {
                        var tmp = GUI.backgroundColor;
                        GUI.backgroundColor = Color.cyan;
                        if(GUILayout.Button("Save as"))
                        {
                            var preset = CreatePresetFromProps(clip.BakedMaterialProperty.MaterialProps);
                            ExportProfile(preset);
                        }
                        GUI.backgroundColor = tmp;
                    }
                }

            }
        }

        
        //--- GUI ---//
        private void PresetGUI()
        {
            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                var label = new GUIContent("Preset Profile");
                EditorGUILayout.PropertyField(_presetRef, label);

                if (changeCheck.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    if (_presetRef.objectReferenceValue != null)
                    {
                        ((BakedMaterialProperty)_presetRef.objectReferenceValue).UpdateShaderID();
                        _targetClip.LoadValuesFromPreset();
                    }
                }
            }

            var clip = (MaterialPropSwitcherClip)target;
            // Load Save buttons
            if (clip.PresetRef != null)
            {

                using (var changeCheck = new EditorGUI.ChangeCheckScope())
                {
                    var label = new GUIContent("Editable");
                    _editable = EditorGUILayout.ToggleLeft(label, _editable);
                }
            }
        }

        private void BakedPropertiesGUI()
        {
            BakedMaterialProperty bakedProperty = 
                _targetClip.PresetRef ? _targetClip.PresetRef : _targetClip.BakedMaterialProperty;
            
            if(bakedProperty == null)
            {
                EditorGUILayout.HelpBox("BakedProperties is null", MessageType.Error);
                return;
            }
            
            if (_editor == null)
            {
                _editor = (BakedMaterialPropertiesEditor)CreateEditor(bakedProperty);
            }
            else if(_editor.target != bakedProperty)
            {
                DestroyImmediate(_editor);
                _editor = null;
                _editor = (BakedMaterialPropertiesEditor)CreateEditor(bakedProperty);
            }
            
            if(_editor != null)
                _editor.OnInspectorGUI();
            
        }
        
        #endregion //--- End GUI ---//
        
        #region AssetsHandle
        //--- Property Assets Handle ---//
        private BakedMaterialProperty CreatePresetFromProps(MaterialProps props)
        {
            var preset = ScriptableObject.CreateInstance<BakedMaterialProperty>();
            preset.name = target.name;
            var materialProps = preset.MaterialProps;
            materialProps.Colors = new List<MaterialProp<Color>>(props.Colors);
            materialProps.Floats = new List<MaterialProp<float>>(props.Floats);
            return preset;
        }

        private void ExportProfile(BakedMaterialProperty preset)
        {
            if (preset == null) return;
            
            var assetName = target == null ? "defaultProfile" : $"BakedProperties_{target.name}";

            BakedMaterialProperty profileToSave = Instantiate(preset);

            EditorUtility.SetDirty(profileToSave);
            var defaultPath = Application.dataPath;
            var path = EditorUtility.SaveFilePanelInProject(
                "Save profile",
                assetName, 
                "asset",
                "Save BakedProperties ScriptableObject",
                defaultPath);
            
            if (path.Length != 0)
            {
                var fullPath = Path.Join(Application.dataPath, path.Replace("Assets/", "\\"));
                if (File.Exists(fullPath))
                {
                    Debug.Log($"{GetType()}: delete existing: {fullPath}");
                    AssetDatabase.DeleteAsset(path);
                }
                AssetDatabase.CreateAsset(profileToSave, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Saved : {path}");
            }
        }
        
        #endregion //--- End Property Assets Handle ---//
        
    }
}
