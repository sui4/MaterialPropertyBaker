﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;

namespace sui4.MaterialPropertyBaker
{
    public class PropertyModifier : EditorWindow
    {
        private MpbProfile _profile;
        private SerializedObject _serializedObject;
        private readonly List<bool> _propFoldoutList = new();
        private readonly List<bool> _colorsFoldoutList = new();
        private readonly List<bool> _floatsFoldoutList = new();
        private readonly List<bool> _intsFoldoutList = new();
        
        private SerializedProperty _materialPropsListProp;
        private SerializedProperty _materialPropsProp;
        
        [MenuItem("MaterialPropertyBaker/PropertyModifier")]
        private static void ShowWindow()
        {
            var window = GetWindow<PropertyModifier>();
            window.titleContent = new GUIContent("PropertyModifier");
            window.Show();
        }

        private static string PropFoldoutKeyAt(string targetName, string id) => $"{targetName}_propFoldout_{id}";
        private static string ColorsFoldoutKeyAt(string targetName, string id) => $"{targetName}_colorsFoldout_{id}";
        private static string FloatsFoldoutKeyAt(string targetName, string id) => $"{targetName}_floatsFoldout_{id}";
        private static string IntsFoldoutKeyAt(string targetName, string id) => $"{targetName}_intsFoldout_{id}";

        private void OnSwitchProfile()
        {
            if(_profile == null) return;
            _serializedObject = new SerializedObject(_profile);
            _materialPropsListProp = _serializedObject.FindProperty("_materialPropsList");
            UpdateFoldoutList(_profile);
        }

        private void UpdateFoldoutList(MpbProfile profile)
        {
            if(profile == null) return;
            
            for (int i = _propFoldoutList.Count; i < _materialPropsListProp.arraySize; i++)
            {
                string id = string.IsNullOrWhiteSpace(profile.MaterialPropsList[i].ID)
                    ? i.ToString()
                    : profile.MaterialPropsList[i].ID;
                string targetName = profile.name;
                _propFoldoutList.Add(SessionState.GetBool(PropFoldoutKeyAt(targetName, id), true));
                _colorsFoldoutList.Add(SessionState.GetBool(ColorsFoldoutKeyAt(targetName, id), true));
                _floatsFoldoutList.Add(SessionState.GetBool(FloatsFoldoutKeyAt(targetName, id), true));
                _intsFoldoutList.Add(SessionState.GetBool(IntsFoldoutKeyAt(targetName, id), true));
            }
        }
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("MaterialのInspector GUIでプロパティの値を調整できます", MessageType.Info);
            
            MpbProfile prevProfile = _profile;
            _profile = EditorGUILayout.ObjectField("TargetProfile", _profile, typeof(MpbProfile), false) as MpbProfile;
            
            if(_profile != prevProfile)
            {
                OnSwitchProfile();
            }
            if(_profile == null)
            {
                return;
            }

            _serializedObject.Update();
            if (_materialPropsListProp.arraySize > _propFoldoutList.Count)
            {
                UpdateFoldoutList(_profile);
            }

            MPBEditorUtils.WarningGUI(_profile.Warnings);
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                using (new GUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Per Property Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    // per id settings
                    for (var i = 0; i < _materialPropsListProp.arraySize; i++)
                    {
                        _materialPropsProp = _materialPropsListProp.GetArrayElementAtIndex(i);
                        string key, foldoutTitle;
                        if (string.IsNullOrWhiteSpace(_profile.MaterialPropsList[i].ID))
                        {
                            key = i.ToString();
                            foldoutTitle = $"Material Property {i}";
                        }
                        else
                        {
                            key = foldoutTitle = _profile.MaterialPropsList[i].ID;
                        }

                        _propFoldoutList[i] = EditorGUILayout.Foldout(_propFoldoutList[i], foldoutTitle);
                        SessionState.SetBool(PropFoldoutKeyAt(_profile.name, key), _propFoldoutList[i]);
                        if (_propFoldoutList[i])
                        {
                            EditorGUI.indentLevel++;
                            MaterialPropsGUI(_profile ,_materialPropsProp, i, _colorsFoldoutList, _floatsFoldoutList, _intsFoldoutList);
                            EditorGUI.indentLevel--;
                        }
                    }

                    EditorGUI.indentLevel--;
                }

