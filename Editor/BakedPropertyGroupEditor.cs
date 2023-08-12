using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(BakedPropertyGroup))]
    public class BakedPropertyGroupEditor : Editor
    {
        private SerializedProperty _presetIDPairsProp;
        private BakedPropertyGroup Target => (BakedPropertyGroup)target;
        private List<string> _warnings = new List<string>();
        private readonly List<bool> _foldouts = new();
        private readonly List<bool> _foldoutsPreset = new();

        private readonly List<BakedMaterialPropertiesEditor> _bakedPropertyEditors = new();
        private void OnEnable()
        {
            if(target == null) return;
            _presetIDPairsProp = serializedObject.FindProperty("_presetIDPairs");
            for (var pi = 0; pi < _presetIDPairsProp.arraySize; pi++)
            {
                _bakedPropertyEditors.Add(null);
                _foldouts.Add(SessionState.GetBool("foldout" + pi, true));
                _foldoutsPreset.Add(SessionState.GetBool("foldoutPreset" + pi, true));
            }
        }

        private void SaveFoldoutState(int index, bool state)
        {
            SessionState.SetBool("foldout" + index, state);
            _foldouts[index] = state;
        }
        
        private void SavePresetFoldoutState(int index, bool state)
        {
            SessionState.SetBool("foldoutPreset" + index, state);
            _foldoutsPreset[index] = state;
        }
        
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            if(Target == null) return;

            serializedObject.Update();
            EditorUtils.WarningGUI(Target.Warnings);

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                PairsGUI();
                if (change.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void PairsGUI()
        {
            EditorGUILayout.LabelField("Preset ID Pairs", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            for (var pi = 0; pi < _presetIDPairsProp.arraySize; pi++)
            {
                var pairProp = _presetIDPairsProp.GetArrayElementAtIndex(pi);
                var presetProp = pairProp.FindPropertyRelative("_preset");
                var idProp = pairProp.FindPropertyRelative("_id");
                var configProp = pairProp.FindPropertyRelative("_config");
                var tmp = EditorGUILayout.Foldout(_foldouts[pi], idProp.stringValue);
                if (tmp != _foldouts[pi])
                {
                    _foldouts[pi] = tmp;
                    SaveFoldoutState(pi, _foldouts[pi]);
                }
                if (!_foldouts[pi]) continue;
                
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(idProp);
                    EditorGUILayout.PropertyField(configProp);
                    EditorGUILayout.PropertyField(presetProp);
                    var preset = presetProp.objectReferenceValue as BakedMaterialProperty;
                    using (new EditorGUI.IndentLevelScope())
                    {
                        BakedPropertyEditorGUI(preset, pi);
                    }
                    _warnings.Clear();
                    Target.PresetIDPairs[pi].GetWarnings(_warnings);
                    EditorUtils.WarningGUI(_warnings);
                }
            }
            EditorGUI.indentLevel--;
        }

        private void BakedPropertyEditorGUI(BakedMaterialProperty bakedProperty, int index)
        {
            if (index >= _bakedPropertyEditors.Count || index < 0 || bakedProperty == null)
            {
                return;
            }

            if (_bakedPropertyEditors[index] == null)
            {
                _bakedPropertyEditors[index] = (BakedMaterialPropertiesEditor)CreateEditor(bakedProperty);
            }
            else if (_bakedPropertyEditors[index].target != bakedProperty)
            {
                DestroyImmediate(_bakedPropertyEditors[index]);
                _bakedPropertyEditors[index] = null;
                _bakedPropertyEditors[index] = (BakedMaterialPropertiesEditor)CreateEditor(bakedProperty);
            }

            if (_bakedPropertyEditors[index] != null)
            {
                var tmp = EditorGUILayout.Foldout(_foldoutsPreset[index], $"{bakedProperty.name}");
                if (tmp != _foldoutsPreset[index])
                {
                    _foldoutsPreset[index] = tmp;
                    SavePresetFoldoutState(index, _foldoutsPreset[index]);
                }
                if (!_foldoutsPreset[index]) return;
                // EditorGUILayout.LabelField(, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                _bakedPropertyEditors[index].OnInspectorGUI();
                EditorGUI.indentLevel--;

            }
        }


    }
}