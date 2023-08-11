using System;
using System.Collections.Generic;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [Serializable]
    public class MaterialStatusDictWrapper
    {
        [SerializeField] private SerializedDictionary<Material, bool> _materialStatusDict = new();
        public Dictionary<Material, bool> MaterialStatusDict => _materialStatusDict.Dictionary;
    }

    public class MaterialGroup : MonoBehaviour
    {
        [SerializeField] private BakedMaterialProperty _overrideDefaultPreset;
        [SerializeField] private MaterialPropertyConfig _materialPropertyConfig;

        [SerializeField]
        private SerializedDictionary<Renderer, MaterialStatusDictWrapper> _materialStatusDictDict = new();

        [SerializeField] private List<Renderer> _renderers = new();
        [SerializeField] private string _id;

        // レンダラーのインデックス、マテリアルのインデックス、マテリアルの状態
        private MaterialPropertyBlock _mpb;
        private readonly List<string> _warnings = new();

        public BakedMaterialProperty OverrideDefaultPreset
        {
            get => _overrideDefaultPreset;
            set => _overrideDefaultPreset = value;
        }

        public MaterialPropertyConfig MaterialPropertyConfig
        {
            get => _materialPropertyConfig;
            set => _materialPropertyConfig = value;
        }

        public Dictionary<Renderer, MaterialStatusDictWrapper> MaterialStatusDictDict =>
            _materialStatusDictDict.Dictionary;

        public SerializedDictionary<Renderer, MaterialStatusDictWrapper> MaterialStatusDictWrapperSDict =>
            _materialStatusDictDict;

        public List<Renderer> Renderers => _renderers;
        public List<string> Warnings => _warnings;

        public string ID
        {
            get => _id;
            set => _id = value;
        }

        private void OnEnable()
        {
            _mpb = new MaterialPropertyBlock();
            if (Renderers.Count == 0)
                Renderers.Add(null);
        }

        public void OnValidate()
        {
            _warnings.Clear();

            if (string.IsNullOrWhiteSpace(ID))
            {
                ID = "Group_" + Guid.NewGuid().ToString();
            }

            if (Renderers.Count == 0)
                Renderers.Add(null);

            if (MaterialPropertyConfig == null)
            {
                Warnings.Add("MaterialPropertyConfig should be set");
            }
            SyncMaterial();
            ValidateShaderName();
        }

        private void SyncMaterial()
        {
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
                        if (!Array.Exists(renderer.sharedMaterials, m => m == material))
                            materialKeysToRemove.Add(material);
                    foreach (var mat in materialKeysToRemove) materialStatusDictWrapper.MaterialStatusDict.Remove(mat);
                    // materialが増えてれば追加する. shaderがconfigと一致していればtargetにする
                    foreach (var material in renderer.sharedMaterials)
                    {
                        var isTarget = MaterialPropertyConfig != null &&
                                       material.shader.name == MaterialPropertyConfig.ShaderName;

                        materialStatusDictWrapper.MaterialStatusDict.TryAdd(material, isTarget);
                    }
                }
            }
        }

        private void ValidateShaderName()
        {
            var hasConfig = MaterialPropertyConfig != null;

            var shaders = new HashSet<string>();
            foreach (var (renderer, materialStatusDictWrapper) in MaterialStatusDictDict)
            {
                foreach (var (material, isTarget) in materialStatusDictWrapper.MaterialStatusDict)
                {
                    if (!isTarget) continue;
                    shaders.Add(material.shader.name);
                    if (hasConfig && material.shader.name != MaterialPropertyConfig.ShaderName)
                    {
                        _warnings.Add(
                            $"Material({material.name}) of Renderer({renderer.name}) use different shader from config({MaterialPropertyConfig.ShaderName})");
                    }
                }
            }

            if (!hasConfig && shaders.Count > 1)
                _warnings.Add($"MaterialGroup({ID}) has multiple shaders({string.Join(", ", shaders)})");
        }

        // shaderがconfigと違う場合はisTargetをfalseにする
        [ContextMenu("Disable UnMatch Material")]
        private void UnTargetUnMatchMaterial()
        {
            if (MaterialPropertyConfig == null) return;

            var shaderName = MaterialPropertyConfig.ShaderName;

            foreach (var (_, materialStatusDictWrapper) in MaterialStatusDictDict)
            {
                List<Material> materialsToDisable = new();
                foreach (var (material, isTarget) in materialStatusDictWrapper.MaterialStatusDict)
                {
                    if (isTarget && material.shader.name != shaderName)
                    {
                        materialsToDisable.Add(material);
                    }
                }

                foreach (var mat in materialsToDisable)
                {
                    materialStatusDictWrapper.MaterialStatusDict[mat] = false;
                }
            }

            OnValidate();
        }

        public void SetPropertyBlock(in MaterialProps materialProps)
        {
            _mpb = new MaterialPropertyBlock();

            foreach (var (renderer, materialStatusDictWrapper) in MaterialStatusDictDict)
                // PropertyBlockにはindexを用いてアクセスするので、for文で回す
                for (var mi = 0; mi < renderer.sharedMaterials.Length; mi++)
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

        public void SetPropertyBlock(in Dictionary<int, Color> cPropMap, in Dictionary<int, float> fPropMap)
        {
            _mpb = new MaterialPropertyBlock();
            foreach (var (renderer, materialStatusDictWrapper) in MaterialStatusDictDict)
                // PropertyBlockにはindexを用いてアクセスするので、for文で回す
                for (var mi = 0; mi < renderer.sharedMaterials.Length; mi++)
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

        public void ResetPropertyBlock()
        {
            _mpb = new MaterialPropertyBlock();
            foreach (var (renderer, materialStatusDictWrapper) in MaterialStatusDictDict)
                // PropertyBlockにはindexを用いてアクセスするので、for文で回す
                for (var mi = 0; mi < renderer.sharedMaterials.Length; mi++)
                {
                    var material = renderer.sharedMaterials[mi];
                    var hasValue = materialStatusDictWrapper.MaterialStatusDict.TryGetValue(material, out var isTarget);
                    if (hasValue && isTarget)
                        // 空のproperty blockをセットするとデフォルトの値に戻る
                        renderer.SetPropertyBlock(_mpb, mi);
                }
        }

        public void ResetDefaultPropertyBlock()
        {
            if (_overrideDefaultPreset)
                SetPropertyBlock(_overrideDefaultPreset.MaterialProps);
            else
                ResetPropertyBlock();
        }
    }
}