using System;
using System.Collections.Generic;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{

    [Serializable]
    public class MaterialStatus
    {
        public Material Material
        {
            get => _material;
            set => _material = value;
        }
        [SerializeField] private Material _material = null;

        public bool IsTarget
        {
            get => _isTarget;
            set => _isTarget = value;
        }
        [SerializeField] private bool _isTarget = false;

        public BakedMaterialProperty Preset
        {
            get => _preset;
            set => _preset = value;
        }
        [SerializeField] private BakedMaterialProperty _preset = null;

        public MaterialStatus() { }

        public MaterialStatus(Material mat)
        {
            _material = mat;
        }
    }

    [Serializable]
    public class MaterialStatusList
    {
        
        public Renderer Renderer
        {
            get => _renderer;
            set => _renderer = value;
        }
        [SerializeField] private Renderer _renderer = null;

        public List<MaterialStatus> MaterialStatuses
        {
            get => _materialStatuses;
            set => _materialStatuses = value;
        }
        [SerializeField] private List<MaterialStatus> _materialStatuses = new List<MaterialStatus>();

        public MaterialStatusList(Renderer ren)
        {
            _renderer = ren;
        }

        public MaterialStatusList() { }
    }
    
    public class MaterialGroups: MonoBehaviour
    {
        // レンダラーのインデックス、マテリアルのインデックス、マテリアルの状態
        private MaterialPropertyBlock _mpb;

        public BakedMaterialProperty OverrideDefaultPreset
        {
            get => _overrideOverrideDefaultPreset;
            set => _overrideOverrideDefaultPreset = value;
        }
        [SerializeField] private BakedMaterialProperty _overrideOverrideDefaultPreset;

        public MaterialPropertyConfig MaterialPropertyConfig
        {
            get => _materialPropertyConfig;
            set => _materialPropertyConfig = value;
        }
        [SerializeField] private MaterialPropertyConfig _materialPropertyConfig;

        public List<MaterialStatusList> MaterialStatusListList => _materialStatusListList;
        [SerializeField] private List<MaterialStatusList> _materialStatusListList = new List<MaterialStatusList>();

        private void OnEnable()
        {
            _mpb = new MaterialPropertyBlock();
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
        
        public void SetPropertyBlock(in MaterialProps materialProps)
        {
            _mpb = new MaterialPropertyBlock();
            for (int lli = 0; lli < _materialStatusListList.Count; lli++)
            {
                var list = _materialStatusListList[lli];
                var ren = list.Renderer;
                for (int li = 0; li < list.MaterialStatuses.Count; li++)
                {
                    var matStatus = list.MaterialStatuses[li];
                    if (matStatus.IsTarget)
                    {
                        try
                        {
                            ren.GetPropertyBlock(_mpb, li);
                        }
                        catch
                        {
                            _mpb = new MaterialPropertyBlock();
                        }
                        Utils.UpdatePropertyBlockFromProps(ref _mpb, materialProps);
                        ren.SetPropertyBlock(_mpb, li);
                    }
                }
            }
        }

        public void SetPropertyBlock(in Dictionary<int, Color> cPropMap, in Dictionary<int, float> fPropMap)
        {
            _mpb = new MaterialPropertyBlock();
            for (int lli = 0; lli < _materialStatusListList.Count; lli++)
            {
                var list = _materialStatusListList[lli];
                var ren = list.Renderer;
                for (int li = 0; li < list.MaterialStatuses.Count; li++)
                {
                    var matStatus = list.MaterialStatuses[li];
                    if (matStatus.IsTarget)
                    {
                        try
                        {
                            ren.GetPropertyBlock(_mpb, li);
                        }
                        catch
                        {
                            _mpb = new MaterialPropertyBlock();
                        }
                        Utils.UpdatePropertyBlockFromDict(ref _mpb, cPropMap, fPropMap);
                        ren.SetPropertyBlock(_mpb, li);
                    }
                }
            }
        }

        public void ResetPropertyBlock()
        {
            _mpb = new MaterialPropertyBlock();
            for (int lli = 0; lli < _materialStatusListList.Count; lli++)
            {
                var list = _materialStatusListList[lli];
                var ren = list.Renderer;
                for (int li = 0; li < list.MaterialStatuses.Count; li++)
                {
                    var matStatus = list.MaterialStatuses[li];
                    if (matStatus.IsTarget)
                    {
                        ren.SetPropertyBlock(_mpb, li);
                    }
                }
            }
        }

        public void ResetDefaultPropertyBlock()
        {
            if (_overrideOverrideDefaultPreset)
            {
                SetPropertyBlock(_overrideOverrideDefaultPreset.MaterialProps);
            }
            else
            {
                ResetPropertyBlock();
            }
        }
    }
}
