using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace sui4.MaterialPropertyBaker.Timeline
{
    [CustomEditor(typeof(MultiMaterialPropClip))]
    public class MultiMaterialPropClipInspectorEditor : Editor
    {
        private SerializedProperty _bakedPropertyGroupProp;
        private bool _editable;
        private BakedPropertyGroupEditor _presetEditor;
        private MultiMaterialPropClip Target => (MultiMaterialPropClip)target;

        private void OnEnable()
        {
            _bakedPropertyGroupProp = serializedObject.FindProperty("_bakedPropertyGroup");
        }

        public override void OnInspectorGUI()
        {
            PresetRefGUI();
            EditorGUILayout.Separator();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUI.DisabledScope(!_editable))
                    PropertyGroupEditor(Target.BakedPropertyGroup);
            }
        }

        private void PresetRefGUI()
        {
            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(_bakedPropertyGroupProp, new GUIContent("Preset Profile"));

                // Load Save buttons
                if (Target.BakedPropertyGroup != null)
                {
                    _editable = EditorGUILayout.Toggle("Edit Preset", _editable);
                }

                if (changeCheck.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);

                    serializedObject.Update();
                }
            }
        }

        private void PropertyGroupEditor(BakedPropertyGroup bakedPropertyGroup)
        {
            if (bakedPropertyGroup == null)
                return;
            if (_presetEditor == null)
            {
                _presetEditor = (BakedPropertyGroupEditor)CreateEditor(bakedPropertyGroup);
            }
            else if (_presetEditor.target != bakedPropertyGroup)
            {
                DestroyImmediate(_presetEditor);
                _presetEditor = null;
                _presetEditor = (BakedPropertyGroupEditor)CreateEditor(bakedPropertyGroup);
            }

            if (_presetEditor != null)
            {
                _presetEditor.OnInspectorGUI();
            }
        }
    }
}