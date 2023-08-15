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
        [SerializeField] private MaterialPropertyConfig _config;
        public string ID => _id;

        public BakedMaterialProperty Preset
        {
            get => _preset;
            set => _preset = value;
        }

        public MaterialPropertyConfig Config
        {
            get => _config;
            set => _config = value;
        }

        public PresetIDPair()
        {
        }

        public PresetIDPair(string id, MaterialPropertyConfig config, BakedMaterialProperty preset)
        {
            _id = id;
            _config = config;
            _preset = preset;
        }

        public void GetWarnings(in List<string> warnings)
        {
            // if (string.IsNullOrWhiteSpace(_id))
            //     warnings.Add("Empty ID");
            // if (_preset == null)
            //     warnings.Add("Empty Preset");
            // if (_config == null)
            //     warnings.Add("Empty Config");
            if (Config && Preset && Preset.Config && Config != Preset.Config)
                warnings.Add($"{ID}'s Preset has different config. Should be {Config.name} but {Preset.Config.name}");
        }
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
                // id duplicate check
                if (ids.Contains(pair.ID))
                    warnings.Add($"Duplicate ID: {pair.ID}");
                else if (string.IsNullOrWhiteSpace(pair.ID))
                    emptyIDNum++;
                else
                    ids.Add(pair.ID);

                // pair.GetWarnings(warnings);
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