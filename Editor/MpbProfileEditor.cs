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
        private readonly List<bool> _propFoldoutList = new();
        private readonly List<bool> _colorsFoldoutList = new();
        private readonly List<bool> _floatsFoldoutList = new();
        private bool _globalColorsFoldout = true;
        private bool _globalFloatsFoldout = true;

        private SerializedProperty _globalPropsProp;
        private SerializedProperty _materialPropsListProp;
        private SerializedProperty _materialPropsProp;
        private MpbProfile Target => (MpbProfile)target;

        private void OnEnable()
        {
            if (target == null) return;
            _materialPropsListProp = serializedObject.FindProperty("_materialPropsList");
            _globalPropsProp = serializedObject.FindProperty("_globalProps");
            Validate();
        }

        private string PropFoldoutKeyAt(string id) => $"{Target.name}_propFoldout_{id}";
        private string ColorsFoldoutKeyAt(string id) => $"{Target.name}_colorsFoldout_{id}";
        private string FloatsFoldoutKeyAt(string id) => $"{Target.name}_floatsFoldout_{id}";

        private void Validate()
        {
            for (var i = _propFoldoutList.Count; i < _materialPropsListProp.arraySize; i++)
            {
                var key = string.IsNullOrWhiteSpace(Target.MaterialPropsList[i].ID)
                    ? i.ToString()
                    : Target.MaterialPropsList[i].ID;
                _propFoldoutList.Add(SessionState.GetBool(PropFoldoutKeyAt(key), true));
                _colorsFoldoutList.Add(SessionState.GetBool(ColorsFoldoutKeyAt(key), true));
                _floatsFoldoutList.Add(SessionState.GetBool(FloatsFoldoutKeyAt(key), true));
            }
        }

        public override void OnInspectorGUI()
        {
            if (target == null) return;
            serializedObject.Update();
            if (_materialPropsListProp.arraySize > _propFoldoutList.Count)
                Validate();

            EditorUtils.WarningGUI(Target.Warnings);
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                // global settings
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    GlobalPropertyGUI(_globalPropsProp);
                }

                using (new GUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Per Property Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    // per id settings
                    for (var i = 0; i < _materialPropsListProp.arraySize; i++)
                    {
                        _materialPropsProp = _materialPropsListProp.GetArrayElementAtIndex(i);
                        string key, title;
                        if (string.IsNullOrWhiteSpace(Target.MaterialPropsList[i].ID))
                        {
                            key = i.ToString();
                            title = $"Material Property {i}";
                        }
                        else
                        {
                            key = title = Target.MaterialPropsList[i].ID;
                        }

                        _propFoldoutList[i] = EditorGUILayout.Foldout(_propFoldoutList[i], title);
                        SessionState.SetBool(PropFoldoutKeyAt(key), _propFoldoutList[i]);
                        if (_propFoldoutList[i])
                        {
                            EditorGUI.indentLevel++;
                            MaterialPropsGUI(_materialPropsProp, i);
                            EditorGUI.indentLevel--;
                        }
                    }

                    EditorGUI.indentLevel--;
                }

                if (change.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void GlobalPropertyGUI(SerializedProperty globalPropsProp)
        {
            var shader = globalPropsProp.FindPropertyRelative("_shader");
            var colors = globalPropsProp.FindPropertyRelative("_colors");
            var floats = globalPropsProp.FindPropertyRelative("_floats");

            EditorGUILayout.LabelField("Global Properties", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(shader);

            // Colors
            _globalColorsFoldout = EditorGUILayout.Foldout(_globalColorsFoldout, "Colors");
            SessionState.SetBool(ColorsFoldoutKeyAt("global"), _colorsFoldoutList[0]);
            if (_globalColorsFoldout)
            {
                EditorGUI.indentLevel++;
                PropsGUI(colors, Target.GlobalProps, true);
                EditorGUI.indentLevel--;
            }

            // Floats
            _globalFloatsFoldout = EditorGUILayout.Foldout(_globalFloatsFoldout, "Floats");
            SessionState.SetBool(FloatsFoldoutKeyAt("global"), _floatsFoldoutList[0]);
            if (_globalFloatsFoldout)
            {
                EditorGUI.indentLevel++;
                PropsGUI(floats, Target.GlobalProps, false);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
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

            var key = string.IsNullOrWhiteSpace(id.stringValue) ? index.ToString() : id.stringValue;
            // Colors
            _colorsFoldoutList[index] = EditorGUILayout.Foldout(_colorsFoldoutList[index], "Colors");
            SessionState.SetBool(ColorsFoldoutKeyAt(key), _colorsFoldoutList[index]);
            if (_colorsFoldoutList[index])
            {
                EditorGUI.indentLevel++;
                PropsGUI(colors, Target.MaterialPropsList[index], true);
                EditorGUI.indentLevel--;
            }

            // Floats
            _floatsFoldoutList[index] = EditorGUILayout.Foldout(_floatsFoldoutList[index], "Floats");
            SessionState.SetBool(FloatsFoldoutKeyAt(key), _floatsFoldoutList[index]);
            if (_floatsFoldoutList[index])
            {
                EditorGUI.indentLevel++;
                PropsGUI(floats, Target.MaterialPropsList[index]);
                EditorGUI.indentLevel--;
            }
        }

        private void PropsGUI(SerializedProperty propsList, MaterialProps matProps, bool isColor = false)
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
                    ShowNewRecorderMenu(matProps, isColor);
                }
            }
        }

        private void ShowNewRecorderMenu(MaterialProps matProps, bool isColor)
        {
            var addPropertyMenu = new GenericMenu();
            var shader = matProps.Shader;
            for (var pi = 0; pi < shader.GetPropertyCount(); pi++)
            {
                var propName = shader.GetPropertyName(pi);
                var propType = shader.GetPropertyType(pi);
                if (isColor)
                {
                    // すでに同じ名前のプロパティがある場合は追加しない
                    if (propType != ShaderPropertyType.Color ||
                        matProps.Colors.Any(c => c.Name == propName))
                        continue;

                    AddPropertyToMenu(propName, addPropertyMenu, matProps, true);
                }
                else if (propType is ShaderPropertyType.Float or ShaderPropertyType.Range)
                {
                    // すでに同じ名前のプロパティがある場合は追加しない
                    if (matProps.Floats.Any(f => f.Name == propName))
                        continue;
                    AddPropertyToMenu(propName, addPropertyMenu, matProps);
                }
            }

            if (addPropertyMenu.GetItemCount() == 0)
            {
                addPropertyMenu.AddDisabledItem(new GUIContent("No Property to Add"));
            }

            addPropertyMenu.ShowAsContext();
        }

        private void AddPropertyToMenu(string propName, GenericMenu menu, MaterialProps props, bool isColor = false)
        {
            menu.AddItem(new GUIContent(propName), false, data => OnAddProperty((string)data, props, isColor),
                propName);
        }

        private void OnAddProperty(string propName, MaterialProps props, bool isColor = false)
        {
            var material = props.Material;
            if (isColor)
            {
                var defaultColor = material == null ? Color.black : material.GetColor(propName);
                var matProp = new MaterialProp<Color>(propName, defaultColor);
                props.Colors.Add(matProp);
            }
            else
            {
                var defaultFloat = material == null ? 0.0f : material.GetFloat(propName);
                var matProp = new MaterialProp<float>(propName, defaultFloat);
                props.Floats.Add(matProp);
            }

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
            serializedObject.Update();
        }
    }
}