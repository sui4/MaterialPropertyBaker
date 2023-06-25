using System.Collections.Generic;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [ExecuteAlways]
    public class MaterialPresetViewer: MonoBehaviour
    {
        private MaterialPropertyBlock _mpb;
        [SerializeField] private MaterialGroups _materialGroups;
        [SerializeField] private List<BakedMaterialProperty> _presets;
        
        public List<BakedMaterialProperty> Presets
        {
            get => _presets;
            set => _presets = value;
        }
        private void OnEnable()
        {
            _mpb = new MaterialPropertyBlock();
            if (_presets == null)
            {
                _presets = new List<BakedMaterialProperty>();
                _presets.Add(null);
            }
        }

        public void ApplyPreset(BakedMaterialProperty preset)
        {
            if (preset == null) return;
            preset.UpdateShaderID();
            Utils.CreatePropertyBlockFromProfile(preset, out _mpb);
            _materialGroups.SetPropertyBlock(_mpb);
            
        }

        public void ResetView()
        {
            _mpb = new MaterialPropertyBlock();
            _materialGroups.SetPropertyBlock(_mpb);
        }
        

    }
}