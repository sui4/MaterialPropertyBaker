using System;
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
        private SerializedProperty _editable;
        private SerializedProperty _props;
        
        private BakedMaterialPropertiesEditor _editor;
        private MaterialPropSwitcherClip _targetClip;

        // プリセットが外れたときに、プリセットの値を_bakedPropertyに引き継ぐための変数
        private BakedMaterialProperty _presetRefPrev;

        private void OnEnable()
        {
            if(target == null)
                return;
            _targetClip = (MaterialPropSwitcherClip)target;

            _presetRef = serializedObject.FindProperty("_presetRef");
            _editable = serializedObject.FindProperty("_editable");
            _props = serializedObject.FindProperty("_props");
        }

        #region GUI

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            if (target == null) return;
            _presetRefPrev = _targetClip.PresetRef;
            serializedObject.Update();

            // initialize
            if (_targetClip.Props == null)
            {
                _targetClip.Props = new MaterialProps();
            }
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                PresetRefGUI();
            }
            
            EditorGUILayout.Separator();

            // Properties GUI
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUI.DisabledScope(_targetClip.PresetRef != null && !_editable.boolValue))
                {
                    // Export button
                    ExportButtonGUI();

                    PropertiesGUI();
                }
            }
        }

        private void ExportButtonGUI()
        {
            var tmp = GUI.backgroundColor;
            GUI.backgroundColor = Color.cyan;
            var label = _targetClip.PresetRef == null ? "Export" : "Save as";
            if(GUILayout.Button(label))
            {
                if (_targetClip.PresetRef != null)
                {
                    ExportProfile(_targetClip.PresetRef);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            GUI.backgroundColor = tmp;
        }
        
        private void PresetRefGUI()
        {
            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(_presetRef, new GUIContent("Preset Profile"));

                // Load Save buttons
                if (_targetClip.PresetRef != null)
                {
                    EditorGUILayout.PropertyField(_editable, new GUIContent("Edit Preset"));
                }
                
                if (changeCheck.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    var preset = (BakedMaterialProperty)_presetRef.objectReferenceValue;
                    if (preset != null)
                    {
                        preset.UpdateShaderID();
                    }
                    else if(_presetRefPrev != null)
                    {
                        RenewValueFromPreset(_presetRefPrev); 
                    }
                    _presetRefPrev = preset;

                    serializedObject.Update();
                }
            }
        }

        private void PropertiesGUI()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                if (_targetClip.PresetRef != null)
                {
                    var so = new SerializedObject(_presetRef.objectReferenceValue);
                    var presetProps = so.FindProperty("_materialProps");
                    EditorGUILayout.PropertyField(presetProps);
                    if (change.changed)
                    {
                        so.ApplyModifiedProperties();
                        so.Update();
                        EditorUtility.SetDirty(_presetRef.objectReferenceValue);
                        AssetDatabase.SaveAssets();
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(_props);
                    if (change.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        serializedObject.Update();
                    }
                }

            }
        }

        private void BakedPropertyGUI(BakedMaterialProperty bakedProperty)
        {
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

            if (_editor != null)
            {
               _editor.OnInspectorGUI();
                EditorGUILayout.Separator();
            }
        }
        
        #endregion //--- End GUI ---//
        
        #region AssetsHandle
        //--- Property Assets Handle ---//

        private void RenewValueFromPreset(in BakedMaterialProperty presetRef)
        {
            if(presetRef == null) return;

            _targetClip.Props.CopyValuesFromOther(presetRef.MaterialProps);
            Debug.Log($"BakedProperties renewed from Preset: {_targetClip.PresetRef.name}");
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
                _targetClip.PresetRef = profileToSave;
                AssetDatabase.Refresh();
                Debug.Log($"Saved : {path}");
            }
        }
        
        #endregion //--- End Property Assets Handle ---//
        
    }
}
