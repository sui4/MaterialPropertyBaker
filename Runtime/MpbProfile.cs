using System.Collections.Generic;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CreateAssetMenu(fileName = "MbpProfile", menuName = "MaterialPropertyBaker/MpbProfile", order = 0)]
    public class MpbProfile : ScriptableObject
    {
        [SerializeField] private MaterialProps _globalProps;
        [SerializeField] private List<MaterialProps> _materialPropsList = new();

        public MaterialProps GlobalProps
        {
            get => _globalProps;
            set => _globalProps = value;
        }

        public List<MaterialProps> MaterialPropsList => _materialPropsList;

        public Dictionary<string, MaterialProps> IdMaterialPropsDict { get; } =
            new();

        public List<string> Warnings { get; } = new();

        private void OnEnable()
        {
            OnValidate();
            GlobalProps.UpdateShaderID();
            foreach (var matProps in MaterialPropsList)
                matProps.UpdateShaderID();
        }

        private void OnValidate()
        {
            IdMaterialPropsDict.Clear();
            Warnings.Clear();
            foreach (var matProps in MaterialPropsList)
                if (!IdMaterialPropsDict.TryAdd(matProps.ID, matProps))
                {
                    var message = $"Duplicate ID: {matProps.ID}";
                    Warnings.Add(message);
                    Debug.LogWarning(message);
                }
        }
    }
}