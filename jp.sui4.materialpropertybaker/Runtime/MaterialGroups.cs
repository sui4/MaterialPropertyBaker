using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace sui4.MaterialPropertyBaker
{

    [Serializable]
    public class MaterialStatus
    {
        [SerializeField] private Material _material = null;
        [SerializeField] private bool _isTarget = false;
        [SerializeField] private BakedMaterialProperty _preset = null;

        public Material Material
        {
            get => _material;
            set => _material = value;
        }
        public bool IsTarget
        {
            get => _isTarget;
            set => _isTarget = value;
        }
        public BakedMaterialProperty Preset
        {
            get => _preset;
            set => _preset = value;
        }
        public MaterialStatus() { }

        public MaterialStatus(Material mat)
        {
            _material = mat;
        }
    }

    [Serializable]
    public class MaterialStatusList
    {
        [SerializeField] private Renderer _renderer = null;
        [SerializeField] private List<MaterialStatus> _materialStatuses = new List<MaterialStatus>();
        
        public Renderer Renderer
        {
            get => _renderer;
            set => _renderer = value;
        }
        
        public List<MaterialStatus> MaterialStatuses
        {
            get => _materialStatuses;
            set => _materialStatuses = value;
        }

        public MaterialStatusList(Renderer ren)
        {
            _renderer = ren;
        }

        public MaterialStatusList() { }
    }
    
    public class MaterialGroups: MonoBehaviour
    {
        [SerializeField] private BakedMaterialProperty _overrideOverrideDefaultPreset;
        [SerializeField] private MaterialPropertyConfig _materialPropertyConfig;
        // レンダラーのインデックス、マテリアルのインデックス、マテリアルの状態
        [SerializeField] private List<MaterialStatusList> _materialStatusListList = new List<MaterialStatusList>();

        public BakedMaterialProperty OverrideDefaultPreset
        {
            get => _overrideOverrideDefaultPreset;
            set => _overrideOverrideDefaultPreset = value;
        }
        public MaterialPropertyConfig MaterialPropertyConfig
        {
            get => _materialPropertyConfig;
            set => _materialPropertyConfig = value;
        }
        public List<MaterialStatusList> MaterialStatusListList => _materialStatusListList;

        private void OnEnable()
        {
            if (_materialStatusListList.Count == 0)
            {
                _materialStatusListList.Add(new MaterialStatusList());
            }
        }

        public int GetIndex(int ri, int mi)
        {
            int index = 0;
            for(int i = 0; i < ri; i++)
            {
                var r = _materialStatusListList[i].Renderer;
                if(r == null) continue;
                
                var matNum = r.sharedMaterials.Length;
                index += matNum;
            }

            index += mi;
            return index;
        }
        
        public void SetPropertyBlock(in MaterialPropertyBlock mpb)
        {
            for (int lli = 0; lli < _materialStatusListList.Count; lli++)
            {
                var list = _materialStatusListList[lli];
                var ren = list.Renderer;
                for (int li = 0; li < list.MaterialStatuses.Count; li++)
                {
                    var matStatus = list.MaterialStatuses[li];
                    if (matStatus.IsTarget)
                    {
                        ren.SetPropertyBlock(mpb, li);
                    }
                }
            }
        }

    }
}
