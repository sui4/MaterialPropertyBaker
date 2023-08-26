using System;
using System.Collections.Generic;
using PlasticGui.Configuration.CloudEdition.Welcome;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace sui4.MaterialPropertyBaker
{
    public class DiffPropertyBaker : EditorWindow
    {
        private MpbProfile _baseProfile;
        private MpbProfile _targetProfile;

        private List<bool> _baseFoldoutList = new();
        private List<bool> _targetFoldoutList = new();

        private readonly Dictionary<string, List<BaseTargetValueHolder>> _diffPropsDict = new();

        [MenuItem("MaterialPropertyBaker/Diff Property Baker")]
        private static void ShowWindow()
        {
            var window = GetWindow<DiffPropertyBaker>();
            window.titleContent = new GUIContent("Diff Property Baker");
            window.Show();
        }

        private void OnEnable()
        {
            Validate();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Validate"))
            {
                Validate();
            }

            EditorGUILayout.Separator();
            BakeButton();
            EditorGUILayout.Separator();

            using (new GUILayout.HorizontalScope())
            {
                var prevBase = _baseProfile;
                var prevTarget = _targetProfile;
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("Base Profile", EditorStyles.boldLabel);
                    _baseProfile = EditorGUILayout.ObjectField(_baseProfile, typeof(MpbProfile), false) as MpbProfile;
                    EditorGUI.indentLevel++;
                    MaterialPropertyGUI(_baseProfile, true);
                    EditorGUI.indentLevel--;
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("Target Profile", EditorStyles.boldLabel);
                    _targetProfile =
                        EditorGUILayout.ObjectField(_targetProfile, typeof(MpbProfile), false) as MpbProfile;
                    EditorGUI.indentLevel++;
                    MaterialPropertyGUI(_targetProfile, false);
                    EditorGUI.indentLevel--;
                }

                if (prevBase != _baseProfile || prevTarget != _targetProfile)
                {
                    Validate();
                }
            }
        }

        private void MaterialPropertyGUI(MpbProfile profile, bool isBase)
        {
            if (profile == null) return;

            foreach (var matProps in profile.MaterialPropsList)
            {
                var foldout = SessionState.GetBool(matProps.ID, false);
                foldout = EditorGUILayout.Foldout(foldout, matProps.ID);
                SessionState.SetBool(matProps.ID, foldout);
                if (!foldout) continue;

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.ObjectField(matProps.Material, typeof(Material), false);
                    if (!_diffPropsDict.TryGetValue(matProps.ID, out var diffProps)) continue;

                    foreach (var prop in diffProps)
                    {
                        switch (prop.PropType)
                        {
                            case ShaderPropertyType.Color:
                                var colorValue = isBase ? prop.BaseColorValue : prop.TargetColorValue;
                                EditorGUILayout.ColorField(new GUIContent(prop.PropName), colorValue, true, true, true);
                                break;
                            case ShaderPropertyType.Float:
                            case ShaderPropertyType.Range:
                                var floatValue = isBase ? prop.BaseFloatValue : prop.TargetFloatValue;
                                EditorGUILayout.FloatField(new GUIContent(prop.PropName), floatValue);
                                break;
                            case ShaderPropertyType.Int:
                                var intValue = isBase ? prop.BaseIntValue : prop.TargetIntValue;
                                EditorGUILayout.IntField(new GUIContent(prop.PropName), intValue);
                                break;
                            default:
                                Debug.LogWarning(
                                    $"Property type {prop.PropType} is not supported. Skipped. (This should not happen))");
                                break;
                        }
                    }
                }
            }
        }

        private void BakeButton()
        {
            GUI.enabled = IsValid();
            var tmp = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Bake Different Properties"))
            {
                BakeDifferentProperties();
                Validate();
            }

            GUI.backgroundColor = tmp;
            GUI.enabled = true;
        }

        private bool IsValid()
        {
            return _baseProfile != null && _targetProfile != null;
        }

        private void Validate()
        {
            _diffPropsDict.Clear();
            if (_baseProfile == null || _targetProfile == null) return;

            foreach (var (id, baseMatProps) in _baseProfile.IdMaterialPropsDict)
            {
                if (_targetProfile.IdMaterialPropsDict.TryGetValue(id, out var targetMatProps))
                {
                    GetDifferentProperties(baseMatProps.Material, targetMatProps.Material, out var diffProps);
                    _diffPropsDict.Add(id, diffProps);
                }
            }
        }


        private void BakeDifferentProperties()
        {
            if (_baseProfile == null || _targetProfile == null) return;
            Validate();

            foreach (var (id, diffProps) in _diffPropsDict)
            {
                if (_targetProfile.IdMaterialPropsDict.TryGetValue(id, out var targetMatProps))
                {
                    foreach (var prop in diffProps)
                    {
                        switch (prop.PropType)
                        {
                            case ShaderPropertyType.Color:
                                targetMatProps.SetColor(prop.PropName, prop.TargetColorValue);
                                break;
                            case ShaderPropertyType.Float:
                            case ShaderPropertyType.Range:
                                targetMatProps.SetFloat(prop.PropName, prop.TargetFloatValue);
                                break;
                            case ShaderPropertyType.Int:
                                targetMatProps.SetInt(prop.PropName, prop.TargetIntValue);
                                break;
                            default:
                                Debug.LogWarning(
                                    $"Property type {prop.PropType} is not supported. Skipped. (This should not happen))");
                                break;
                        }
                    }
                }
            }

            if (EditorUtility.DisplayDialog("Bake Succeeded",
                    $"Properties baked to {_targetProfile.name}",
                    "OK"))
            {
                Close();
                Selection.activeObject = _targetProfile;
            }
        }

        private class BaseTargetValueHolder
        {
            public string PropName;
            public Material Material;
            public ShaderPropertyType PropType;
            public Color BaseColorValue;
            public float BaseFloatValue;
            public int BaseIntValue;
            public Color TargetColorValue;
            public float TargetFloatValue;
            public int TargetIntValue;
        }

        private static void GetDifferentProperties(Material baseMat, Material targetMat,
            out List<BaseTargetValueHolder> differentProps)
        {
            differentProps = new List<BaseTargetValueHolder>();
            const float tolerance = 0.0001f;
            if (baseMat.shader != targetMat.shader)
            {
                Debug.LogWarning($"{baseMat.name} and {targetMat.name} have different shaders.");
                return;
            }

            for (var pi = 0; pi < baseMat.shader.GetPropertyCount(); pi++)
            {
                var propName = baseMat.shader.GetPropertyName(pi);
                var propType = baseMat.shader.GetPropertyType(pi);

                var baseTargetValueHolder = new BaseTargetValueHolder()
                {
                    PropName = propName,
                    Material = baseMat,
                    PropType = propType,
                };
                switch (propType)
                {
                    case ShaderPropertyType.Color:
                        var baseColor = baseMat.GetColor(propName);
                        var targetColor = targetMat.GetColor(propName);
                        if (baseColor != targetColor)
                        {
                            baseTargetValueHolder.BaseColorValue = baseColor;
                            baseTargetValueHolder.TargetColorValue = targetColor;
                            differentProps.Add(baseTargetValueHolder);
                        }

                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        var baseFloat = baseMat.GetFloat(propName);
                        var targetFloat = targetMat.GetFloat(propName);
                        if (Math.Abs(baseFloat - targetFloat) > tolerance)
                        {
                            baseTargetValueHolder.BaseFloatValue = baseFloat;
                            baseTargetValueHolder.TargetFloatValue = targetFloat;
                            differentProps.Add(baseTargetValueHolder);
                        }

                        break;
                    case ShaderPropertyType.Int:
                        var baseInt = baseMat.GetInteger(propName);
                        var targetInt = targetMat.GetInteger(propName);
                        if (baseInt != targetInt)
                        {
                            baseTargetValueHolder.BaseIntValue = baseInt;
                            baseTargetValueHolder.TargetIntValue = targetInt;
                            differentProps.Add(baseTargetValueHolder);
                        }

                        break;
                    case ShaderPropertyType.Texture:
                    case ShaderPropertyType.Vector:
                    default:
                        // not supported
                        break;
                }
            }
        }
    }
}