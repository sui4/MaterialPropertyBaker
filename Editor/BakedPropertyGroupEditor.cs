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

        private static class Styles
        {
            public static readonly GUIContent IDLabel = new GUIContent("ID");
            public static readonly GUIContent PresetLabel = new GUIContent("Preset Property");
        }

        private void OnEnable()
        {
            if (target == null) return;
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
            if (Target == null) return;

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
                    EditorGUILayout.PropertyField(idProp, Styles.IDLabel);
                    EditorGUILayout.PropertyField(configProp);

                    var preset = presetProp.objectReferenceValue as BakedMaterialProperty;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(presetProp, Styles.PresetLabel);
                        if (presetProp.objectReferenceValue != null)
                        {
                            ClonePresetButtonGUI(preset, pi);
                        }
                        else
                        {
                            NewPresetButtonGUI(pi);
                        }
                    }

                    using (new EditorGUI.IndentLevelScope())
                    {
                        BakedPropertyEditorGUI(preset, pi);
                    }

                    _warnings.Clear();
                    var warnings = new List<string>();
                    Target.PresetIDPairs[pi].GetWarnings(warnings);
                    EditorUtils.WarningGUI(warnings);
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

        private void ClonePresetButtonGUI(BakedMaterialProperty preset, int index)
        {
            if (GUILayout.Button("Clone", GUILayout.Width(50)))
            {
                var clone = Instantiate(preset);
                clone.name = preset.name + "_Clone";

                EditorUtils.CreateAsset(clone, out var saved, GetType(), clone.name, $"Clone {preset.name}", "");
                if (saved == null) return;

                Target.PresetIDPairs[index].Preset = saved as BakedMaterialProperty;
                EditorUtility.SetDirty(Target);
                AssetDatabase.SaveAssetIfDirty(Target);
                serializedObject.Update();
            }
        }

        private void NewPresetButtonGUI(int index)
        {
            if (GUILayout.Button("New", GUILayout.Width(50)))
            {
                var preset = CreateInstance<BakedMaterialProperty>();
                preset.name = $"{Target.PresetIDPairs[index].ID}_property";
                preset.SyncPropertyWithConfig(Target.PresetIDPairs[index].Config);
                preset.Config = Target.PresetIDPairs[index].Config;
                EditorUtils.CreateAsset(preset, out var saved, GetType(), preset.name, $"New Baked Property", "");
                if (saved == null) return;

                Target.PresetIDPairs[index].Preset = saved as BakedMaterialProperty;
                EditorUtility.SetDirty(Target);
                AssetDatabase.SaveAssetIfDirty(Target);
                serializedObject.Update();
            }
        }
    }
}