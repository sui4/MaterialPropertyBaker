using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private readonly List<bool> _intsFoldoutList = new();
        private readonly List<bool> _texturesFoldoutList = new();
        private bool _globalColorsFoldout = true;
        private bool _globalFloatsFoldout = true;
        private bool _globalIntsFoldout = true;
        private bool _globalTexturesFoldout = true;

        private SerializedProperty _globalPropsProp;
        private SerializedProperty _materialPropsListProp;
        private SerializedProperty _materialPropsProp;
        private MpbProfile TargetProfile => (MpbProfile)target;

        private void OnEnable()
        {
            if (target == null) return;
            _materialPropsListProp = serializedObject.FindProperty("_materialPropsList");
            _globalPropsProp = serializedObject.FindProperty("_globalProps");
            Validate();
        }

        private static string PropFoldoutKeyAt(string targetName, string id) => $"{targetName}_propFoldout_{id}";
        private static string ColorsFoldoutKeyAt(string targetName, string id) => $"{targetName}_colorsFoldout_{id}";
        private static string FloatsFoldoutKeyAt(string targetName, string id) => $"{targetName}_floatsFoldout_{id}";
        private static string IntsFoldoutKeyAt(string targetName, string id) => $"{targetName}_intsFoldout_{id}";
        private static string TexturesFoldoutKeyAt(string targetName, string id) => $"{targetName}_texturesFoldout_{id}";

        private void Validate()
        {
            for (int i = _propFoldoutList.Count; i < _materialPropsListProp.arraySize; i++)
            {
                string id = string.IsNullOrWhiteSpace(TargetProfile.MaterialPropsList[i].ID)
                    ? i.ToString()
                    : TargetProfile.MaterialPropsList[i].ID;
                string targetName = TargetProfile.name;
                _propFoldoutList.Add(SessionState.GetBool(PropFoldoutKeyAt(targetName, id), true));
                _colorsFoldoutList.Add(SessionState.GetBool(ColorsFoldoutKeyAt(targetName, id), true));
                _floatsFoldoutList.Add(SessionState.GetBool(FloatsFoldoutKeyAt(targetName, id), true));
                _intsFoldoutList.Add(SessionState.GetBool(IntsFoldoutKeyAt(targetName, id), true));
                _texturesFoldoutList.Add(SessionState.GetBool(TexturesFoldoutKeyAt(targetName, id), true));
            }
        }

        public override void OnInspectorGUI()
        {
            if (target == null) return;
            serializedObject.Update();
            if (_materialPropsListProp.arraySize > _propFoldoutList.Count)
                Validate();

            MPBEditorUtils.WarningGUI(TargetProfile.Warnings);
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                // global settings
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    GlobalPropertyGUI(_globalPropsProp);
                }

                using (new GUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Per ID Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    // per id settings
                    for (var i = 0; i < _materialPropsListProp.arraySize; i++)
                    {
                        _materialPropsProp = _materialPropsListProp.GetArrayElementAtIndex(i);
                        string key, title;
                        if (string.IsNullOrWhiteSpace(TargetProfile.MaterialPropsList[i].ID))
                        {
                            key = i.ToString();
                            title = $"Material Property {i}";
                        }
                        else
                        {
                            key = title = TargetProfile.MaterialPropsList[i].ID;
                        }

                        _propFoldoutList[i] = EditorGUILayout.Foldout(_propFoldoutList[i], title);
                        SessionState.SetBool(PropFoldoutKeyAt(TargetProfile.name, key), _propFoldoutList[i]);
                        if (_propFoldoutList[i])
                        {
                            EditorGUI.indentLevel++;
                            MaterialPropsGUI(TargetProfile ,_materialPropsProp, i, _colorsFoldoutList, _floatsFoldoutList, _intsFoldoutList, _texturesFoldoutList);
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
            SerializedProperty shader = globalPropsProp.FindPropertyRelative("_shader");
            SerializedProperty colors = globalPropsProp.FindPropertyRelative("_colors");
            SerializedProperty floats = globalPropsProp.FindPropertyRelative("_floats");
            SerializedProperty ints = globalPropsProp.FindPropertyRelative("_ints");
            SerializedProperty textures = globalPropsProp.FindPropertyRelative("_textures");

            EditorGUILayout.LabelField("Global Properties", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(shader);

            var targetShader = shader.objectReferenceValue as Shader;
            // Colors
            _globalColorsFoldout = EditorGUILayout.Foldout(_globalColorsFoldout, "Colors");
            SessionState.SetBool(ColorsFoldoutKeyAt(TargetProfile.name, "global"), _globalColorsFoldout);
            if (_globalColorsFoldout)
            {
                EditorGUI.indentLevel++;
                PropsGUI(colors, TargetProfile.GlobalProps, ShaderPropertyType.Color, targetShader);
                EditorGUI.indentLevel--;
            }

            // Floats
            _globalFloatsFoldout = EditorGUILayout.Foldout(_globalFloatsFoldout, "Floats");
            SessionState.SetBool(FloatsFoldoutKeyAt(TargetProfile.name, "global"), _globalFloatsFoldout);
            if (_globalFloatsFoldout)
            {
                EditorGUI.indentLevel++;
                PropsGUI(floats, TargetProfile.GlobalProps, ShaderPropertyType.Float, targetShader);
                EditorGUI.indentLevel--;
            }

            // // Ints
            // _globalIntsFoldout = EditorGUILayout.Foldout(_globalIntsFoldout, "Ints");
            // SessionState.SetBool(IntsFoldoutKeyAt("global"), _globalIntsFoldout);
            // if (_globalIntsFoldout)
            // {
            //     EditorGUI.indentLevel++;
            //     PropsGUI(ints, Target.GlobalProps, ShaderPropertyType.Int);
            //     EditorGUI.indentLevel--;
            // }
            
            // Textures
            _globalTexturesFoldout = EditorGUILayout.Foldout(_globalTexturesFoldout, "Textures");
            SessionState.SetBool(TexturesFoldoutKeyAt(TargetProfile.name, "global"), _globalTexturesFoldout);
            if (_globalTexturesFoldout)
            {
                EditorGUI.indentLevel++;
                PropsGUI(textures, TargetProfile.GlobalProps, ShaderPropertyType.Texture, targetShader);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        private void MaterialPropsGUI(MpbProfile targetProfile, SerializedProperty materialPropsProp, int index, IList<bool> colorsFoldoutList, IList<bool> floatsFoldoutList, IList<bool> intsFoldoutList, IList<bool> texturesFoldoutList)
        {
            SerializedProperty id = materialPropsProp.FindPropertyRelative("_id");
            SerializedProperty material = materialPropsProp.FindPropertyRelative("_material");
            SerializedProperty shader = materialPropsProp.FindPropertyRelative("_shader");
            SerializedProperty colors = materialPropsProp.FindPropertyRelative("_colors");
            SerializedProperty floats = materialPropsProp.FindPropertyRelative("_floats");
            SerializedProperty ints = materialPropsProp.FindPropertyRelative("_ints");
            SerializedProperty textures = materialPropsProp.FindPropertyRelative("_textures");

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
            
            // Textures
            texturesFoldoutList[index] = EditorGUILayout.Foldout(texturesFoldoutList[index], "Textures");
            SessionState.SetBool(TexturesFoldoutKeyAt(targetProfile.name, key), texturesFoldoutList[index]);
            if (texturesFoldoutList[index])
            {
                EditorGUI.indentLevel++;
                PropsGUI(textures, targetProfile.MaterialPropsList[index], ShaderPropertyType.Texture, targetShader);
                EditorGUI.indentLevel--;
            }
        }

        private void PropsGUI(SerializedProperty propsList, MaterialProps matProps, ShaderPropertyType targetPropType,
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
                            break;
                        case ShaderPropertyType.Texture:
                            EditorGUILayout.PropertyField(valueProp, new GUIContent(label));
                            break;
                        default:
                            break;
                    }

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
                    ShowNewRecorderMenu(matProps, targetPropType);
                }
            }
        }


        // for popup menu
        private void ShowNewRecorderMenu(MaterialProps matProps, ShaderPropertyType targetPropType)
        {
            var addPropertyMenu = new GenericMenu();
            Shader shader = matProps.Shader;
            for (var pi = 0; pi < shader.GetPropertyCount(); pi++)
            {
                string propName = shader.GetPropertyName(pi);
                ShaderPropertyType propType = shader.GetPropertyType(pi);
                if (!MPBEditorUtils.IsMatchShaderType(targetPropType, propType)) continue;

                // 隠しプロパティは追加しない
                int si = shader.FindPropertyIndex(propName);
                ShaderPropertyFlags flags = shader.GetPropertyFlags(si);
                if (flags.HasFlag(ShaderPropertyFlags.HideInInspector)) continue;
                switch (propType)
                {
                    // すでに同じ名前のプロパティがある場合は追加しない
                    case ShaderPropertyType.Color when matProps.Colors.All(c => c.Name != propName):
                        AddPropertyToMenu(propName, addPropertyMenu, matProps, propType);
                        break;
                    case ShaderPropertyType.Float or ShaderPropertyType.Range
                        when matProps.Floats.All(f => f.Name != propName):
                        AddPropertyToMenu(propName, addPropertyMenu, matProps, propType);
                        break;
                    case ShaderPropertyType.Int when matProps.Ints.All(f => f.Name != propName):
                        AddPropertyToMenu(propName, addPropertyMenu, matProps, propType);
                        break;
                    
                    case ShaderPropertyType.Texture when matProps.Textures.All(f => f.Name != propName):
                        AddPropertyToMenu(propName, addPropertyMenu, matProps, propType);
                        break;
                }
            }

            if (addPropertyMenu.GetItemCount() == 0)
            {
                addPropertyMenu.AddDisabledItem(new GUIContent("No Property to Add"));
            }

            addPropertyMenu.ShowAsContext();
        }

        private void AddPropertyToMenu(string propName, GenericMenu menu, MaterialProps props,
            ShaderPropertyType propType)
        {
            menu.AddItem(new GUIContent(propName), false, data => OnAddProperty((string)data, props, propType),
                propName);
        }

        private void OnAddProperty(string propName, MaterialProps props, ShaderPropertyType propType)
        {
            Material material = props.Material;
            Shader shader = props.Shader;
            int propIndex = shader.FindPropertyIndex(propName);

            if (propType is ShaderPropertyType.Color)
            {
                Vector4 vCol = shader.GetPropertyDefaultVectorValue(propIndex);
                Color defaultColor = material == null
                    ? new Color(vCol.x, vCol.y, vCol.z, vCol.w)
                    : material.GetColor(propName);
                var matProp = new MaterialProp<Color>(propName, defaultColor);
                props.Colors.Add(matProp);
            }
            else if (propType is ShaderPropertyType.Float or ShaderPropertyType.Range)
            {
                float defaultFloat = material == null
                    ? shader.GetPropertyDefaultFloatValue(propIndex)
                    : material.GetFloat(propName);
                var matProp = new MaterialProp<float>(propName, defaultFloat);
                props.Floats.Add(matProp);
            }
            else if (propType is ShaderPropertyType.Int)
            {
                int defaultInt = material == null
                    ? shader.GetPropertyDefaultIntValue(propIndex)
                    : material.GetInteger(propName);
                var matProp = new MaterialProp<int>(propName, defaultInt);
                props.Ints.Add(matProp);
            }
            else if (propType is ShaderPropertyType.Texture)
            {
                Texture defaultTexture = material == null
                    ? null
                    : material.GetTexture(propName);
                var matProp = new MaterialProp<Texture>(propName, defaultTexture);
                props.Textures.Add(matProp);
            }

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
            serializedObject.Update();
        }
    }
}