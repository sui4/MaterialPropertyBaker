using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomPropertyDrawer(typeof(MaterialProps))]
    public class MaterialPropsPropertyDrawer : PropertyDrawer
    {
        private SerializedProperty _colors;
        private SerializedProperty _floats;
        
        private SerializedProperty _property;
        private SerializedProperty _value;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            _colors = property.FindPropertyRelative("_colors");
            _floats = property.FindPropertyRelative("_floats");
            
            // Colors
            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            PropsGUI(_colors, true);
            EditorGUI.indentLevel--;

            // Floats
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Floats", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            PropsGUI(_floats);

            EditorGUI.EndProperty();
        }

        private void PropsGUI(SerializedProperty propsList, bool isColor = false)
        {
            for (int i = 0; i < propsList.arraySize; i++)
            {
                SerializedProperty prop = propsList.GetArrayElementAtIndex(i);
                _property = prop.FindPropertyRelative("_name");
                _value = prop.FindPropertyRelative("_value");
                var label = Utils.UnderscoresToSpaces(_property.stringValue);
                label = label.Length == 0 ? " " : label;
                if (isColor)
                {
                    _value.colorValue = EditorGUILayout.ColorField(new GUIContent(label), _value.colorValue, true, true, true);
                }
                else
                {
                    EditorGUILayout.PropertyField(_value, new GUIContent(label));
                }
            }
        }
    }


}