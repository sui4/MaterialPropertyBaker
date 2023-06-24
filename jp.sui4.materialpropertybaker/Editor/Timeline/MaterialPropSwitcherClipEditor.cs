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
        private SerializedProperty _props;
        private SerializedProperty _presetRef;
        private SerializedProperty _syncWithPreset;
        private void OnEnable()
        {
            if(target == null)
                return;
            var t = (MaterialPropSwitcherClip)target;

            _props = serializedObject.FindProperty("_materialProps");
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

            // Load Save buttons
            if (clip.PresetRef != null)
            {
                using (var h = new EditorGUILayout.HorizontalScope())
                {
                    var tmp = GUI.backgroundColor;
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("Load"))
                    {
                        clip.LoadProfile(clip.PresetRef);
                    }
                
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("Update Preset"))
                    {
                        UpdatePreset();
                    }
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

            EditorGUILayout.Separator();

            // Export button
            using (var h = new EditorGUILayout.HorizontalScope())
            {
                var tmp = GUI.backgroundColor;
                GUI.backgroundColor = Color.cyan;
                if(GUILayout.Button("Export"))
                {
                    var preset = CreatePresetFromProps(clip.MaterialProps);
                    ExportProfile(preset);
                }
                GUI.backgroundColor = tmp;
            }
            
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            // Property Editor
            using (new EditorGUI.DisabledScope(_syncWithPreset.boolValue))
            {
                if (clip.SyncWithPreset)
                {
                    if (clip.PresetRef == null)
                    {
                        clip.SyncWithPreset = false;
                        serializedObject.Update();
                    }
                    else
                    {
                        clip.LoadProfile(clip.PresetRef);
                    }
                }
                MaterialPropsGUI(_props);
            }
        }

        
        //--- GUI ---//
        private void MaterialPropsGUI(SerializedProperty materialProps)
        {
            var colors = materialProps.FindPropertyRelative("_colors");
            var floats = materialProps.FindPropertyRelative("_floats");
            var ints = materialProps.FindPropertyRelative("_ints");
            
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

                PropGUI(ints);
                EditorGUILayout.Separator();

                if (changeCheckScope.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
        
        private void PropGUI(SerializedProperty matProps)
        {
            for(int i = 0; i < matProps.arraySize; i++)
            {
                var matProp = matProps.GetArrayElementAtIndex(i);
                var propName = matProp.FindPropertyRelative("property");
                var id = matProp.FindPropertyRelative("id");
                var value = matProp.FindPropertyRelative("value");
                
                var label = new GUIContent(Utils.UnderscoresToSpaces(propName.stringValue));

                EditorGUILayout.PropertyField(value, label);
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
            materialProps.Colors = new List<MaterialPropColor>(props.Colors);
            materialProps.Floats = new List<MaterialPropFloat>(props.Floats);
            materialProps.Ints = new List<MaterialPropInt>(props.Ints);
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
            var props = clip.MaterialProps;
            // 単純にやると、参照渡しになって、変更が同期されてしまうので、一旦コピー
            // Listになってる各MaterialPropがクラスのため、参照になっちゃう
            props.GetCopyProperties(out var cList, out var fList, out var iList);
            
            clip.PresetRef.MaterialProps.Colors = cList;
            clip.PresetRef.MaterialProps.Floats = fList;
            clip.PresetRef.MaterialProps.Ints = iList;
            EditorUtility.SetDirty(clip.PresetRef);
            AssetDatabase.SaveAssetIfDirty(clip.PresetRef);
        }

        #endregion //--- End Property Assets Handle ---//
        
    }
}
