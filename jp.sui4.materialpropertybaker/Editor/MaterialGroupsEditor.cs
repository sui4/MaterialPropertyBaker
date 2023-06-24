using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(MaterialGroups))]
    public class MaterialGroupsEditor: Editor
    {
        private SerializedProperty _defaultProfile;
        private SerializedProperty _materialStatusListList;
        private void OnEnable()
        {
            _defaultProfile = serializedObject.FindProperty("defaultProfile");
            _materialStatusListList = serializedObject.FindProperty("_materialStatusListList");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
           
            var mg = (MaterialGroups)target;
            if (mg == null)
                return;

            // default
            using (var change = new EditorGUI.ChangeCheckScope())
            {
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
                    
                    SerializedProperty listlistProp = _materialStatusListList.GetArrayElementAtIndex(i);
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        RendererGUI(listlistProp, i);
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
            SerializedProperty matStatusListProp = listlistProp.FindPropertyRelative("_materialStatusList");
            SerializedProperty listRendererProp = listlistProp.FindPropertyRelative("_renderer");
            
            using (new GUILayout.HorizontalScope())
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.ObjectField(listRendererProp);
                    if (change.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        if (listRendererProp.objectReferenceValue != null)
                        {
                            var ren = (Renderer)listRendererProp.objectReferenceValue;
                            mg.MaterialStatusListList[ri]._renderer = ren;
                            mg.MaterialStatusListList[ri]._materialStatusList.Clear();
                            for(int mi = 0; mi < ren.sharedMaterials.Length; mi++)
                            {
                                var mat = ren.sharedMaterials[mi];
                                var matStatus = new MaterialStatus();
                                matStatus.material = mat;
                                matStatus.isTarget = true;
                                mg.MaterialStatusListList[ri]._materialStatusList.Add(matStatus);
                            }
                            Debug.Log("Added" + ren.sharedMaterials.Length);
                        }
                        else
                        {
                            mg.MaterialStatusListList[ri]._materialStatusList.Clear();
                        }
                        serializedObject.Update();

                    }
                }
                if(GUILayout.Button("-", GUILayout.Width(25)))
                {
                    mg.MaterialStatusListList[ri]._materialStatusList.Clear();
                    mg.MaterialStatusListList[ri]._renderer = null;
                    mg.MaterialStatusListList.RemoveAt(ri);
                    EditorUtility.SetDirty(mg);
                    serializedObject.Update();
                    return;
                }
            }

            var renderer = mg.MaterialStatusListList[ri]._renderer;
            if(renderer == null) return;

            var mats = renderer.sharedMaterials;
            
            EditorGUI.indentLevel++;
            for (int mi = 0; mi < matStatusListProp.arraySize; mi++)
            {
                int index = mg.GetIndex(ri, mi);
                var mat = mats[mi];
                SerializedProperty matStatusProp = matStatusListProp.GetArrayElementAtIndex(mi);
                
                SerializedProperty matProp = matStatusProp.FindPropertyRelative("material");
                SerializedProperty isTargetProp = matStatusProp.FindPropertyRelative("isTarget");
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