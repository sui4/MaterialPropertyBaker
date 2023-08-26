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

        private MaterialPropertyBlock _mpb; // to reduce GCAlloc

        public Dictionary<Renderer, MaterialTargetInfoSDictWrapper> RendererMatTargetInfoWrapperDict =>
            _rendererMatTargetInfoWrapperSDict.Dictionary;

        public SerializedDictionary<Renderer, MaterialTargetInfoSDictWrapper> RendererMatTargetInfoWrapperSDict =>
            _rendererMatTargetInfoWrapperSDict;

        public List<Renderer> Renderers => _renderers;
        public List<string> Warnings { get; } = new();

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
                if (ren == null) continue;
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
                if (ren == null) continue;
                RendererMatTargetInfoWrapperDict.TryAdd(ren, new MaterialTargetInfoSDictWrapper());

                var matTargetInfoSDictWrapper = RendererMatTargetInfoWrapperDict[ren];
                // 削除されたmaterialを取り除く
                var matKeysToRemove = new List<Material>();
                foreach (var mat in matTargetInfoSDictWrapper.MatTargetInfoDict.Keys)
                    if (!ren.sharedMaterials.Contains(mat))
                        matKeysToRemove.Add(mat);

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
            if (_target == null) return;

            List<Renderer> renderers = new();
            _target.GetComponentsInChildren(true, renderers);
            var renderersToRemove = new List<Renderer>();
            foreach (var ren in Renderers)
            {
                if (renderers.Contains(ren)) continue;
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
            // merge global profile
            Dictionary<MpbProfile, Dictionary<string, MaterialProps>> mergedPropsDictDict = new();
            foreach (var (profile, _) in profileWeightDict)
            {
                MergeGlobalProps(profile, out var mergedPropsDict);
                mergedPropsDictDict[profile] = mergedPropsDict;
            }
            foreach (var ren in Renderers)
            {
                var wrapper = RendererMatTargetInfoWrapperDict[ren];
                for (var mi = 0; mi < ren.sharedMaterials.Length; mi++)
                {
                    var mat = ren.sharedMaterials[mi];
                    var targetInfo = wrapper.MatTargetInfoDict[mat];
                    var defaultProps = DefaultMaterialPropsDict[mat];
                    // ren.GetPropertyBlock(_mpb, mi); // 初期化時にsetしてるため、ここで例外は発生しないはず
                    _mpb = new MaterialPropertyBlock();
                    HashSet<int> usedProperty = new();
                    foreach (var (profile, weight) in profileWeightDict)
                    {
                        if (mergedPropsDictDict[profile].TryGetValue(targetInfo.ID, out var props))
                        {
                            Debug.Log(props.ID);
                            SetPropertyBlock(props, weight, defaultProps, usedProperty, _mpb);
                        }
                    }

                    ren.SetPropertyBlock(_mpb, mi);
                }
            }
        }
        
        private static void SetPropertyBlock(MaterialProps targetProps, float weight, MaterialProps defaultProps,
            ISet<int> usedProperty, MaterialPropertyBlock mpb)
        {
            foreach (var color in targetProps.Colors)
            {
                var defaultProp = defaultProps.Colors.Find(c => c.ID == color.ID);
                if (defaultProp == null) continue;
                var current = defaultProp.Value;
                if (usedProperty.Add(defaultProp.ID) == false)
                    current = mpb.GetColor(defaultProp.ID); //already set

                var diff = color.Value - defaultProp.Value;
                mpb.SetColor(defaultProp.ID, current + diff * weight);
            }

            foreach (var f in targetProps.Floats)
            {
                var prop = defaultProps.Floats.Find(c => c.ID == f.ID);
                if (prop == null) continue;
                var current = prop.Value;
                if (usedProperty.Add(prop.ID) == false)
                    current = mpb.GetFloat(prop.ID); // already set

                var diff = f.Value - prop.Value;
                mpb.SetFloat(prop.ID, current + diff * weight);
            }
        }
        
        // 個別に設定された値を優先する
        private static void MergeGlobalProps(MpbProfile profile, out Dictionary<string, MaterialProps> mergedPropsDict)
        {
            mergedPropsDict = new Dictionary<string, MaterialProps>();
            foreach (var (id, props) in profile.IdMaterialPropsDict)
            {
                var mergedProps = MergeMaterialProps(new MaterialProps[2]{profile.GlobalProps, props});
                mergedPropsDict[id] = mergedProps;
            }
        }


        // layerが上(indexが大きい)のを優先する
        private static MaterialProps MergeMaterialProps(in IReadOnlyList<MaterialProps> layeredProps)
        {
            MaterialProps mergedProps = new();
            Dictionary<string, MaterialProp<Color>> idColorDict = new();
            Dictionary<int, MaterialProp<float>> idFloatDict = new();
            for (int li = 0; li < layeredProps.Count; li++)
            {
                var target = layeredProps[li];
                foreach (var colorProp in target.Colors)
                {
                    idColorDict[colorProp.Name] = colorProp;
                    if (li == 0)
                    {
                        Debug.Log(colorProp.Name + " " + colorProp.Value);
                    }
                }

                foreach (var floatProp in target.Floats)
                    idFloatDict[floatProp.ID] = floatProp;
            }

            foreach (var (_, colorProp) in idColorDict)
                mergedProps.Colors.Add(colorProp);

            foreach (var (_, floatProp) in idFloatDict)
                mergedProps.Floats.Add(floatProp);
            return mergedProps;
        }

        public void ResetPropertyBlock()
        {
            _mpb = new MaterialPropertyBlock();
            foreach (var ren in Renderers)
                for (var mi = 0; mi < ren.sharedMaterials.Length; mi++)
                    ren.SetPropertyBlock(_mpb, mi);
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
                    if (asset.IdMaterialPropsDict.ContainsKey(targetInfo.ID)) continue;
                    var matProps = new MaterialProps(mat, false);
                    matProps.ID = targetInfo.ID;
                    asset.MaterialPropsList.Add(matProps);
                    asset.IdMaterialPropsDict.Add(targetInfo.ID, matProps);
                }
            }

            var defaultName = $"{name}_profile";
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