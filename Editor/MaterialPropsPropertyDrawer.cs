using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomPropertyDrawer(typeof(MaterialProps))]
    public class MaterialPropsPropertyDrawer : PropertyDrawer
    {
        private SerializedProperty _colors;
        private SerializedProperty _floats;
        private SerializedProperty _ints;
        private SerializedProperty _textures;
        private SerializedProperty _id;

        private SerializedProperty _property;
        private SerializedProperty _shader;
        private SerializedProperty _value;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            _id = property.FindPropertyRelative("_id");
            _shader = property.FindPropertyRelative("_shader");
            _colors = property.FindPropertyRelative("_colors");
            _floats = property.FindPropertyRelative("_floats");
            _ints = property.FindPropertyRelative("_ints");
            _textures = property.FindPropertyRelative("_textures");

            EditorGUILayout.PropertyField(_id);
            EditorGUILayout.PropertyField(_shader);

            // Colors
            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            PropsGUI(_colors, true);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            // Floats
            EditorGUILayout.LabelField("Floats", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            PropsGUI(_floats);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            // Ints
            EditorGUILayout.LabelField("Ints", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            PropsGUI(_ints);
            EditorGUI.indentLevel--;
            
            // Textures
            EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            PropsGUI(_textures);
            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        private void PropsGUI(SerializedProperty propsList, bool isColor = false)
        {
            for (var i = 0; i < propsList.arraySize; i++)
            {
                SerializedProperty prop = propsList.GetArrayElementAtIndex(i);
                _property = prop.FindPropertyRelative("_name");
                _value = prop.FindPropertyRelative("_value");
                string label = Utils.UnderscoresToSpaces(_property.stringValue);
                label = label.Length == 0 ? " " : label;
                if (isColor)
                {
                    _value.colorValue =
                        EditorGUILayout.ColorField(new GUIContent(label), _value.colorValue, true, true, true);
                }
                else
                {
                    EditorGUILayout.PropertyField(_value, new GUIContent(label));
                }
            }
        }
    }
}