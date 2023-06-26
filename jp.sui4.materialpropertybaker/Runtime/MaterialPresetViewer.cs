using System.Collections.Generic;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [ExecuteAlways]
    public class MaterialPresetViewer: MonoBehaviour
    {
        [SerializeField] private MaterialGroups _materialGroups;
        [SerializeField] private List<BakedMaterialProperty> _presets;
        
        public List<BakedMaterialProperty> Presets
        {
            get => _presets;
            set => _presets = value;
        }
        private void OnEnable()
        {
            if (_presets == null)
            {
                _presets = new List<BakedMaterialProperty> { null };
            }
        }

        public void ApplyPreset(BakedMaterialProperty preset)
        {
            if (preset == null) return;
            preset.UpdateShaderID();
            _materialGroups.SetPropertyBlock(preset.MaterialProps);
        }


        public void ResetView()
        {
            _materialGroups.ResetDefaultPropertyBlock();
        }
        
    }
}