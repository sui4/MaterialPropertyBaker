using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
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
        
        private void OnEnable()
        {
            if (Renderers.Count == 0)
                Renderers.Add(null);

            OnValidate();
        }

        public void OnValidate()
        {
            Warnings.Clear();
            if(Renderers.Count == 0) Renderers.Add(null);
            
            SyncRenderer();
            SyncMaterial();
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
            foreach (var ren in Renderers)
            {
                if(renderers.Contains(ren)) continue;
                Renderers.Remove(ren);
                RendererMatTargetInfoWrapperDict.Remove(ren);
            }
            foreach (var ren in renderers)
            {
                if(Renderers.Contains(ren)) continue;
                Renderers.Add(ren);
                RendererMatTargetInfoWrapperDict.TryAdd(ren, new MaterialTargetInfoSDictWrapper());
            }

        }
        
        public void GetRenderersInChild()
        {
            if (_target == null) return;
            Renderers.Clear();
            _target.GetComponentsInChildren<Renderer>(true, Renderers);
        }
        
        // validate shader name: 同じIDを持つmaterialのshaderが同じかどうか
        private void ValidateShader()
        {
            
        }

        public void SetPropertyBlock(Dictionary<MpbProfile, float> profileWeightDict)
        {
            
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