                if (change.changed)
                {
                    _serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void MaterialPropsGUI(MpbProfile targetProfile, SerializedProperty materialPropsProp,
            int index, IList<bool> colorsFoldoutList, IList<bool> floatsFoldoutList, IList<bool> intsFoldoutList)
        {
            SerializedProperty id = materialPropsProp.FindPropertyRelative("_id");
            SerializedProperty material = materialPropsProp.FindPropertyRelative("_material");
            SerializedProperty shader = materialPropsProp.FindPropertyRelative("_shader");
            SerializedProperty colors = materialPropsProp.FindPropertyRelative("_colors");
            SerializedProperty floats = materialPropsProp.FindPropertyRelative("_floats");
            SerializedProperty ints = materialPropsProp.FindPropertyRelative("_ints");

            EditorGUILayout.PropertyField(id, new GUIContent("ID"));
            EditorGUILayout.PropertyField(material);
            using (new EditorGUI.DisabledScope(material.objectReferenceValue != null))
            {
                EditorGUILayout.PropertyField(shader);
            }

            var targetShader = shader.objectReferenceValue as Shader;
            string key = string.IsNullOrWhiteSpace(id.stringValue) ? index.ToString() : id.stringValue;
            // Colors
            colorsFoldoutList[index] = EditorGUILayout.Foldout(colorsFoldoutList[index], "Colors");
            SessionState.SetBool(ColorsFoldoutKeyAt(targetProfile.name, key), colorsFoldoutList[index]);
            if (colorsFoldoutList[index])
            {
                EditorGUI.indentLevel++;
                PropsGUI(colors, targetProfile.MaterialPropsList[index], ShaderPropertyType.Color, targetShader);
                EditorGUI.indentLevel--;
            }

            // Floats
            floatsFoldoutList[index] = EditorGUILayout.Foldout(floatsFoldoutList[index], "Floats");
            SessionState.SetBool(FloatsFoldoutKeyAt(targetProfile.name, key), floatsFoldoutList[index]);
            if (floatsFoldoutList[index])
            {
                EditorGUI.indentLevel++;
                PropsGUI(floats, targetProfile.MaterialPropsList[index], ShaderPropertyType.Float, targetShader);
                EditorGUI.indentLevel--;
            }

            // // Ints
            // intsFoldoutList[index] = EditorGUILayout.Foldout(intsFoldoutList[index], "Ints");
            // SessionState.SetBool(IntsFoldoutKeyAt(key), intsFoldoutList[index]);
            // if (intsFoldoutList[index])
            // {
            //     EditorGUI.indentLevel++;
            //     PropsGUI(ints, Target.MaterialPropsList[index], ShaderPropertyType.Int);
            //     EditorGUI.indentLevel--;
            // }
        }

        private static void PropsGUI(SerializedProperty propsList, MaterialProps matProps, ShaderPropertyType targetPropType,
            Shader targetShader)
        {
            if (propsList.arraySize == 0)
            {
                EditorGUILayout.LabelField("List is Empty");
            }

            for (int i = 0; i < propsList.arraySize; i++)
            {
                SerializedProperty prop = propsList.GetArrayElementAtIndex(i);
                SerializedProperty property = prop.FindPropertyRelative("_name");
                SerializedProperty valueProp = prop.FindPropertyRelative("_value");

                // attribute がある場合は、attribute を適用する
                int si = targetShader.FindPropertyIndex(property.stringValue);
                string[] shaderAttributes = targetShader.GetPropertyAttributes(si);
                MPBEditorUtils.ParseShaderAttribute(shaderAttributes, out List<string> attribs, out List<string> attribValues);
                // 最後の要素が適用されるため、逆順にする
                attribs.Reverse();
                attribValues.Reverse();

                ShaderPropertyType propType = targetShader.GetPropertyType(si);
                string label = Utils.UnderscoresToSpaces(property.stringValue);
                using (new GUILayout.HorizontalScope())
                {
                    switch (propType)
                    {
                        case ShaderPropertyType.Color:
                            ShaderPropertyFlags flags = targetShader.GetPropertyFlags(si);
                            bool isHdr = flags.HasFlag(ShaderPropertyFlags.HDR);
                            valueProp.colorValue =
                                EditorGUILayout.ColorField(new GUIContent(label), valueProp.colorValue, true, true,
                                    isHdr);
                            break;
                        case ShaderPropertyType.Float:
                            var controlCreated = false;
                            for (var ai = 0; ai < attribs.Count(); ai++)
                            {
                                if (attribs[ai] == "Toggle" || attribs[ai] == "MaterialToggle" || attribs[ai] == "ToggleUI")
                                {
                                    bool flag = valueProp.floatValue != 0;
                                    flag = EditorGUILayout.Toggle(new GUIContent(label), flag);
                                    valueProp.floatValue = flag ? 1 : 0;
                                    // キーワードの有効・無効の切り替えが必要？materialを直接いじらないからいらない？
                                    controlCreated = true;
                                }
                                else if (attribs[ai] == "Enum")
                                {
                                    string tmp = Regex.Replace(attribValues[ai], @"\s", "");
                                    string[] enumValues = tmp.Split(',');
                                    if (enumValues.Length % 2 != 0) break; // 名前と値がペアになっていない→無効
                                    
                                    int enumNum = enumValues.Length / 2;
                                    var enumNames = new string[enumNum];
                                    var enumInts = new int[enumNum];
                                    var errorOccured = false;
                                    var selected = 0;
                                    for (var ei = 0; ei < enumNum; ei++)
                                    {
                                        enumNames[ei] = enumValues[ei * 2];
                                        try
                                        {
                                            enumInts[ei] = int.Parse(enumValues[ei * 2 + 1]);
                                        }
                                        catch (Exception _)
                                        {
                                            errorOccured = true;
                                            break;
                                        }
                                        if((int)valueProp.floatValue == enumInts[ei])
                                        {
                                            selected = ei;
                                        }
                                    }
                                    if (errorOccured) continue;
                                    
                                    selected = EditorGUILayout.Popup(label, selected, enumNames);
                                    valueProp.floatValue = enumInts[selected];
                                    controlCreated = true;
                                    break;
                                }
                                else if (attribs[ai] == "KeywordEnum")
                                {
                                    // TODO: 実装
                                    // material を直接いじらないので、不要かも？
                                }
                            }

                            if (!controlCreated)
                            {
                                EditorGUILayout.PropertyField(valueProp, new GUIContent(label));
                            }

                            break;
                        case ShaderPropertyType.Range:
                            float min = ShaderUtil.GetRangeLimits(targetShader, si, 1);
                            float max = ShaderUtil.GetRangeLimits(targetShader, si, 2);
                            var created = false;
                            for (var ai = 0; ai < attribs.Count(); ai++)
                            {
                                if (attribs[ai] == "PowerSlider")
                                {
                                    // var power = 0f;
                                    // try
                                    //     power = float.Parse(attribValues[ai]);
                                    // catch
                                    //     continue;
                                    // CAUTION: PowerSliderはEditorGUILayoutにはなさそう？
                                    valueProp.floatValue =
                                        EditorGUILayout.Slider(label, valueProp.floatValue, min, max);
                                    created = true;
                                }
                                else if (attribs[ai] == "IntRange")
                                {
                                    valueProp.floatValue = EditorGUILayout.IntSlider(label, (int)valueProp.floatValue,
                                        (int)min, (int)max);
                                    created = true;
                                }
                            }

                            if (!created)
                            {
                                valueProp.floatValue = EditorGUILayout.Slider(label, valueProp.floatValue, min, max);
                            }

                            break;
                        case ShaderPropertyType.Int:
                            EditorGUILayout.PropertyField(valueProp, new GUIContent(label));
                            break;
                        case ShaderPropertyType.Vector:
                        case ShaderPropertyType.Texture:
                        default:
                            break;
                    }
                }
            }
        }

    }
}