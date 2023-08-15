using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(MpbProfile))]
    public class MpbProfileEditor : Editor
    {
        private SerializedProperty _materialPropsListProp;
        private SerializedProperty _materialPropsProp;
        
        private List<bool> _propFoldoutList = new List<bool>();
        const string PropFoldoutKey = "propFoldout";
        private List<bool> _colorsFoldoutList = new List<bool>();
        const string ColorsFoldoutKey = "colorsFoldout";
        private List<bool> _floatsFoldoutList = new List<bool>();
        const string FloatsFoldoutKey = "floatsFoldout";
        public MpbProfile Target => (MpbProfile)target;
        private void OnEnable()
        {
            _materialPropsListProp = serializedObject.FindProperty("_materialPropsList");
            for (var i = 0; i < _materialPropsListProp.arraySize; i++)
            {
                _propFoldoutList.Add(SessionState.GetBool(PropFoldoutKey + i, false));
                _colorsFoldoutList.Add(SessionState.GetBool(ColorsFoldoutKey + i, false));
                _floatsFoldoutList.Add(SessionState.GetBool(FloatsFoldoutKey + i, false));
            }
        }
        
        private void SaveFoldoutState(int index, string key ,bool state, ref List<bool> foldouts)
        {
            SessionState.SetBool(key + index, state);
            foldouts[index] = state;
        }

        public override void OnInspectorGUI()
        {
            for (var i = 0; i < _materialPropsListProp.arraySize; i++)
            {
                _materialPropsProp = _materialPropsListProp.GetArrayElementAtIndex(i);
                var title = Target.MaterialPropsList[i] == null ? $"MaterialProps {i}" : Target.MaterialPropsList[i].ID;
                _propFoldoutList[i] = EditorGUILayout.Foldout(_propFoldoutList[i], Target.MaterialPropsList[i].ID);
                SaveFoldoutState(i, PropFoldoutKey, _propFoldoutList[i], ref _propFoldoutList);
                if(!_propFoldoutList[i]) continue;
                EditorGUI.indentLevel++;
                MaterialPropsGUI(_materialPropsProp, i);
                EditorGUI.indentLevel--;
            }
        }

        private void MaterialPropsGUI(SerializedProperty materialPropsProp, int index)
        {
            var id = materialPropsProp.FindPropertyRelative("_id");
            var shader = materialPropsProp.FindPropertyRelative("_shader");
            var material = materialPropsProp.FindPropertyRelative("_material");
            var colors = materialPropsProp.FindPropertyRelative("_colors");
            var floats = materialPropsProp.FindPropertyRelative("_floats");
            
            EditorGUILayout.PropertyField(id, new GUIContent("ID"));
            EditorGUILayout.PropertyField(shader);
            EditorGUILayout.PropertyField(material);

            // Colors
            _colorsFoldoutList[index] = EditorGUILayout.Foldout(_colorsFoldoutList[index], "Colors");
            SaveFoldoutState(index, ColorsFoldoutKey, _colorsFoldoutList[index], ref _colorsFoldoutList);
            if (_colorsFoldoutList[index])
            {
                EditorGUI.indentLevel++;
                PropsGUI(colors, true);
                EditorGUI.indentLevel--;
            }
            
            // Floats
            _floatsFoldoutList[index] = EditorGUILayout.Foldout(_floatsFoldoutList[index], "Floats");
            SaveFoldoutState(index, FloatsFoldoutKey, _floatsFoldoutList[index], ref _floatsFoldoutList);
            if (_floatsFoldoutList[index])
            {
                EditorGUI.indentLevel++;
                PropsGUI(floats);
                EditorGUI.indentLevel--;
            }
        }
        
        
        
        private void PropsGUI(SerializedProperty propsList, bool isColor = false)
        {
            if (propsList.arraySize == 0)
            {
                EditorGUILayout.LabelField("List is Empty");
            }
            for (int i = 0; i < propsList.arraySize; i++)
            {
                SerializedProperty prop = propsList.GetArrayElementAtIndex(i);
                var property = prop.FindPropertyRelative("_name");
                var valueProp = prop.FindPropertyRelative("_value");
                var label = Utils.UnderscoresToSpaces(property.stringValue);
                label = label.Length == 0 ? " " : label;
                using (new GUILayout.HorizontalScope())
                {
                    if (isColor)
                        valueProp.colorValue =
                            EditorGUILayout.ColorField(new GUIContent(label), valueProp.colorValue, true, true, true);
                    else
                        EditorGUILayout.PropertyField(valueProp, new GUIContent(label));
                    
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        propsList.DeleteArrayElementAtIndex(i);
                    }
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    propsList.InsertArrayElementAtIndex(propsList.arraySize);
                }
            }
        }
    }
}