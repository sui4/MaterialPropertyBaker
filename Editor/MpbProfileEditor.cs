using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

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
                PropsGUI(colors, index, true);
                EditorGUI.indentLevel--;
            }
            
            // Floats
            _floatsFoldoutList[index] = EditorGUILayout.Foldout(_floatsFoldoutList[index], "Floats");
            SaveFoldoutState(index, FloatsFoldoutKey, _floatsFoldoutList[index], ref _floatsFoldoutList);
            if (_floatsFoldoutList[index])
            {
                EditorGUI.indentLevel++;
                PropsGUI(floats, index);
                EditorGUI.indentLevel--;
            }
        }
        
        
        
        private void PropsGUI(SerializedProperty propsList, int index, bool isColor = false)
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
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    ShowNewRecorderMenu(index, isColor);
                }
            }
        }

        private void ShowNewRecorderMenu(int index, bool isColor)
        {
            var addPropertyMenu = new GenericMenu();
            var shader = Target.MaterialPropsList[index].Shader;
            for (var pi = 0; pi < shader.GetPropertyCount(); pi++)
            {
                var propName = shader.GetPropertyName(pi);
                var propType = shader.GetPropertyType(pi);
                if (isColor)
                {
                    // すでに同じ名前のプロパティがある場合は追加しない
                    if (propType != ShaderPropertyType.Color ||
                        Target.MaterialPropsList[index].Colors.Any(c => c.Name == propName))
                        continue;
                       
                    AddPropertyToMenu(propName, addPropertyMenu, index, true);

                }
                else if(propType is ShaderPropertyType.Float or ShaderPropertyType.Range)
                {
                    // すでに同じ名前のプロパティがある場合は追加しない
                    if (Target.MaterialPropsList[index].Floats.Any(f => f.Name == propName))
                        continue;
                    AddPropertyToMenu(propName, addPropertyMenu, index);
                }
            }

            if (addPropertyMenu.GetItemCount() == 0)
            {
                addPropertyMenu.AddDisabledItem(new GUIContent("No Property to Add"));
            }

            addPropertyMenu.ShowAsContext();
        }

        private void AddPropertyToMenu(string propName, GenericMenu menu, int index, bool isColor = false)
        {
            menu.AddItem(new GUIContent(propName), false, data => OnAddProperty((string)data, index, isColor), propName);
        }

        private void OnAddProperty(string propName, int index, bool isColor = false)
        {
            var material = Target.MaterialPropsList[index].Material;
            if (isColor)
            {
                var defaultColor = material == null ? Color.black : material.GetColor(propName);
                var matProp = new MaterialProp<Color>(propName, defaultColor);
                Target.MaterialPropsList[index].Colors.Add(matProp);
            }
            else
            {
                var defaultFloat = material == null ? 0.0f : material.GetFloat(propName);
                var matProp = new MaterialProp<float>(propName, defaultFloat);
                Target.MaterialPropsList[index].Floats.Add(matProp);
            }
            serializedObject.Update();
        }
    }
}