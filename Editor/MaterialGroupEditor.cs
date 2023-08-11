using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(MaterialGroup))]
    public class MaterialGroupEditor : Editor
    {
        // serialized property of MaterialGroup(target)
        private SerializedProperty _renderersProp;
        private SerializedProperty _materialStatusSDictSDictProp;
        private SerializedProperty _defaultProfileProp;
        private SerializedProperty _materialPropertyConfigProp;
        private SerializedProperty _idProp;

        private static class Styles
        {
            public static readonly GUIContent MaterialPropertyConfigLabel = new GUIContent("Material Property Config");

            public static readonly GUIContent
                OverrideDefaultProfileLabel = new GUIContent("Preset to Override Default");

            public static readonly GUIContent MaterialLabel = GUIContent.none;
            public static readonly GUIContent IsTargetLabel = new GUIContent("Apply");
            public static readonly GUIContent IDLabel = new GUIContent("ID");
        }

        private MaterialGroup Target => (MaterialGroup)target;

        private void OnEnable()
        {
            _defaultProfileProp = serializedObject.FindProperty("_overrideDefaultPreset");
            _materialPropertyConfigProp = serializedObject.FindProperty("_materialPropertyConfig");
            _materialStatusSDictSDictProp = serializedObject.FindProperty("_materialStatusDictDict");
            _renderersProp = serializedObject.FindProperty("_renderers");
            _idProp = serializedObject.FindProperty("_id");
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            serializedObject.Update();
            if (Target == null)
                return;

            // default
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(_idProp, Styles.IDLabel);
                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(_materialPropertyConfigProp, Styles.MaterialPropertyConfigLabel);
                    if (GUILayout.Button("New", GUILayout.Width(50)))
                    {
                        CreateConfigAsset();
                    }
                }
                EditorGUILayout.PropertyField(_defaultProfileProp, Styles.OverrideDefaultProfileLabel);

                if (change.changed)
                    serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Separator();

            // renderer list
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Renderers", new GUIStyle("label"));
                EditorGUI.indentLevel++;
                for (int ri = 0; ri < _renderersProp.arraySize; ri++)
                {
                    var rendererProp = _renderersProp.GetArrayElementAtIndex(ri);
                    var (rendererKeysProp, matStatusSDictWrapperValuesProp) =
                        SerializedDictionaryUtil.GetKeyValueListSerializedProperty(_materialStatusSDictSDictProp);
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        RendererGUI(ri, rendererProp, rendererKeysProp, matStatusSDictWrapperValuesProp);
                    }

                    EditorGUILayout.Separator();
                }

                EditorGUI.indentLevel--;

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Validate"))
                    {
                        Target.OnValidate();
                    }

                    // Add Renderer button
                    if (GUILayout.Button("+"))
                    {
                        Target.Renderers.Add(null);
                        EditorUtility.SetDirty(Target);
                        serializedObject.Update();
                    }
                }
            }
            WarningGUI(Target.Warnings);
        }

        // ri = renderer index
        private void RendererGUI(int ri, SerializedProperty rendererProp, SerializedProperty rendererKeysProp,
            SerializedProperty matStatusSDictWrapperListProps)
        {
            var currentRenderer = rendererProp.objectReferenceValue as Renderer;

            using (new GUILayout.HorizontalScope())
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.PropertyField(rendererProp, new GUIContent("Renderer"));
                    if (change.changed)
                    {
                        var newRenderer = rendererProp.objectReferenceValue as Renderer;
                        OnRendererChanging(currentRenderer, newRenderer, ri);
                    }
                }

                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    if (currentRenderer != null)
                    {
                        Target.MaterialStatusDictDict.Remove(currentRenderer);
                    }
                    Target.Renderers.RemoveAt(ri);
                    Target.OnValidate();
                    EditorUtility.SetDirty(Target);
                    serializedObject.Update();
                    return;
                }
            }

            currentRenderer = rendererProp.objectReferenceValue as Renderer;
            if (currentRenderer == null) return;

            EditorGUI.indentLevel++;
            var hasValue =
                Target.MaterialStatusDictDict.TryGetValue(currentRenderer, out var materialStatusDictWrapper);
            if (hasValue)
            {
                var index = Target.MaterialStatusDictWrapperSDict.Keys.IndexOf(currentRenderer);
                var (_, materialStatusSDictWrapperProp) =
                    SerializedDictionaryUtil.GetKeyValueSerializedPropertyAt(index, rendererKeysProp,
                        matStatusSDictWrapperListProps);
                var (matListProp, isTargetListProp) = GetSerializedPropertyFrom(materialStatusSDictWrapperProp);

                // foreachで回すと、要素の変更時にエラーが出るので、forで回す
                // 今回ここでは要素数を変えないため、index out of rangeは起きない
                for (int mi = 0; mi < materialStatusDictWrapper.MaterialStatusDict.Count; mi++)
                {
                    var (materialProp, isTargetProp) =
                        SerializedDictionaryUtil.GetKeyValueSerializedPropertyAt(mi, matListProp, isTargetListProp);

                    MaterialGUI(materialProp, isTargetProp);
                }
            }
            else
            {
                Debug.LogError(
                    "Renderer is not found in MaterialGroup. This should not happen. Data may be corrupted.");
            }

            EditorGUI.indentLevel--;
        }

        private void OnRendererChanging(Renderer currentRenderer, Renderer newRenderer, int ri)
        {
            if (currentRenderer == newRenderer)
            {
                Target.OnValidate();
                return;
            }

            // when currentRenderer != newRenderer
            if (newRenderer == null)
            {
                // currentRenderer != newRendererなので、currentRendererはnullではない
                Target.MaterialStatusDictDict.Remove(currentRenderer);
                Target.Renderers[ri] = newRenderer;
            }
            else if (Target.MaterialStatusDictDict.ContainsKey(newRenderer))
            {
                // すでにMaterialGroupに追加されているRendererは追加しない
                Debug.LogWarning($"this renderer is already added to MaterialGroup {target.name}. so skipped.");
                return;
            }
            else
            {
                if (currentRenderer != null)
                {
                    Target.MaterialStatusDictDict.Remove(currentRenderer);
                }

                var materialStatusDictWrapperToAdd = new MaterialStatusDictWrapper();

                foreach (var mat in newRenderer.sharedMaterials)
                {
                    var isTarget = Target.MaterialPropertyConfig != null &&
                                   Target.MaterialPropertyConfig.ShaderName == mat.shader.name;
                    if (!materialStatusDictWrapperToAdd.MaterialStatusDict.TryAdd(mat, isTarget))
                    {
                        // failed to add
                        Debug.LogWarning($"MaterialGroup: Failed to add material to MaterialStatusDict {target.name}");
                    }
                }

                if (!Target.MaterialStatusDictDict.TryAdd(newRenderer, materialStatusDictWrapperToAdd))
                {
                    Debug.LogWarning($"MaterialGroup: Failed to add {newRenderer.name} to MaterialGroup {target.name}");
                }
                else
                {
                    Debug.Log(
                        $"Added {newRenderer.sharedMaterials.Length} materials to MaterialGroup of {target.name}");
                }

                Target.Renderers[ri] = newRenderer;
            }
            Target.OnValidate();
            EditorUtility.SetDirty(Target);
            serializedObject.Update();
        }

        private void MaterialGUI(SerializedProperty materialProp, SerializedProperty isTarget)
        {
            // Caution: 要素数が変わるとエラーが出るので、要素数を変えないようにする
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.PropertyField(isTarget, Styles.IsTargetLabel);
                    if (change.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(Target);
                    }
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(materialProp, Styles.MaterialLabel);
                }
            }
        }
        private static void WarningGUI(List<string> warnings)
        {
            // helpBox
            if (warnings.Count > 0)
            {
                foreach (var warning in warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }
        }

        private void CreateConfigAsset()
        {
            // get any material from any renderer that is target
            Material mat = GetAnyTargetMaterial();
            if (mat == null)
            {
                Debug.LogWarning("MaterialGroup: No target material found. Please add a material to MaterialGroup.");
                return;
            }
            
            MaterialPropertyExporter.Init(mat, OnExported);
        }

        private void OnExported(MaterialPropertyConfig config)
        {
            Target.MaterialPropertyConfig = config;
            Target.OnValidate();
            EditorUtility.SetDirty(Target);
        }

        private Material GetAnyTargetMaterial()
        {
            foreach (var (ren, materialStatusDictWrapper) in Target.MaterialStatusDictDict)
            {
                foreach (var (material, isTarget) in materialStatusDictWrapper.MaterialStatusDict)
                {
                    if(isTarget)
                    {
                        return material;
                    }
                }
            }

            return null;
        }
        
        // utils
        private static (SerializedProperty keyMaterialListProp, SerializedProperty valueIsTargetListProp)
            GetSerializedPropertyFrom(SerializedProperty materialStatusSDictWrapperProp)
        {
            if (materialStatusSDictWrapperProp == null)
                throw new NullReferenceException("materialStatusSDictWrapperProp is null");
            var materialStatusSDictProp = materialStatusSDictWrapperProp.FindPropertyRelative("_materialStatusDict");
            if (materialStatusSDictProp == null) throw new NullReferenceException("materialStatusSDictProp is null");
            return SerializedDictionaryUtil.GetKeyValueListSerializedProperty(materialStatusSDictProp);
        }
    }
}