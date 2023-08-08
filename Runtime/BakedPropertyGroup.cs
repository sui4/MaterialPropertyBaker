using System;
using System.Collections.Generic;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [Serializable]
    public class PresetIDPair
    {
        [SerializeField] private string _id;
        [SerializeField] private BakedMaterialProperty _preset;
        public string ID => _id;
        public BakedMaterialProperty Preset => _preset;
    }

    [CreateAssetMenu(menuName = "MaterialPropertyBaker/BakedPropertyGroup", order = 1)]
    public class BakedPropertyGroup : ScriptableObject
    {
        [SerializeField] private List<PresetIDPair> _presetIDPairs = new();
        public List<PresetIDPair> PresetIDPairs => _presetIDPairs;
        public List<string> Warnings => _warnings;
        private readonly List<string> _warnings = new();

        private void GetWarnings(in List<string> warnings)
        {
            var ids = new HashSet<string>();
            var emptyIDNum = 0;
            foreach (var pair in _presetIDPairs)
            {
                if (ids.Contains(pair.ID))
                    warnings.Add($"Duplicate ID: {pair.ID}");
                else if (string.IsNullOrWhiteSpace(pair.ID))
                    emptyIDNum++;
                else
                    ids.Add(pair.ID);
            }

            if (emptyIDNum > 0)
                warnings.Add($"There are {emptyIDNum} Empty ID");
        }

        private void OnValidate()
        {
            _warnings.Clear();
            GetWarnings(_warnings);
        }
    }
}