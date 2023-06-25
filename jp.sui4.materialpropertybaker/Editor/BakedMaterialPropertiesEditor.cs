using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(BakedMaterialProperty))]
    public class BakedMaterialPropertiesEditor : Editor
    {
        private SerializedProperty _shaderName;
        private SerializedProperty _shaderProperties;
        private SerializedProperty _materialProps;
        private SerializedProperty _colors;
        private SerializedProperty _floats;

        private bool _forceEditMode;

        private void OnEnable()
        {
            if (target == null)
                return;
            _shaderName = serializedObject.FindProperty("_shaderName");
            _shaderProperties = serializedObject.FindProperty("_materialPropertyConfig");
            _materialProps = serializedObject.FindProperty("_materialProps");
            _colors = _materialProps.FindPropertyRelative("_colors");
            _floats = _materialProps.FindPropertyRelative("_floats");
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            serializedObject.Update();
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.LabelField("Shader" ,_shaderName.stringValue, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_shaderProperties);
                EditorGUILayout.Separator();
                _forceEditMode = EditorGUILayout.Toggle("Force Edit Mode", _forceEditMode);
                
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    PropsGUI(_colors, ShaderPropertyType.Color);
                }
                EditorGUILayout.Separator();
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    PropsGUI(_floats, ShaderPropertyType.Float);
                }
                
                EditorGUILayout.Separator();
                if(_shaderProperties.objectReferenceValue != null)
                {
                    var tmp = GUI.backgroundColor;
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("Delete According to ShaderProperties"))
                    {
                        ((BakedMaterialProperty)target).DeleteUnEditableProperties();
                    }
                    GUI.backgroundColor = tmp;
                }
                  
                if (change.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
            
        }

        private SerializedProperty _property;
        private SerializedProperty _value;
        private void PropsGUI(SerializedProperty props, ShaderPropertyType type)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(props.displayName);
            }
            for (int pi = 0; pi < props.arraySize; pi++)
            {
                SerializedProperty prop = props.GetArrayElementAtIndex(pi);
                _property = prop.FindPropertyRelative("_name");
                _value = prop.FindPropertyRelative("_value");
                
                using (new GUILayout.HorizontalScope())
                {
                    if (_forceEditMode)
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

            if (_forceEditMode)
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
            else
            {
                // add from shader properties
                var shaderProperties = (MaterialPropertyConfig)_shaderProperties.objectReferenceValue;
                if (shaderProperties != null)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.Separator();
                        AddPropertyPopupGUI(props, type);
                    }
                }
            }
        }

        private void AddPropertyPopupGUI(SerializedProperty props, ShaderPropertyType spType, Type type = null)
        {
            var shaderProperties = (MaterialPropertyConfig)_shaderProperties.objectReferenceValue;
            if (shaderProperties == null)
                return;

            var bp = (BakedMaterialProperty)target;

            // 一致する型のプロパティを取得
            var propertySelectList = new List<string> { "Add Property" };
            
            for(int pi = 0; pi < shaderProperties.PropertyNames.Count; pi++)
            {
                var pName = shaderProperties.PropertyNames[pi];
                var pType = shaderProperties.PropertyTypes[pi];
                // TODO: ShaderPropertyTypeと、MaterialPropsのTypeは1:1じゃない。Range, floatはともにfloatに対応する
                // 型が一致、かつ、MaterialPropsに存在しない かつ editableなプロパティのみ追加
                if (pType == spType && shaderProperties.HasEditableProperty(pName) && !bp.MaterialProps.HasProperties(pName, pType))
                {
                    propertySelectList.Add(pName);
                }
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.Separator();
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var selected = EditorGUILayout.Popup(0, propertySelectList.ToArray());
                    if (change.changed)
                    {
                        if(selected == 0) return;
                        
                        var propName = propertySelectList[selected];
                        bp.MaterialProps.AddProperty(propName, spType);
                        serializedObject.Update();
                    }
                }
            }
        }
        
    }
}