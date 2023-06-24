using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(MaterialPresetViewer))]
    public class MaterialPresetViewerEditor: Editor
    {
        private SerializedProperty _materialGroups;
        private SerializedProperty _presets;
        private void OnEnable()
        {
            _materialGroups = serializedObject.FindProperty("_materialGroups");
            _presets = serializedObject.FindProperty("_presets");
        }
        
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();

            var viewer = (MaterialPresetViewer)target;
            if (viewer == null)
                return;
            
            serializedObject.Update();

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(_materialGroups);
                if (change.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                // Header of List
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Presets");

                    if (GUILayout.Button("+"))
                    {
                        viewer.Presets.Add(null);
                        serializedObject.Update();
                    }
                }
                
                EditorGUILayout.Separator();

                EditorGUI.indentLevel++;
                for (int pi = 0; pi < _presets.arraySize; pi++)
                {
                    SerializedProperty presetProp = _presets.GetArrayElementAtIndex(pi);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (var change = new EditorGUI.ChangeCheckScope())
                        {
                            EditorGUILayout.PropertyField(presetProp, new GUIContent());
                            if (change.changed)
                            {
                                serializedObject.ApplyModifiedProperties();
                                ((BakedProperties)presetProp.objectReferenceValue).UpdateShaderID();
                            }
                        }

                        var tmp = GUI.backgroundColor;
                        GUI.backgroundColor = Color.green;
                        if (GUILayout.Button("Apply"))
                        {
                            var preset = (BakedProperties)presetProp.objectReferenceValue;
                            if(preset == null)
                                continue;
                            preset.UpdateShaderID();
                            viewer.ApplyPreset(preset);
                        }

                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button("-"))
                        {
                            viewer.Presets.RemoveAt(pi);
                            serializedObject.Update();
                        }

                        GUI.backgroundColor = tmp;
                    }
                }
                EditorGUI.indentLevel--;
            }

            if (GUILayout.Button("Reset View"))
            {
                viewer.ResetView();
            }
                
        }
    }
}