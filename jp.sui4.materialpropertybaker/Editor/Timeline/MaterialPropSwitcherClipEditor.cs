using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using File = System.IO.File;

namespace sui4.MaterialPropertyBaker.Timeline
{
    [CustomEditor(typeof(MaterialPropSwitcherClip))]
    [CanEditMultipleObjects]
    public class MaterialPropSwitcherClipEditor : Editor
    {
        private SerializedProperty _bakedProperties;
        private SerializedProperty _presetRef;
        private SerializedProperty _syncWithPreset;

        private BakedPropertiesEditor _editor;
        private void OnEnable()
        {
            if(target == null)
                return;
            var t = (MaterialPropSwitcherClip)target;

            _bakedProperties = serializedObject.FindProperty("_bakedProperties");
            _presetRef = serializedObject.FindProperty("_presetRef");
            _syncWithPreset = serializedObject.FindProperty("_syncWithPreset");
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
            var clip = (MaterialPropSwitcherClip)target;


            using (new EditorGUILayout.VerticalScope("box"))
            {
                PresetGUI();
            }
            
            EditorGUILayout.Separator();

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUI.DisabledScope(_syncWithPreset.boolValue))
                {
                    // Property Editor
                    if (clip.SyncWithPreset)
                    {
                        if (clip.PresetRef == null)
                        {
                            clip.SyncWithPreset = false;
                        }
                        else
                        {
                            clip.InstantiateBakedPropertiesFromPreset();
                        }
                        serializedObject.Update();
                    }
                    BakedPropertiesGUI();
                    
                    EditorGUILayout.Separator();
            
                    // Export button
                    using (var h = new EditorGUILayout.HorizontalScope())
                    {
                        var tmp = GUI.backgroundColor;
                        GUI.backgroundColor = Color.cyan;
                        if(GUILayout.Button("Export"))
                        {
                            var preset = CreatePresetFromProps(clip.BakedProperties.MaterialProps);
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
                    if (_presetRef.objectReferenceValue != null)
                    {
                        ((BakedProperties)_presetRef.objectReferenceValue).UpdateShaderID();
                    }
                    serializedObject.ApplyModifiedProperties();
                }
            }

            var clip = (MaterialPropSwitcherClip)target;
            // Load Save buttons
            if (clip.PresetRef != null)
            {
                using (var h = new EditorGUILayout.HorizontalScope())
                {
                    var tmp = GUI.backgroundColor;
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("Load"))
                    {
                        clip.InstantiateBakedPropertiesFromPreset();
                    }
                
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("Update Preset"))
                        UpdatePreset();
                    GUI.backgroundColor = tmp;
                }
                using (var changeCheck = new EditorGUI.ChangeCheckScope())
                {
                    var label = new GUIContent("Sync with preset");
                    EditorGUILayout.PropertyField(_syncWithPreset, label);
                    if (changeCheck.changed)
                        serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void BakedPropertiesGUI()
        {
            var bakedProperties = _bakedProperties.objectReferenceValue as BakedProperties;
            if(bakedProperties == null)
            {
                EditorGUILayout.HelpBox("BakedProperties is null", MessageType.Error);
                return;
            }
            
            if (_editor == null)
            {
                _editor = (BakedPropertiesEditor)Editor.CreateEditor(bakedProperties);
            }
            else if(_editor.target != bakedProperties)
            {
                DestroyImmediate(_editor);
                _editor = null;
                _editor = (BakedPropertiesEditor)Editor.CreateEditor(bakedProperties);
            }
            
            if(_editor != null)
                _editor.OnInspectorGUI();
            
        }
        private void MaterialPropsGUI(SerializedProperty materialProps)
        {
            var colors = materialProps.FindPropertyRelative("_colors");
            var floats = materialProps.FindPropertyRelative("_floats");
            
            serializedObject.Update();
            
            EditorGUI.BeginChangeCheck();
            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                PropGUI(colors);
            
                EditorGUILayout.Separator();
                EditorGUILayout.Separator();

                PropGUI(floats);
            
                EditorGUILayout.Separator();
                EditorGUILayout.Separator();

                if (changeCheckScope.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
        
        private SerializedProperty _matProp;
        private SerializedProperty _propName;
        private SerializedProperty _value;
        private void PropGUI(SerializedProperty matProps)
        {
            for(int i = 0; i < matProps.arraySize; i++)
            {
                _matProp = matProps.GetArrayElementAtIndex(i);
                _propName = _matProp.FindPropertyRelative("_name");
                _value = _matProp.FindPropertyRelative("_value");
                
                var label = new GUIContent(Utils.UnderscoresToSpaces(_propName.stringValue));

                EditorGUILayout.PropertyField(_value, label);
            }
        }
        #endregion //--- End GUI ---//
        
        #region AssetsHandle
        //--- Property Assets Handle ---//
        private BakedProperties CreatePresetFromProps(MaterialProps props)
        {
            var preset = ScriptableObject.CreateInstance<BakedProperties>();
            preset.name = target.name;
            var materialProps = preset.MaterialProps;
            materialProps.Colors = new List<MaterialProp<Color>>(props.Colors);
            materialProps.Floats = new List<MaterialProp<float>>(props.Floats);
            return preset;
        }

        private void ExportProfile(BakedProperties preset)
        {
            if (preset == null) return;
            
            var assetName = target == null ? "defaultProfile" : $"BakedProperties_{target.name}";

            BakedProperties profileToSave = Instantiate(preset);

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
        
        // apply to preset(Override)
        private void UpdatePreset()
        {
            var clip = (MaterialPropSwitcherClip)target;
            var props = clip.BakedProperties.MaterialProps;
            // 単純にやると、参照渡しになって、変更が同期されてしまうので、一旦コピー
            // Listになってる各MaterialPropがクラスのため、参照になっちゃう
            props.GetCopyProperties(out var cList, out var fList);
            
            clip.PresetRef.MaterialProps.Colors = cList;
            clip.PresetRef.MaterialProps.Floats = fList;
            EditorUtility.SetDirty(clip.PresetRef);
            AssetDatabase.SaveAssetIfDirty(clip.PresetRef);
        }

        #endregion //--- End Property Assets Handle ---//
        
    }
}
