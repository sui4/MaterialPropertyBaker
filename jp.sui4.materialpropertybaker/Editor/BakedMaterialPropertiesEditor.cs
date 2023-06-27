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
        private SerializedProperty _materialProps;
        private SerializedProperty _colors;
        private SerializedProperty _floats;


        private MaterialPropertyConfig _materialPropertyConfig;
        public MaterialPropertyConfig MaterialPropertyConfig
        {
            get => _materialPropertyConfig;
            set => _materialPropertyConfig = value;
        }

        private void OnEnable()
        {
            if (target == null)
                return;
            _shaderName = serializedObject.FindProperty("_shaderName");
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
                EditorGUILayout.Separator();
                
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
                // if(_materialPropertyConfig != null)
                // {
                //     var tmp = GUI.backgroundColor;
                //     GUI.backgroundColor = Color.red;
                //     if (GUILayout.Button("Delete According to ShaderProperties"))
                //     {
                //         ((BakedMaterialProperty)target).DeleteUnEditableProperties();
                //     }
                //     GUI.backgroundColor = tmp;
                // }
                  
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
                    var label = Utils.UnderscoresToSpaces(_property.stringValue);
                    label = label.Length == 0 ? " " : label;
                    EditorGUILayout.PropertyField(_value, new GUIContent(label));
                    // remove button
                    var tmp = GUI.backgroundColor;
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("-", GUILayout.Width(30)))
                    {
                        props.DeleteArrayElementAtIndex(pi);
                    }
                    GUI.backgroundColor = tmp;
                }
            }
            // add from shader properties
            if (_materialPropertyConfig != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.Separator();
                    AddPropertyPopupGUI(props, type);
                }
            }
        }

        private void AddPropertyPopupGUI(SerializedProperty props, ShaderPropertyType spType, Type type = null)
        {
            var config = _materialPropertyConfig;
            
            if (config == null)
                return;

            var bp = (BakedMaterialProperty)target;

            // 一致する型のプロパティを取得
            var propertySelectList = new List<string> { "Add Property" };
            
            for(int pi = 0; pi < config.PropertyNames.Count; pi++)
            {
                var pName = config.PropertyNames[pi];
                var pType = config.PropertyTypes[pi];
                // TODO: ShaderPropertyTypeと、MaterialPropsのTypeは1:1じゃない。Range, floatはともにfloatに対応する
                // 型が一致、かつ、MaterialPropsに存在しない かつ editableなプロパティのみ追加
                if (pType == spType && config.HasEditableProperty(pName) && !bp.MaterialProps.HasProperties(pName, pType))
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