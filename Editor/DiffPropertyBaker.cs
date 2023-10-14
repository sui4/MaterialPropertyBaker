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

        private List<bool> _baseFoldoutList = new();

        private readonly Dictionary<string, DiffHolder> _diffHolderDict = new();

        private Vector2 _scrollPos = Vector2.zero; 
        [MenuItem("MaterialPropertyBaker/Diff Property Baker")]
        private static void ShowWindow()
        {
            var window = GetWindow<DiffPropertyBaker>();
            window.titleContent = new GUIContent("Diff Property Baker");
            window.Show();
        }

        private void OnEnable()
        {
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("2つのマテリアルを比較し、異なる値を持つプロパティを保存します。", MessageType.Info);
            
            var prevBase = _baseProfile;
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("Base Profile", EditorStyles.boldLabel);
                _baseProfile = EditorGUILayout.ObjectField(_baseProfile, typeof(MpbProfile), allowSceneObjects:false) as MpbProfile;
                EditorGUI.indentLevel++;
                using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPos))
                {
                    MaterialPropertyGUI(_baseProfile);
                    _scrollPos = scroll.scrollPosition;
                }
                EditorGUI.indentLevel--;
            }
            if(_baseProfile != prevBase)
            {
                Refresh();
            }

            EditorGUILayout.Separator();
            BakeButton();
            EditorGUILayout.Separator();
        }

        private void Refresh()
        {
            _diffHolderDict.Clear();
            Register(_baseProfile);
        }

        private void Register(MpbProfile profile)
        {
            foreach (MaterialProps matProps in profile.MaterialPropsList)
            {
                _diffHolderDict.Add(matProps.ID, new DiffHolder());
            }
        }

        private void MaterialPropertyGUI(MpbProfile profile)
        {
            if (profile == null) return;

            foreach (MaterialProps matProps in profile.MaterialPropsList)
            {
                bool foldout = SessionState.GetBool(profile.name + matProps.ID, true);
                foldout = EditorGUILayout.Foldout(foldout, matProps.ID);
                SessionState.SetBool(profile.name + matProps.ID, foldout);
                if (!foldout) continue;

                using (new EditorGUI.IndentLevelScope())
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(new GUIContent("base material") ,matProps.Material, typeof(Material), allowSceneObjects:false);
                    }

                    if (!_diffHolderDict.TryGetValue(matProps.ID, out DiffHolder diffHolder))
                    {
                        _diffHolderDict.Add(matProps.ID, new DiffHolder());
                        diffHolder = _diffHolderDict[matProps.ID];
                    }
                    Material prevTarget = diffHolder.TargetMaterial;
                    diffHolder.TargetMaterial = EditorGUILayout.ObjectField(new GUIContent("target material"), diffHolder.TargetMaterial, typeof(Material), allowSceneObjects:false) as Material;

                    if (diffHolder.TargetMaterial != prevTarget)
                    {
                        if (diffHolder.TargetMaterial != null)
                        {
                            GetDifferentProperties(matProps.Material, diffHolder.TargetMaterial, out diffHolder.DiffProps);
                        }
                        else
                        {
                            diffHolder.DiffProps.Clear();
                        }
                    }
                    if(diffHolder.TargetMaterial == null) continue;
                    EditorGUILayout.LabelField("Different Properties", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    PropertiesGUI(diffHolder);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private static void PropertiesGUI(DiffHolder diffHolder)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Base Value");
                EditorGUILayout.LabelField("Target Value");
            }
            foreach (BaseTargetValueHolder prop in diffHolder.DiffProps)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    switch (prop.PropType)
                    {
                        case ShaderPropertyType.Color:
                            EditorGUILayout.ColorField(new GUIContent(prop.PropName), prop.BaseColorValue, true, true, true);
                            EditorGUILayout.ColorField(new GUIContent(prop.PropName), prop.TargetColorValue, true, true, true);
                            break;
                        case ShaderPropertyType.Float:
                        case ShaderPropertyType.Range:
                            EditorGUILayout.FloatField(new GUIContent(prop.PropName), prop.BaseFloatValue);
                            EditorGUILayout.FloatField(new GUIContent(prop.PropName), prop.TargetFloatValue);
                            break;
                        case ShaderPropertyType.Int:
                            EditorGUILayout.IntField(new GUIContent(prop.PropName), prop.BaseIntValue);
                            EditorGUILayout.IntField(new GUIContent(prop.PropName), prop.TargetIntValue);
                            break;
                        default:
                            Debug.LogWarning(
                                $"Property type {prop.PropType} is not supported. Skipped. (This should not happen))");
                            break;
                    }
                }
            }
        }

        private void BakeButton()
        {
            GUI.enabled = IsValid();
            Color cache = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Bake Different Properties"))
            {
                BakeDifferentProperties();
            }

            GUI.backgroundColor = cache;
            GUI.enabled = true;
        }

        private bool IsValid()
        {
            return _baseProfile != null;
        }

        private void BakeDifferentProperties()
        {
            if (_baseProfile == null) return;

            foreach ((string id, DiffHolder diffHolder) in _diffHolderDict)
            {
                if (_baseProfile.IdMaterialPropsDict.TryGetValue(id, out MaterialProps targetMatProps))
                {
                    foreach (BaseTargetValueHolder prop in diffHolder.DiffProps)
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

            if (EditorUtility.DisplayDialog(
                    "Bake Succeeded",
                    $"Properties baked to {_baseProfile.name}",
                    "OK"))
            {
                Close();
                Selection.activeObject = _baseProfile;
            }
        }

        private class DiffHolder
        {
            public Material BaseMaterial;
            public Material TargetMaterial;
            public List<BaseTargetValueHolder> DiffProps = new();
        }

        private class BaseTargetValueHolder
        {
            public string PropName;
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
                string propName = baseMat.shader.GetPropertyName(pi);
                ShaderPropertyType propType = baseMat.shader.GetPropertyType(pi);

                var baseTargetValueHolder = new BaseTargetValueHolder()
                {
                    PropName = propName,
                    PropType = propType,
                };
                switch (propType)
                {
                    case ShaderPropertyType.Color:
                        Color baseColor = baseMat.GetColor(propName);
                        Color targetColor = targetMat.GetColor(propName);
                        if (baseColor != targetColor)
                        {
                            baseTargetValueHolder.BaseColorValue = baseColor;
                            baseTargetValueHolder.TargetColorValue = targetColor;
                            differentProps.Add(baseTargetValueHolder);
                        }

                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        float baseFloat = baseMat.GetFloat(propName);
                        float targetFloat = targetMat.GetFloat(propName);
                        if (Math.Abs(baseFloat - targetFloat) > tolerance)
                        {
                            baseTargetValueHolder.BaseFloatValue = baseFloat;
                            baseTargetValueHolder.TargetFloatValue = targetFloat;
                            differentProps.Add(baseTargetValueHolder);
                        }

                        break;
                    case ShaderPropertyType.Int:
                        int baseInt = baseMat.GetInteger(propName);
                        int targetInt = targetMat.GetInteger(propName);
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