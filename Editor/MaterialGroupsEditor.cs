using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(MaterialGroups))]
    public class MaterialGroupsEditor: Editor
    {
        private SerializedProperty _defaultProfile;
        private SerializedProperty _materialPropertyConfig;
        private SerializedProperty _materialStatusListList;

        private SerializedProperty _materialStatusList;
        private SerializedProperty _materialStatues;
        private SerializedProperty _renderer;
        private void OnEnable()
        {
            _defaultProfile = serializedObject.FindProperty("_overrideOverrideDefaultPreset");
            _materialPropertyConfig = serializedObject.FindProperty("_materialPropertyConfig");
            _materialStatusListList = serializedObject.FindProperty("_materialStatusListList");
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();

            serializedObject.Update();
           
            var mg = (MaterialGroups)target;
            if (mg == null)
                return;

            // default
            using (var change = new EditorGUI.ChangeCheckScope())
            {
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
                for(int i=0; i < _materialStatusListList.arraySize; i++)
                {
                    
                    _materialStatusList = _materialStatusListList.GetArrayElementAtIndex(i);
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        RendererGUI(_materialStatusList, i);
                    }
                    EditorGUILayout.Separator();
                }
                // Add Renderer button
                if (GUILayout.Button("+"))
                {
                    var matStatusList = new MaterialStatusList();
                    mg.MaterialStatusListList.Add(matStatusList);
                    EditorUtility.SetDirty(mg);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
        
        // ri = renderer index
        private void RendererGUI(SerializedProperty listlistProp, int ri)
        {
            var mg = (MaterialGroups)target;
            _materialStatues = listlistProp.FindPropertyRelative("_materialStatuses");
            _renderer = listlistProp.FindPropertyRelative("_renderer");
            
            using (new GUILayout.HorizontalScope())
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.ObjectField(_renderer);
                    if (change.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        if (_renderer.objectReferenceValue != null)
                        {
                            var ren = (Renderer)_renderer.objectReferenceValue;
                            mg.MaterialStatusListList[ri].Renderer = ren;
                            mg.MaterialStatusListList[ri].MaterialStatuses.Clear();
                            for(int mi = 0; mi < ren.sharedMaterials.Length; mi++)
                            {
                                var mat = ren.sharedMaterials[mi];
                                var matStatus = new MaterialStatus();
                                matStatus.Material = mat;
                                matStatus.IsTarget = true;
                                mg.MaterialStatusListList[ri].MaterialStatuses.Add(matStatus);
                            }
                            Debug.Log("Added" + ren.sharedMaterials.Length);
                        }
                        else
                        {
                            mg.MaterialStatusListList[ri].MaterialStatuses.Clear();
                        }
                        serializedObject.Update();

                    }
                }
                if(GUILayout.Button("-", GUILayout.Width(25)))
                {
                    mg.MaterialStatusListList[ri].MaterialStatuses.Clear();
                    mg.MaterialStatusListList[ri].Renderer = null;
                    mg.MaterialStatusListList.RemoveAt(ri);
                    EditorUtility.SetDirty(mg);
                    serializedObject.Update();
                    return;
                }
            }

            var renderer = mg.MaterialStatusListList[ri].Renderer;
            if(renderer == null) return;

            var mats = renderer.sharedMaterials;
            
            EditorGUI.indentLevel++;
            for (int mi = 0; mi < _materialStatues.arraySize; mi++)
            {
                int index = mg.GetIndex(ri, mi);
                var mat = mats[mi];
                SerializedProperty matStatusProp = _materialStatues.GetArrayElementAtIndex(mi);
                
                SerializedProperty matProp = matStatusProp.FindPropertyRelative("_material");
                SerializedProperty isTargetProp = matStatusProp.FindPropertyRelative("_isTarget");
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.LabelField("Apply", GUILayout.Width(60));
                        EditorGUILayout.PropertyField(isTargetProp, label:new GUIContent());
                        if (change.changed)
                        {
                            serializedObject.ApplyModifiedProperties();
                        }
                    }

                    using (new EditorGUI.DisabledScope())
                    {
                        EditorGUILayout.PropertyField(matProp, label:new GUIContent());
                    }
                }
            }


            EditorGUI.indentLevel--;
        }
    }
}