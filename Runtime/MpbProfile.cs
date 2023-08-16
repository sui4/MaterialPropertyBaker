using System;
using System.Collections.Generic;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CreateAssetMenu(fileName = "MbpProfile", menuName = "MaterialPropertyBaker/MpbProfile", order = 0)]
    public class MpbProfile : ScriptableObject
    {
        [SerializeField] private List<MaterialProps> _materialPropsList = new();
        public List<MaterialProps> MaterialPropsList => _materialPropsList;

        public Dictionary<string, MaterialProps> IdMaterialPropsDict { get; private set; } =
            new();
        private void OnValidate()
        {
            IdMaterialPropsDict.Clear();
            foreach (var matProps in MaterialPropsList)
            {
                if (!IdMaterialPropsDict.TryAdd(matProps.ID, matProps))
                {
                    // failed to add
                    Debug.LogWarning($"Duplicate ID: {matProps.ID}");
                }
            }
        }
    }
}