using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
    }
}