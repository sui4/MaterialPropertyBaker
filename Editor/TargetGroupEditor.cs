using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(TargetGroup))]
    public class TargetGroupEditor : Editor
    {
        // serialized property of MaterialGroup(target)
        private SerializedProperty _targetProp;
        private SerializedProperty _renderersProp;
        private SerializedProperty _rendererMatTargetInfoWrapperSDictProp;
        // private SerializedProperty _defaultProfileProp;

        private static class Styles
        {
            public static readonly GUIContent
                OverrideDefaultProfileLabel = new GUIContent("Preset to Override Default");

            public static readonly GUIContent MaterialLabel = GUIContent.none;
            public static readonly GUIContent TargetInfoLabel = new GUIContent("Target");
            public static readonly GUIContent IDLabel = new GUIContent("ID");
        }

        private TargetGroup Target => (TargetGroup)target;

        private void OnEnable()
        {
            // _defaultProfileProp = serializedObject.FindProperty("_overrideDefaultPreset");
            _targetProp = serializedObject.FindProperty("_target");
            _rendererMatTargetInfoWrapperSDictProp = serializedObject.FindProperty("_rendererMatTargetInfoWrapperSDict");
            _renderersProp = serializedObject.FindProperty("_renderers");
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
                EditorGUILayout.PropertyField(_targetProp);
                // EditorGUILayout.PropertyField(_defaultProfileProp, Styles.OverrideDefaultProfileLabel);
            
                if (change.changed)
                    serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.Separator();

            CreateBakedPropertyGroupGUI();
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
                        SerializedDictionaryUtil.GetKeyValueListSerializedProperty(_rendererMatTargetInfoWrapperSDictProp);
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

            EditorUtils.WarningGUI(Target.Warnings);
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
                        Target.RendererMatTargetInfoWrapperDict.Remove(currentRenderer);
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
                Target.RendererMatTargetInfoWrapperDict.TryGetValue(currentRenderer, out var materialTargetInfoSDictWrapper);
            if (hasValue)
            {
                var index = Target.RendererMatTargetInfoWrapperSDict.Keys.IndexOf(currentRenderer);
                var (_, materialStatusSDictWrapperProp) =
                    SerializedDictionaryUtil.GetKeyValueSerializedPropertyAt(index, rendererKeysProp,
                        matStatusSDictWrapperListProps);
                var (matListProp, isTargetListProp) = GetSerializedPropertyFrom(materialStatusSDictWrapperProp);

                // foreachで回すと、要素の変更時にエラーが出るので、forで回す
                // 今回ここでは要素数を変えないため、index out of rangeは起きない
                for (int mi = 0; mi < materialTargetInfoSDictWrapper.MatTargetInfoDict.Count; mi++)
                {
                    var (materialProp, targetInfoProp) =
                        SerializedDictionaryUtil.GetKeyValueSerializedPropertyAt(mi, matListProp, isTargetListProp);

                    MaterialGUI(materialProp, targetInfoProp);
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
                Target.RendererMatTargetInfoWrapperDict.Remove(currentRenderer);
                Target.Renderers[ri] = newRenderer;
            }
            else if (Target.RendererMatTargetInfoWrapperDict.ContainsKey(newRenderer))
            {
                // すでにMaterialGroupに追加されているRendererは追加しない
                Debug.LogWarning($"this renderer is already added to MaterialGroup {target.name}. so skipped.");
                return;
            }
            else
            {
                if (currentRenderer != null)
                {
                    Target.RendererMatTargetInfoWrapperDict.Remove(currentRenderer);
                }

                var matTargetInfoSDictWrapperToAdd = new MaterialTargetInfoSDictWrapper();

                foreach (var mat in newRenderer.sharedMaterials)
                {
                    var targetInfo = new TargetInfo()
                    {
                        ID = mat.name,
                        Material = mat
                    };
                    if (!matTargetInfoSDictWrapperToAdd.MatTargetInfoDict.TryAdd(mat, targetInfo))
                    {
                        // failed to add
                        Debug.LogWarning($"MaterialGroup: Failed to add material to MaterialStatusDict {target.name}");
                    }
                }

                if (!Target.RendererMatTargetInfoWrapperDict.TryAdd(newRenderer, matTargetInfoSDictWrapperToAdd))
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

        private void MaterialGUI(SerializedProperty materialProp, SerializedProperty targetInfoProp)
        {
            // Caution: 要素数が変わるとエラーが出るので、要素数を変えないようにする
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.PropertyField(targetInfoProp, new GUIContent(materialProp.objectReferenceValue.name));
                    if (change.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(Target);
                    }
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    // EditorGUILayout.PropertyField(materialProp, Styles.MaterialLabel);
                }
            }
        }

        private void CreateBakedPropertyGroupGUI()
        {
            if (GUILayout.Button("Create MPB Profile"))
            {
                Target.CreateMpbProfileAsset();
            }
        }

        private void OnExported(MaterialPropertyConfig config)
        {
            // Target.MaterialPropertyConfig = config;
            Target.OnValidate();
            EditorUtility.SetDirty(Target);
        }

        // utils
        private static (SerializedProperty keyMaterialListProp, SerializedProperty valueTargetInfoListProp)
            GetSerializedPropertyFrom(SerializedProperty matTargetInfoSDictWrapperProp)
        {
            if (matTargetInfoSDictWrapperProp == null)
                throw new NullReferenceException("matTargetInfoSDictWrapperProp is null");
            var matTargetInfoSDictProp = matTargetInfoSDictWrapperProp.FindPropertyRelative("_matTargetInfoSDict");
            if (matTargetInfoSDictProp == null) throw new NullReferenceException("matTargetInfoSDictProp is null");
            return SerializedDictionaryUtil.GetKeyValueListSerializedProperty(matTargetInfoSDictProp);
        }
    }
}