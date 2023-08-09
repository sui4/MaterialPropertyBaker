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
        private List<bool> _foldouts = new List<bool>();

        private List<BakedMaterialPropertiesEditor> _bakedPropertyEditors = new List<BakedMaterialPropertiesEditor>();
        private void OnEnable()
        {
            _presetIDPairsProp = serializedObject.FindProperty("_presetIDPairs");
            for (int pi = 0; pi < _presetIDPairsProp.arraySize; pi++)
            {
                _bakedPropertyEditors.Add(null);
                _foldouts.Add(SessionState.GetBool("foldout" + pi, true));
            }
        }

        private void SaveFoldoutState(int index, bool state)
        {
            SessionState.SetBool("foldout" + index, state);
            _foldouts[index] = state;
        }
        
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            if(Target == null) return;
            
            serializedObject.Update();
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                PairsGUI();
                if (change.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

            WarningGUI(Target.Warnings);
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
                    EditorGUILayout.PropertyField(presetProp);
                
                    var preset = presetProp.objectReferenceValue as BakedMaterialProperty;
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        BakedPropertyEditorGUI(preset, pi);
                    }
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
                EditorGUILayout.LabelField($"{bakedProperty.name}", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                _bakedPropertyEditors[index].OnInspectorGUI();
                EditorGUI.indentLevel--;

            }
        }

        private void WarningGUI(List<string> warnings)
        {
            // helpBox
            if (warnings.Count > 0)
            {
                foreach (var warning in warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }
        }
    }
}