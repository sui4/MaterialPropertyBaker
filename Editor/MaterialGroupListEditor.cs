using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(MaterialGroupList))]
    public class MaterialGroupListEditor : Editor
    {
        private SerializedProperty _materialGroupsListProp;

        private bool _showMaterialGroupsInScene = false;
        private MaterialGroupList Target => (MaterialGroupList)target;

        private void OnEnable()
        {
            Target.FetchBakedPropertiesInScene();
            _materialGroupsListProp = serializedObject.FindProperty("_materialGroupsList");
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                MaterialGroupsListGUI();
                if (change.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

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
                EditorGUILayout.ObjectField(materialGroup, typeof(MaterialGroup), true);
            }

            EditorGUILayout.Separator();

            if (GUILayout.Button("Fetch Material Groups in Scene"))
                Target.FetchBakedPropertiesInScene();
            EditorGUI.indentLevel--;
        }

        private void MaterialGroupsListGUI()
        {
            EditorGUILayout.LabelField("Material Group List", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            for (int i = 0; i < _materialGroupsListProp.arraySize; i++)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    var materialGroupsProp = _materialGroupsListProp.GetArrayElementAtIndex(i);
                    //EditorGUILayout.Foldout(true, materialGroups.ID);

                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(Target.MaterialGroupsList[i].ID);
                        if (RemoveMaterialGroupsGUI(i, _materialGroupsListProp))
                        {
                            return;
                        }
                    }
                    EditorGUI.indentLevel++;
                    var idProp = materialGroupsProp.FindPropertyRelative("_id");
                    EditorGUILayout.PropertyField(idProp, new GUIContent("ID"));

                    MaterialGroupListGUI(Target.MaterialGroupsList[i], materialGroupsProp);
                    EditorGUI.indentLevel--;
                }
            }

            AddMaterialGroupsGUI(_materialGroupsListProp);
            EditorGUI.indentLevel--;
        }

        private static bool RemoveMaterialGroupsGUI(int index, SerializedProperty materialGroupsListProp)
        {
            var ret = false;
            var tmp = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                materialGroupsListProp.DeleteArrayElementAtIndex(index);
                ret = true;
            }
            GUI.backgroundColor = tmp;
            return ret;
        }

        private void AddMaterialGroupsGUI(SerializedProperty materialGroupsListProp)
        {
            var tmp = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("+"))
            {
                materialGroupsListProp.InsertArrayElementAtIndex(_materialGroupsListProp.arraySize);
                serializedObject.ApplyModifiedProperties();
                Target.MaterialGroupsList[^1].MaterialGroupList.Clear();
                Target.MaterialGroupsList[^1].ID = "Default";
            }
            GUI.backgroundColor = tmp;
        }

        private void MaterialGroupListGUI(MaterialGroups materialGroups, SerializedProperty mgsProp)
        {
            EditorGUILayout.LabelField("Material Group List", EditorStyles.boldLabel);
            var mgListProp = mgsProp.FindPropertyRelative("_materialGroupList");
            EditorGUI.indentLevel++;

            if (mgListProp.arraySize == 0)
            {
                EditorGUILayout.LabelField("List is Empty");
            }

            for (int i = 0; i < mgListProp.arraySize; i++)
            {
                using (new GUILayout.HorizontalScope())
                {
                    var materialGroupProp = mgListProp.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(materialGroupProp, GUIContent.none);
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                        mgListProp.DeleteArrayElementAtIndex(i);
                }
            }

            AddMaterialGroupGUI(materialGroups);
            EditorGUI.indentLevel--;
        }

        private void AddMaterialGroupGUI(MaterialGroups mgs)
        {
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.Separator();
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    ShowNewRecorderMenu(mgs);
                }
            }
        }

        private void AddRecorderInfoToMenu(MaterialGroup mg, MaterialGroups mgs, GenericMenu menu)
        {
            menu.AddItem(new GUIContent(mg.name), false, data => OnAddMaterialGroup((MaterialGroup)data, mgs), mg);
        }

        private void ShowNewRecorderMenu(MaterialGroups mgs)
        {
            var addMaterialGroupMenu = new GenericMenu();
            foreach (var mg in Target.MaterialGroupsInScene)
            {
                if (mgs.MaterialGroupList.Contains(mg)) continue;
                AddRecorderInfoToMenu(mg, mgs, addMaterialGroupMenu);
            }

            if (addMaterialGroupMenu.GetItemCount() == 0)
            {
                addMaterialGroupMenu.AddDisabledItem(new GUIContent("No Material Group to Add"));
            }

            addMaterialGroupMenu.ShowAsContext();
        }

        private void OnAddMaterialGroup(MaterialGroup materialGroup, in MaterialGroups mgs)
        {
            mgs.MaterialGroupList.Add(materialGroup);
            foreach (var materialGroups in Target.MaterialGroupsList)
            {
                if (materialGroups == mgs) continue;
                materialGroups.MaterialGroupList.Remove(materialGroup);
            }
        }
    }
}