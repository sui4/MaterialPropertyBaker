using System;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(BakedProperties))]
    public class BakedPropertiesEditor : Editor
    {
        private SerializedProperty _shaderName;
        private SerializedProperty _materialProps;
        private SerializedProperty _colors;
        private SerializedProperty _floats;
        private SerializedProperty _ints;

        private void OnEnable()
        {
            if (target == null)
                return;
            _shaderName = serializedObject.FindProperty("_shaderName");
            _materialProps = serializedObject.FindProperty("_materialProps");
            _colors = _materialProps.FindPropertyRelative("_colors");
            _floats = _materialProps.FindPropertyRelative("_floats");
            _ints = _materialProps.FindPropertyRelative("_ints");
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            serializedObject.Update();
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.LabelField("Shader" ,_shaderName.stringValue, EditorStyles.boldLabel);
                EditorGUILayout.Separator();
                
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    PropsGUI(_colors);
                }
                EditorGUILayout.Separator();
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    PropsGUI(_floats);
                }
                EditorGUILayout.Separator();
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    PropsGUI(_ints);
                }
                  
                if (change.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
            
        }

        private void PropsGUI(SerializedProperty props)
        {
            EditorGUILayout.LabelField(props.displayName);
            for (int pi = 0; pi < props.arraySize; pi++)
            {
                SerializedProperty prop = props.GetArrayElementAtIndex(pi);
                var property = prop.FindPropertyRelative("property");
                var value = prop.FindPropertyRelative("value");
                
                using (new GUILayout.HorizontalScope())
                {
                    
                    var label = Utils.UnderscoresToSpaces(property.stringValue);
                    label = label.Length == 0 ? " " : label;
                    // EditorGUILayout.PropertyField(property, label);
                    EditorGUILayout.PropertyField(value, new GUIContent(label));    
                }                
            }
        }
    }
}