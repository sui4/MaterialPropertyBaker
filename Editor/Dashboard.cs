using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    public class Dashboard : EditorWindow
    {
        private const string WindowTitle = "Dashboard";

        private MaterialGroupList _materialGroupList;
        private List<MaterialGroup> _materialGroupsInScene = new();

        [MenuItem("MaterialPropertyBaker/Dashboard")]
        private static void ShowWindow()
        {
            var window = GetWindow<Dashboard>();
            window.titleContent = new GUIContent(WindowTitle);
            window.Show();
        }

        private void OnGUI()
        {
            MaterialGroupListGUI();
        }

        private void OnEnable()
        {
            _materialGroupsInScene = FindObjectsByType<MaterialGroup>(findObjectsInactive: FindObjectsInactive.Include,
                FindObjectsSortMode.None).ToList();
        }

        #region GUI

        private void MaterialGroupListGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Material Group List", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Material Group Count: {_materialGroupsInScene.Count}");
                EditorGUILayout.Separator();
                foreach (var materialGroup in _materialGroupsInScene)
                {
                    EditorGUILayout.ObjectField(materialGroup.ID, materialGroup, typeof(MaterialGroup), true);
                    if (materialGroup.Warnings.Count > 0)
                        EditorGUILayout.HelpBox(string.Join("\n", materialGroup.Warnings), MessageType.Warning);
                }
            }
        }

        #endregion // GUI
    }
}