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
        }

        #region GUI

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            if (target == null) return;
            _presetRefPrev = _targetClip.PresetRef;
            serializedObject.Update();

            // initialize
            if (_targetClip.BakedMaterialProperty == null)
            {
                CreateAndSaveBakedProperty(_targetClip.PresetRef);
            }

            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                PresetGUI();
            }
            
            EditorGUILayout.Separator();

            // Properties GUI
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUI.DisabledScope(_targetClip.PresetRef != null && !_editable.boolValue))
                {
                    BakedPropertiesGUI();
                }
            }
        }
        
        //--- GUI ---//
        private void PresetGUI()
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

            if (_editor != null)
            {
                // Export button
                using (var h = new EditorGUILayout.HorizontalScope())
                {
                    var tmp = GUI.backgroundColor;
                    GUI.backgroundColor = Color.cyan;
                    var label = _targetClip.PresetRef == null ? "Export" : "Save as";
                    if(GUILayout.Button(label))
                    {
                        ExportProfile(bakedProperty);
                    }
                    GUI.backgroundColor = tmp;
                }
                
                _editor.OnInspectorGUI();
                EditorGUILayout.Separator();
            }
        }
        
        #endregion //--- End GUI ---//
        
        #region AssetsHandle
        //--- Property Assets Handle ---//
        
        private void CreateAndSaveBakedProperty(BakedMaterialProperty presetRef)
        {
            if (_targetClip.BakedMaterialProperty != null)
            {
                DestroyBakedPropertyIfChild();
            }
            if (_targetClip.BakedMaterialProperty == null)
            {
                if (presetRef != null)
                {
                    var baked = Instantiate(presetRef);
                    baked.name = _targetClip.name + "_" + presetRef.name;
                    AssetDatabase.AddObjectToAsset(baked, _targetClip);
                    AssetDatabase.SaveAssets();
                    _targetClip.BakedMaterialProperty = baked;
                    AssetDatabase.Refresh();
                    Debug.Log($"BakedProperties created from Preset: {_targetClip.BakedMaterialProperty.name}");

                }
                else
                {
                    var baked = CreateInstance<BakedMaterialProperty>();
                    baked.name = _targetClip.name + "_BakedProperties";
                    AssetDatabase.AddObjectToAsset(baked, _targetClip);
                    AssetDatabase.SaveAssets();
                    _targetClip.BakedMaterialProperty = baked;
                    AssetDatabase.Refresh();
                    Debug.Log($"BakedProperties created: {_targetClip.BakedMaterialProperty.name}");
                }
            }
            else
            {
                Debug.LogError("MaterialPropSwitcherClipInspectorEditor: Failed to destroy existing BakedProperties.");
            }
        }

        private void RenewValueFromPreset(BakedMaterialProperty presetRef)
        {
            if(presetRef == null) return;

            if (_targetClip.BakedMaterialProperty != null)
            {
                if (presetRef != null)
                {
                    _targetClip.BakedMaterialProperty.CopyValuesFromOther(presetRef);
                    _targetClip.BakedMaterialProperty.name = _targetClip.name + "_" + presetRef.name;
                    
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.Log($"BakedProperties renewed from Preset: {_targetClip.BakedMaterialProperty.name}");
                }
            }
        }
        private void DestroyBakedPropertyIfChild()
        {
            var bakedProperties = _targetClip.BakedMaterialProperty;
            if(bakedProperties == null) return;
            
            // _bakedPropertiesのアセットパスを取得
            string bakedPropertiesPath = AssetDatabase.GetAssetPath(bakedProperties);
            if (string.IsNullOrEmpty(bakedPropertiesPath))
            {
                DestroyImmediate(bakedProperties);
                bakedProperties = null;
            }
            else
            {
                // このオブジェクト自身のアセットパスを取得
                string thisAssetPath = AssetDatabase.GetAssetPath(this);

                // _bakedPropertiesが自身の子のアセットであるかどうかを確認
                if (!string.IsNullOrEmpty(bakedPropertiesPath) &&
                    bakedPropertiesPath.StartsWith(thisAssetPath))
                {
                    Debug.Log($"Destroy BakedProperties: {bakedProperties.name}");
                    Undo.DestroyObjectImmediate(bakedProperties);
                    DestroyImmediate(bakedProperties, true);
                    _targetClip.BakedMaterialProperty = null;
                }
            }
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
