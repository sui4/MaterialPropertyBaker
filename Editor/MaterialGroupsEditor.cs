using System;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(MaterialGroups))]
    public class MaterialGroupsEditor: Editor
    {
        // serialized property of MaterialGroups(target)
        private SerializedProperty _renderersProp;
        private SerializedProperty _materialStatusSDictSDict;
        private SerializedProperty _defaultProfile;
        private SerializedProperty _materialPropertyConfig;
        
        private static class Styles
        {
            public static readonly GUIContent MaterialPropertyConfigLabel = new GUIContent("Material Property Config");
            public static readonly GUIContent OverrideDefaultProfileLabel = new GUIContent("Preset to Override Default");
            public static readonly GUIContent MaterialLabel = GUIContent.none;
            public static readonly GUIContent IsTargetLabel = new GUIContent("Apply");
        }
        private MaterialGroups Target => (MaterialGroups)target;
        private void OnEnable()
        {
            _defaultProfile = serializedObject.FindProperty("_overrideDefaultPreset");
            _materialPropertyConfig = serializedObject.FindProperty("_materialPropertyConfig");
            _materialStatusSDictSDict = serializedObject.FindProperty("_materialStatusDictDict");
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
                // EditorGUILayout.PropertyField(_materialStatusSDictSDict);
                EditorGUILayout.PropertyField(_materialPropertyConfig, Styles.MaterialPropertyConfigLabel);
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(_defaultProfile, Styles.OverrideDefaultProfileLabel);

                if (change.changed)
                    serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.Separator();
            
            // renderer list
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Renderers", new GUIStyle("label"));
                for(int ri=0; ri < _renderersProp.arraySize; ri++)
                {
                    var rendererProp = _renderersProp.GetArrayElementAtIndex(ri);
                    var (rendererKeysProp, matStatusSDictWrapperValuesProp ) = SerializedDictionaryUtil.GetKeyValueListSerializedProperty(_materialStatusSDictSDict);
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        RendererGUI(ri, rendererProp, rendererKeysProp, matStatusSDictWrapperValuesProp);
                    }
                    EditorGUILayout.Separator();
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
        
        // ri = renderer index
        private void RendererGUI(int ri, SerializedProperty rendererProp, SerializedProperty rendererKeysProp, SerializedProperty matStatusSDictWrapperListProps)
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
                        Target.OnValidate();
                    }
                }
                if(GUILayout.Button("-", GUILayout.Width(25)))
                {
                    if (currentRenderer != null)
                    {
                        Target.MaterialStatusDictDict.Remove(currentRenderer);
                    }
                    Target.Renderers.RemoveAt(ri);
                    EditorUtility.SetDirty(Target);
                    serializedObject.Update();
                    return;
                }
            }

            currentRenderer = rendererProp.objectReferenceValue as Renderer;
            if(currentRenderer == null) return;
            
            EditorGUI.indentLevel++;
            var hasValue = Target.MaterialStatusDictDict.TryGetValue(currentRenderer, out var materialStatusDictWrapper);
            if (hasValue)
            {
                var index = Target.MaterialStatusDictWrapperSDict.Keys.IndexOf(currentRenderer);
                var (_, materialStatusSDictWrapperProp) =
                    SerializedDictionaryUtil.GetKeyValueSerializedPropertyAt(index, rendererKeysProp, matStatusSDictWrapperListProps);
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
                Debug.LogError("Renderer is not found in MaterialGroups. This should not happen. Data may be corrupted.");
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
                // すでにMaterialGroupsに追加されているRendererは追加しない
                Debug.LogWarning($"this renderer is already added to MaterialGroups {target.name}. so skipped.");
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
                    if (!materialStatusDictWrapperToAdd.MaterialStatusDict.TryAdd(mat, true))
                    {
                        // failed to add
                        Debug.LogWarning($"MaterialGroups: Failed to add material to MaterialStatusDict {target.name}");
                    }
                }

                if (!Target.MaterialStatusDictDict.TryAdd(newRenderer, materialStatusDictWrapperToAdd))
                {
                    Debug.LogWarning($"MaterialGroups: Failed to add {newRenderer.name} to MaterialGroups {target.name}");
                }
                else
                {
                    Debug.Log($"Added {newRenderer.sharedMaterials.Length} materials to MaterialGroups of {target.name}");
                }
                Target.Renderers[ri] = newRenderer;
            }
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
        
        // utils
        private static (SerializedProperty keyMaterialListProp, SerializedProperty valueIsTargetListProp) GetSerializedPropertyFrom(SerializedProperty materialStatusSDictWrapperProp)
        {
            if (materialStatusSDictWrapperProp == null)
                throw new NullReferenceException("materialStatusSDictWrapperProp is null");
            var materialStatusSDictProp = materialStatusSDictWrapperProp.FindPropertyRelative("_materialStatusDict");
            if (materialStatusSDictProp == null) throw new NullReferenceException("materialStatusSDictProp is null");
            return SerializedDictionaryUtil.GetKeyValueListSerializedProperty(materialStatusSDictProp);
        }
    }
}