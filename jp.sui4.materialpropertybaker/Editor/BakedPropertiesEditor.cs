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

        private bool _editMode;

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
                _editMode = EditorGUILayout.Toggle("Edit Mode", _editMode);
                
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

        private SerializedProperty _property;
        private SerializedProperty _value;
        private void PropsGUI(SerializedProperty props)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(props.displayName);
            }
            for (int pi = 0; pi < props.arraySize; pi++)
            {
                SerializedProperty prop = props.GetArrayElementAtIndex(pi);
                _property = prop.FindPropertyRelative("property");
                _value = prop.FindPropertyRelative("value");
                
                using (new GUILayout.HorizontalScope())
                {
                    if (_editMode)
                    {
                        EditorGUILayout.PropertyField(_property, new GUIContent());
                        EditorGUILayout.PropertyField(_value, new GUIContent());
                        // remove button
                        var tmp = GUI.backgroundColor;
                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button("-", GUILayout.Width(30)))
                        {
                            props.DeleteArrayElementAtIndex(pi);
                        }
                        GUI.backgroundColor = tmp;
                    }
                    else
                    {
                        var label = Utils.UnderscoresToSpaces(_property.stringValue);
                        label = label.Length == 0 ? " " : label;
                        EditorGUILayout.PropertyField(_value, new GUIContent(label));
                    }
 
                }                
            }

            if (_editMode)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.Separator();
                    if (GUILayout.Button("+", GUILayout.Width(40)))
                    {
                        props.InsertArrayElementAtIndex(props.arraySize);
                    }  
                }
            }

        }
    }
}