using System.Collections.Generic;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [ExecuteAlways]
    public class MaterialPresetViewer: MonoBehaviour
    {
        [SerializeField] private MaterialGroup _materialGroup;
        
        public List<BakedMaterialProperty> Presets
        {
            get => _presets;
            set => _presets = value;
        }
        [SerializeField] private List<BakedMaterialProperty> _presets;

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
            _materialGroup.SetPropertyBlock(preset.MaterialProps);
        }


        public void ResetView()
        {
            _materialGroup.ResetDefaultPropertyBlock();
        }
        
    }
}