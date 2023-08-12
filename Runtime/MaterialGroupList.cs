using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    public class MaterialGroupList : MonoBehaviour
    {
        [SerializeField] private List<MaterialGroup> _materialGroups = new List<MaterialGroup>();

        private List<MaterialGroup> _materialGroupsInScene = new();
        private bool _isInitialized = false;

        public List<MaterialGroup> MaterialGroups => _materialGroups;
        public List<MaterialGroup> MaterialGroupsInScene => _materialGroupsInScene;


        private void OnValidate()
        {
            if (!_isInitialized)
            {
                FetchBakedPropertiesInScene();
                _materialGroups = new List<MaterialGroup>(MaterialGroupsInScene);
                _isInitialized = true;
            }
        }

        // <MaterialGroupsID, MaterialProps>
        public void SetPropertyBlock(Dictionary<string, MaterialProps> materialPropsDict)
        {
            foreach (var materialGroup in _materialGroups)
            {
                if (materialGroup == null)
                    continue;

                if (materialPropsDict.TryGetValue(materialGroup.ID, out var materialProps))
                {
                    materialGroup.SetPropertyBlock(materialProps);
                }
            }
        }

        public void ResetPropertyBlockToDefault()
        {
            foreach (var materialGroup in _materialGroups)
            {
                if (materialGroup == null)
                    continue;

                materialGroup.ResetDefaultPropertyBlock();
            }
        }

        public void FetchBakedPropertiesInScene()
        {
            var materialGroups = FindObjectsByType<MaterialGroup>(findObjectsInactive: FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            _materialGroupsInScene = new List<MaterialGroup>(materialGroups);
        }

#if UNITY_EDITOR
        [ContextMenu("Create Baked Property Group Asset")]
        public void CreateBakedPropertyGroupAsset()
        {
            var propertyGroup = ScriptableObject.CreateInstance<BakedPropertyGroup>();
            foreach (var mg in MaterialGroups)
            {
                var presetIDPairs = new PresetIDPair(mg.ID, mg.MaterialPropertyConfig, null);
                propertyGroup.PresetIDPairs.Add(presetIDPairs);
            }

            var defaultName = $"New BakedPropertyGroup";
            Utils.CreateAsset(propertyGroup, defaultName, "Create BakedPropertyGroup", "BakedPropertyGroup");
        }
#endif
    }
}