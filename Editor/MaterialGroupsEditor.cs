using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(MaterialGroups))]
    public class MaterialGroupsEditor: Editor
    {
        // serialized property of MaterialGroups(target)
        private SerializedProperty _renderers;
        private SerializedProperty _materialStatusSDictSDict;
        private SerializedProperty _defaultProfile;
        private SerializedProperty _materialPropertyConfig;

        private MaterialGroups Target => (MaterialGroups)target;
        private void OnEnable()
        {
            _defaultProfile = serializedObject.FindProperty("_overrideOverrideDefaultPreset");
            _materialPropertyConfig = serializedObject.FindProperty("_materialPropertyConfig");
            _materialStatusSDictSDict = serializedObject.FindProperty("_materialStatusDictDict");
            _renderers = serializedObject.FindProperty("_renderers");
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
                EditorGUILayout.PropertyField(_materialStatusSDictSDict);
                EditorGUILayout.PropertyField(_materialPropertyConfig);
                EditorGUILayout.Separator();
                
                EditorGUILayout.PropertyField(_defaultProfile);

                if (change.changed)
                    serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            
            // renderer list
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Renderers", new GUIStyle("label"));
                for(int i=0; i < _renderers.arraySize; i++)
                {
                    var rendererProp = _renderers.GetArrayElementAtIndex(i);
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        RendererGUI(rendererProp, i);
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
        private void RendererGUI(SerializedProperty rendererProp, int ri)
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
                        if (Target.Renderers.Contains(newRenderer))
                        {
                            Debug.LogWarning($"this renderer is already added to MaterialGroups {target.name}. so skipped.");
                        }
                        else
                        {
                            if (currentRenderer != null)
                            {
                                Target.MaterialStatusDictDict.Remove(currentRenderer);
                            }
                        
                            if (newRenderer != null)
                            {
                                var materialStatusDictWrapperToAdd = new MaterialStatusDictWrapper();

                                foreach (var mat in newRenderer.sharedMaterials)
                                {
                                    if (!materialStatusDictWrapperToAdd.MaterialStatusDict.TryAdd(mat, true))
                                    {
                                        // failed to add
                                        Debug.LogWarning($"MaterialGroups: Failed to add material to MaterialStatusDict {target.name}");
                                    }
                                }
                                Target.MaterialStatusDictDict.TryAdd(newRenderer, materialStatusDictWrapperToAdd);
                                Debug.Log($"Added {newRenderer.sharedMaterials.Length} materials to MaterialGroups of {target.name}");
                            }
                        }
                        Target.Renderers[ri] = newRenderer;
                        EditorUtility.SetDirty(Target);
                        serializedObject.Update();

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
                // foreachで回すと、要素の変更時にエラーが出るので、forで回す
                // 今回ここでは要素数を変えないため、index out of rangeは起きない
                for (int mi = 0; mi < materialStatusDictWrapper.MaterialStatusDict.Count; mi++)
                {
                    var kvp = materialStatusDictWrapper.MaterialStatusDict.ElementAt(mi);
                    MaterialGUI(kvp.Key, kvp.Value, ref materialStatusDictWrapper);
                }
            }
            EditorGUI.indentLevel--;
        }

        private void MaterialGUI(Material material, bool isTarget, ref MaterialStatusDictWrapper materialStatusDictWrapper)
        {
            // Caution: 要素数が変わるとエラーが出るので、要素数を変えないようにする
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var newValue = EditorGUILayout.ToggleLeft("Apply", isTarget);
                    if (change.changed)
                    {
                        materialStatusDictWrapper.MaterialStatusDict[material] = newValue;
                        serializedObject.Update();
                        EditorUtility.SetDirty(Target);
                    }
                }

                EditorGUILayout.ObjectField(material, typeof(Material), allowSceneObjects:false);
            }
        }
    }
}