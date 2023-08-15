using System.Collections.Generic;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CreateAssetMenu(fileName = "MbpProfile", menuName = "MaterialPropertyBaker/MpbProfile", order = 0)]
    public class MpbProfile : ScriptableObject
    {
        [SerializeField] private List<MaterialProps> _materialPropsList = new();
        public List<MaterialProps> MaterialPropsList => _materialPropsList;
    }
}