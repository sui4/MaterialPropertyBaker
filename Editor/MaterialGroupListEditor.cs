using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(MaterialGroupList))]
    public class MaterialGroupListEditor : Editor
    {
        private SerializedProperty _materialGroupsProp;

        private bool _showMaterialGroupsInScene = false;
        private MaterialGroupList Target => (MaterialGroupList)target;

        private void OnEnable()
        {
            Target.FetchBakedPropertiesInScene();
            _materialGroupsProp = serializedObject.FindProperty("_materialGroups");
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            serializedObject.Update();
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                MaterialGroupListGUI();
                if (change.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.Separator();
            
            CreateBakedPropertyGroupGUI();
            
            EditorGUILayout.Separator();

            using (new GUILayout.VerticalScope("box"))
            {
                MaterialGroupsInSceneGUI();
            }
        }

        private void MaterialGroupsInSceneGUI()
        {
            EditorGUI.indentLevel++;
            _showMaterialGroupsInScene = EditorGUILayout.Foldout(_showMaterialGroupsInScene,
                "All Material Groups in Scene (For Reference)");
            if (!_showMaterialGroupsInScene) return;

            EditorGUILayout.Separator();
            foreach (var materialGroup in Target.MaterialGroupsInScene)
            {
                EditorGUILayout.ObjectField(new GUIContent(materialGroup.ID), materialGroup, typeof(MaterialGroup),
                    true);
            }

            EditorGUILayout.Separator();

            if (GUILayout.Button("Fetch Material Groups in Scene"))
                Target.FetchBakedPropertiesInScene();
            EditorGUI.indentLevel--;
        }

        private void MaterialGroupListGUI()
        {
            EditorGUILayout.LabelField("Material Group List", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            if (_materialGroupsProp.arraySize == 0)
            {
                EditorGUILayout.LabelField("List is Empty");
            }

            for (int i = 0; i < _materialGroupsProp.arraySize; i++)
            {
                var materialGroupProp = _materialGroupsProp.GetArrayElementAtIndex(i);

                using (new EditorGUILayout.VerticalScope())
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        if (materialGroupProp.objectReferenceValue != null)
                        {
                            var mg = materialGroupProp.objectReferenceValue as MaterialGroup;
                            EditorGUILayout.PropertyField(materialGroupProp, new GUIContent(mg ? mg.ID : ""));
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(materialGroupProp);
                        }

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            _materialGroupsProp.DeleteArrayElementAtIndex(i);
                            serializedObject.ApplyModifiedProperties();
                            return;
                        }
                    }
                }
            }

            AddMaterialGroupGUI();

            EditorGUI.indentLevel--;
        }

        private void CreateBakedPropertyGroupGUI()
        {
            if (GUILayout.Button("Create Baked Property Group"))
            {
                Target.CreateBakedPropertyGroupAsset();
            }
        }

        private void AddMaterialGroupGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.Separator();
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    ShowNewRecorderMenu();
                }
            }
        }

        private void ShowNewRecorderMenu()
        {
            var addMaterialGroupMenu = new GenericMenu();
            foreach (var mg in Target.MaterialGroupsInScene)
            {
                if (Target.MaterialGroups.Contains(mg)) continue;
                AddRecorderInfoToMenu(mg, addMaterialGroupMenu);
            }

            if (addMaterialGroupMenu.GetItemCount() == 0)
            {
                addMaterialGroupMenu.AddDisabledItem(new GUIContent("No Material Group to Add"));
            }

            addMaterialGroupMenu.ShowAsContext();
        }
        
        private void AddRecorderInfoToMenu(MaterialGroup mg, GenericMenu menu)
        {
            menu.AddItem(new GUIContent(mg.ID), false, data => OnAddMaterialGroup((MaterialGroup)data), mg);
        }

        private void OnAddMaterialGroup(MaterialGroup materialGroup)
        {
            Target.MaterialGroups.Add(materialGroup);
        }
    }
}