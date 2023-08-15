using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [AddComponentMenu("MaterialPropertyBaker/MPB TargetGroup")]
    public class TargetGroup : MonoBehaviour
    {
        [SerializeField] private GameObject _target;

        [SerializeField]
        private SerializedDictionary<Renderer, MaterialTargetInfoSDictWrapper> _rendererMatTargetInfoWrapperSDict =
            new();

        [SerializeField] private List<Renderer> _renderers = new();

        private readonly List<string> _warnings = new();
        private MaterialPropertyBlock _mpb; // to reduce GCAlloc

        public Dictionary<Renderer, MaterialTargetInfoSDictWrapper> RendererMatTargetInfoWrapperDict =>
            _rendererMatTargetInfoWrapperSDict.Dictionary;
        public SerializedDictionary<Renderer, MaterialTargetInfoSDictWrapper> RendererMatTargetInfoWrapperSDict =>
            _rendererMatTargetInfoWrapperSDict;

        public List<Renderer> Renderers => _renderers;
        public List<string> Warnings => _warnings;
        
        public Dictionary<Material, MaterialProps> DefaultMaterialPropsDict { get; } = new();

        private void OnEnable()
        {
            OnValidate();
            ResetPropertyBlock();
        }

        public void OnValidate()
        {
            Warnings.Clear();
            
            SyncRenderer();
            SyncMaterial();
            RetrieveInitialProps();
        }

        private void RetrieveInitialProps()
        {
            DefaultMaterialPropsDict.Clear();
            foreach (var ren in Renderers)
            {
                if(ren == null) continue;
                var wrapper = RendererMatTargetInfoWrapperDict[ren];
                foreach (var mat in wrapper.MatTargetInfoDict.Keys)
                {
                    var defaultProps = new MaterialProps(mat);
                    DefaultMaterialPropsDict.TryAdd(mat, defaultProps);
                }
            }
        }

        private void SyncMaterial()
        {
            foreach (var ren in Renderers)
            {
                if(ren == null) continue;
                RendererMatTargetInfoWrapperDict.TryAdd(ren, new MaterialTargetInfoSDictWrapper());

                var matTargetInfoSDictWrapper = RendererMatTargetInfoWrapperDict[ren];
                // 削除されたmaterialを取り除く
                var matKeysToRemove = new List<Material>();
                foreach (var mat in matTargetInfoSDictWrapper.MatTargetInfoDict.Keys)
                {
                    if(!ren.sharedMaterials.Contains(mat))
                        matKeysToRemove.Add(mat);
                }

                foreach (var mat in matKeysToRemove)
                    matTargetInfoSDictWrapper.MatTargetInfoDict.Remove(mat);

                // 追加されたmaterialを追加する
                foreach (var mat in ren.sharedMaterials)
                {
                    if (matTargetInfoSDictWrapper.MatTargetInfoDict.ContainsKey(mat)) continue;
                    var targetInfo = new TargetInfo
                    {
                        ID = mat.name,
                        Material = mat
                    };
                    matTargetInfoSDictWrapper.MatTargetInfoDict.Add(mat, targetInfo);
                }
            }
        }

        private void SyncRenderer()
        {
            if(_target == null) return;
            
            List<Renderer> renderers = new();
            _target.GetComponentsInChildren<Renderer>(true, renderers);
            var renderersToRemove = new List<Renderer>();
            foreach (var ren in Renderers)
            {
                if(renderers.Contains(ren)) continue;
                renderersToRemove.Add(ren);
            }
            foreach (var ren in renderersToRemove)
            {
                Renderers.Remove(ren);
                RendererMatTargetInfoWrapperDict.Remove(ren);
            }
            
            var renderersToAdd = new List<Renderer>();
            foreach (var ren in renderers)
            {
                if (Renderers.Contains(ren)) continue;
                renderersToAdd.Add(ren);
            }

            foreach (var ren in renderersToAdd)
            {
                Renderers.Add(ren);
                RendererMatTargetInfoWrapperDict.TryAdd(ren, new MaterialTargetInfoSDictWrapper());
            }

        }

        // validate shader name: 同じIDを持つmaterialのshaderが同じかどうか
        private void ValidateShader()
        {
            
        }

        public void SetPropertyBlock(Dictionary<MpbProfile, float> profileWeightDict)
        {
            foreach (var ren in Renderers)
            {
                var wrapper = RendererMatTargetInfoWrapperDict[ren];
                for (int mi = 0; mi < ren.sharedMaterials.Length; mi++)
                {
                    var mat = ren.sharedMaterials[mi];
                    var targetInfo = wrapper.MatTargetInfoDict[mat];
                    var defaultProps = DefaultMaterialPropsDict[mat];
                    ren.GetPropertyBlock(_mpb, mi); // 初期化時にsetしてるため、ここで例外は発生しないはず
                    HashSet<int> isFirstTime = new();
                    foreach (var (profile, weight) in profileWeightDict)
                    {
                        if (profile.IdMaterialPropsDict.TryGetValue(targetInfo.ID, out var props))
                        {
                            foreach (var color in props.Colors)
                            {
                                var prop = defaultProps.Colors.Find(c => c.ID == color.ID);
                                if (prop == null) continue;
                                Color current;
                                if (isFirstTime.Contains(prop.ID))
                                {
                                    // second time
                                    current = _mpb.GetColor(prop.ID);
                                }
                                else
                                {
                                    // first time
                                    current = prop.Value;
                                    isFirstTime.Add(prop.ID);
                                }
                                var diff = color.Value - prop.Value;  
                                _mpb.SetColor(prop.ID, current + diff * weight);
                            }

                            foreach (var f in props.Floats)
                            {
                                var prop = defaultProps.Floats.Find(c => c.ID == f.ID);
                                if (prop == null) continue;
                                float current;
                                if (isFirstTime.Contains(prop.ID))
                                {
                                    // second time
                                    current = _mpb.GetFloat(prop.ID);
                                }
                                else
                                {
                                    // first time
                                    current = prop.Value;
                                    isFirstTime.Add(prop.ID);
                                }
                                var diff = f.Value - prop.Value;
                                _mpb.SetFloat(prop.ID, current + diff * weight);
                            }
                        }
                    }
                    ren.SetPropertyBlock(_mpb, mi);
                }
            }
        }

        public void ResetPropertyBlock()
        {
            _mpb = new MaterialPropertyBlock();
            foreach (var ren in Renderers)
            {
                for(var mi = 0; mi < ren.sharedMaterials.Length; mi++)
                {
                    ren.SetPropertyBlock(_mpb, mi);
                }
            }
        }

        public void ResetToDefault()
        {
            ResetPropertyBlock();
        }
        
#if UNITY_EDITOR
        [ContextMenu("Create MPB Profile Asset")]
        public void CreateMpbProfileAsset()
        {
            var asset = ScriptableObject.CreateInstance<MpbProfile>();
            foreach (var ren in Renderers)
            {
                var wrapper = RendererMatTargetInfoWrapperDict[ren];
                for (var mi = 0; mi < ren.sharedMaterials.Length; mi++)
                {
                    var mat = ren.sharedMaterials[mi];
                    var targetInfo = wrapper.MatTargetInfoDict[mat];
                    if(asset.IdMaterialPropsDict.ContainsKey(targetInfo.ID)) continue;
                    var matProps = new MaterialProps(mat, false);
                    matProps.ID = targetInfo.ID;
                    asset.MaterialPropsList.Add(matProps);
                    asset.IdMaterialPropsDict.Add(targetInfo.ID, matProps);
                }
            }

            var defaultName = $"{this.name}_profile";
            Utils.CreateAsset(asset, defaultName, "Create MPB Profile", "");
        }
#endif


    }
    
    [Serializable]
    public class MaterialTargetInfoSDictWrapper
    {
        [SerializeField] private SerializedDictionary<Material, TargetInfo> _matTargetInfoSDict = new();
        public Dictionary<Material, TargetInfo> MatTargetInfoDict => _matTargetInfoSDict.Dictionary;
    }
    
    [Serializable]
    public class TargetInfo
    {
        [SerializeField] private string _id;
        [SerializeField] private Material _material;

        public string ID
        {
            get => _id;
            set => _id = value;
        }

        public Material Material
        {
            get => _material;
            set => _material = value;
        }
    }
}