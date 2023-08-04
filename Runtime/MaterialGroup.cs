using System;
using System.Collections.Generic;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [Serializable]
    public class MaterialStatusDictWrapper
    {
        public Dictionary<Material, bool> MaterialStatusDict => _materialStatusDict.Dictionary;
        [SerializeField] private SerializedDictionary<Material, bool> _materialStatusDict = new SerializedDictionary<Material, bool>();
    }
    
    public class MaterialGroup: MonoBehaviour
    {
        // レンダラーのインデックス、マテリアルのインデックス、マテリアルの状態
        private MaterialPropertyBlock _mpb;

        public BakedMaterialProperty OverrideDefaultPreset
        {
            get => _overrideDefaultPreset;
            set => _overrideDefaultPreset = value;
        }
        [SerializeField] private BakedMaterialProperty _overrideDefaultPreset;

        public MaterialPropertyConfig MaterialPropertyConfig
        {
            get => _materialPropertyConfig;
            set => _materialPropertyConfig = value;
        }
        [SerializeField] private MaterialPropertyConfig _materialPropertyConfig;
        
        public Dictionary<Renderer, MaterialStatusDictWrapper> MaterialStatusDictDict => _materialStatusDictDict.Dictionary;
        public SerializedDictionary<Renderer, MaterialStatusDictWrapper> MaterialStatusDictWrapperSDict => _materialStatusDictDict;
        [SerializeField] private SerializedDictionary<Renderer, MaterialStatusDictWrapper> _materialStatusDictDict = new SerializedDictionary<Renderer, MaterialStatusDictWrapper>();
        
        public List<Renderer> Renderers => _renderers;
        [SerializeField] private List<Renderer> _renderers = new List<Renderer>();
        private void OnEnable()
        {
            _mpb = new MaterialPropertyBlock();
            if(Renderers.Count == 0)
                Renderers.Add(null);
        }

        public void OnValidate()
        {
            if(Renderers.Count == 0)
                Renderers.Add(null);
            foreach (var renderer in Renderers)
            {
                if (renderer == null)
                    continue;
                MaterialStatusDictDict.TryAdd(renderer, new MaterialStatusDictWrapper());
                
                var materialStatusDictWrapper = MaterialStatusDictDict[renderer];
                if (materialStatusDictWrapper.MaterialStatusDict.Count != renderer.sharedMaterials.Length)
                {
                    // materialが減ってれば削除する
                    var materialKeysToRemove = new List<Material>();
                    foreach (var material in materialStatusDictWrapper.MaterialStatusDict.Keys)
                    {
                        if (!Array.Exists(renderer.sharedMaterials, m => m == material))
                        {
                            materialKeysToRemove.Add(material);
                        }
                    }
                    foreach (var mat in materialKeysToRemove)
                    {
                        materialStatusDictWrapper.MaterialStatusDict.Remove(mat);
                    }
                    // materialが増えてれば追加する
                    foreach (var material in renderer.sharedMaterials)
                    {
                        materialStatusDictWrapper.MaterialStatusDict.TryAdd(material, true);
                    }
                }
            }
        }

        public void SetPropertyBlock(in MaterialProps materialProps)
        {
            _mpb = new MaterialPropertyBlock();
            
            foreach (var (renderer, materialStatusDictWrapper) in MaterialStatusDictDict)
            {
                // PropertyBlockにはindexを用いてアクセスするので、for文で回す
                for (int mi = 0; mi < renderer.sharedMaterials.Length; mi++)
                {
                    var material = renderer.sharedMaterials[mi];
                    var hasValue = materialStatusDictWrapper.MaterialStatusDict.TryGetValue(material, out var isTarget);
                    if (hasValue && isTarget)
                    {
                        try
                        {
                            renderer.GetPropertyBlock(_mpb, mi);
                        }
                        catch
                        {
                            _mpb = new MaterialPropertyBlock();
                        }
                        // property blockに値をセットし、rendererにproperty blockをセットする
                        Utils.UpdatePropertyBlockFromProps(ref _mpb, materialProps);
                        renderer.SetPropertyBlock(_mpb, mi);
                    }
                }
            }
        }

        public void SetPropertyBlock(in Dictionary<int, Color> cPropMap, in Dictionary<int, float> fPropMap)
        {
            _mpb = new MaterialPropertyBlock();
            foreach (var (renderer, materialStatusDictWrapper) in MaterialStatusDictDict)
            {
                // PropertyBlockにはindexを用いてアクセスするので、for文で回す
                for (int mi = 0; mi < renderer.sharedMaterials.Length; mi++)
                {
                    var material = renderer.sharedMaterials[mi];
                    var hasValue = materialStatusDictWrapper.MaterialStatusDict.TryGetValue(material, out var isTarget);
                    if (hasValue && isTarget)
                    {
                        try
                        {
                            renderer.GetPropertyBlock(_mpb, mi);
                        }
                        catch
                        {
                            _mpb = new MaterialPropertyBlock();
                        }
                        // property blockに値をセットし、rendererにproperty blockをセットする
                        Utils.UpdatePropertyBlockFromDict(ref _mpb, cPropMap, fPropMap);
                        renderer.SetPropertyBlock(_mpb, mi);
                    }
                }
            }
        }

        public void ResetPropertyBlock()
        {
            _mpb = new MaterialPropertyBlock();
            foreach (var (renderer, materialStatusDictWrapper) in MaterialStatusDictDict)
            {
                // PropertyBlockにはindexを用いてアクセスするので、for文で回す
                for (int mi = 0; mi < renderer.sharedMaterials.Length; mi++)
                {
                    var material = renderer.sharedMaterials[mi];
                    var hasValue = materialStatusDictWrapper.MaterialStatusDict.TryGetValue(material, out var isTarget);
                    if (hasValue && isTarget)
                    {
                        // 空のproperty blockをセットするとデフォルトの値に戻る
                        renderer.SetPropertyBlock(_mpb, mi);
                    }
                }
            }
        }

        public void ResetDefaultPropertyBlock()
        {
            if (_overrideDefaultPreset)
            {
                SetPropertyBlock(_overrideDefaultPreset.MaterialProps);
            }
            else
            {
                ResetPropertyBlock();
            }
        }


    }
}
