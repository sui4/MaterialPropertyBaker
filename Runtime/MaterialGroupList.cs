using System;
using System.Collections.Generic;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [Serializable]
    public class MaterialGroups
    {
        [SerializeField] private string _id;
        [SerializeField] private List<MaterialGroup> _materialGroupList = new List<MaterialGroup>();

        public string ID
        {
            get => _id;
            set => _id = value;
        }
        public List<MaterialGroup> MaterialGroupList
        {
            get => _materialGroupList;
            set => _materialGroupList = value;
        }
    }
    public class MaterialGroupList : MonoBehaviour
    {
        [SerializeField]
        private List<MaterialGroups> _materialGroupsList = new List<MaterialGroups>();
        
        private List<MaterialGroup> _materialGroupsInScene = new();
        [SerializeField, HideInInspector] private bool _onCreate = true;

        public List<MaterialGroups> MaterialGroupsList => _materialGroupsList;
        public List<MaterialGroup> MaterialGroupsInScene => _materialGroupsInScene;

        private void OnValidate()
        {
            if (_onCreate)
            {
                OnCreate();
            }
        }

        private void OnCreate()
        {
            FetchBakedPropertiesInScene();
            var materialGroups = new MaterialGroups
            {
                ID = "Default"
            };
            materialGroups.MaterialGroupList.AddRange(MaterialGroupsInScene);
            _materialGroupsList.Add(materialGroups);
            _onCreate = false;
        }

        // <MaterialGroupsID, MaterialProps>
        public void SetPropertyBlock(Dictionary<string, MaterialProps> materialPropsDict)
        {
            foreach (var materialGroups in _materialGroupsList)
            {
                var materialGroupList = materialGroups.MaterialGroupList;
                if (materialGroupList == null)
                    continue;

                if (materialPropsDict.TryGetValue(materialGroups.ID, out var materialProps))
                {
                    foreach (var materialGroup in materialGroupList)
                    {
                        materialGroup.SetPropertyBlock(materialProps);
                    }
                }
            }
        }

        public void ResetPropertyBlockToDefault()
        {
            foreach (var materialGroups in _materialGroupsList)
            {
                var materialGroupList = materialGroups.MaterialGroupList;
                if (materialGroupList == null)
                    continue;

                foreach (var materialGroup in materialGroupList)
                {
                    materialGroup.ResetDefaultPropertyBlock();
                }
            }
        }
        
        public void FetchBakedPropertiesInScene()
        {
            var materialGroups = FindObjectsByType<MaterialGroup>(findObjectsInactive: FindObjectsInactive.Include, FindObjectsSortMode.None);
            _materialGroupsInScene = new List<MaterialGroup>(materialGroups);
        }

        private void GetWarnings()
        {
            var materialGroupSet = new HashSet<MaterialGroup>();
            foreach (var materialGroups in _materialGroupsList)
            {
                foreach (var mg in materialGroups.MaterialGroupList)
                {
                    if (materialGroupSet.Contains(mg))
                    {
                        
                    }
                    else if (mg == null)
                    {
                        
                    }
                    else
                    {
                        materialGroupSet.Add(mg);
                    }

                }
            }
        }
        
        
    }
}