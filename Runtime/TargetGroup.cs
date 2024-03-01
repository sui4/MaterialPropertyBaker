using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

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
        private static Dictionary<int, float> _usedPropWeightDict;

        public Dictionary<Renderer, MaterialTargetInfoSDictWrapper> RendererMatTargetInfoWrapperDict =>
            _rendererMatTargetInfoWrapperSDict.Dictionary;

        public SerializedDictionary<Renderer, MaterialTargetInfoSDictWrapper> RendererMatTargetInfoWrapperSDict =>
            _rendererMatTargetInfoWrapperSDict;

        public List<Renderer> Renderers => _renderers;
        public List<string> Warnings { get; } = new();

        public Dictionary<Material, MaterialProps> DefaultMaterialPropsDict { get; } = new();

        public static bool _verboseMode = false; // output debug log if true
        
#if UNITY_EDITOR
        private const string MenuPath = "MaterialPropertyBaker/MPB TargetGroup/Output Verbose Log"; 
        [MenuItem(MenuPath)]
        private static void ToggleVerboseMode()
        {
            _verboseMode = Menu.GetChecked(MenuPath);
            _verboseMode = !_verboseMode;
            Menu.SetChecked(MenuPath, _verboseMode);
        }
#endif
        
        private void OnEnable()
        {
            OnValidate();
            ResetPropertyBlock();
        }

        public void OnValidate()
        {
            Warnings.Clear();
            if (_target == null)
            {
                _target = this.gameObject;
            }

            SyncRenderer();
            SyncMaterial();
            RetrieveInitialProps();
        }

        private void RetrieveInitialProps()
        {
            DefaultMaterialPropsDict.Clear();
            foreach (Renderer ren in Renderers)
            {
                if (ren == null) continue;
                MaterialTargetInfoSDictWrapper wrapper = RendererMatTargetInfoWrapperDict[ren];
                foreach (Material mat in wrapper.MatTargetInfoDict.Keys)
                {
                    Assert.IsNotNull(mat);
                    var defaultProps = new MaterialProps(mat);
                    DefaultMaterialPropsDict.TryAdd(mat, defaultProps);
                }
            }
        }

        private void SyncMaterial()
        {
            foreach (Renderer ren in Renderers)
            {
                if (ren == null) continue;
                RendererMatTargetInfoWrapperDict.TryAdd(ren, new MaterialTargetInfoSDictWrapper());

                MaterialTargetInfoSDictWrapper matTargetInfoSDictWrapper = RendererMatTargetInfoWrapperDict[ren];
                // 削除されたmaterialを取り除く
                var matKeysToRemove = new List<Material>();
                foreach (Material mat in matTargetInfoSDictWrapper.MatTargetInfoDict.Keys)
                {
                    if (!ren.sharedMaterials.Contains(mat))
                    {
                        matKeysToRemove.Add(mat);
                    }
                }

                foreach (Material mat in matKeysToRemove)
                {
                    matTargetInfoSDictWrapper.MatTargetInfoDict.Remove(mat);
                }

                // 追加されたmaterialを追加する
                foreach (Material mat in ren.sharedMaterials)
                {
                    if (mat == null)
                    {
                        if(_verboseMode)
                            Debug.LogWarning($"MPB TargetGroup: {ren.name} has null material.", ren);
                        continue;
                    }
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
            // mesh renderer, skinned mesh renderer以外を取り除く
            renderers = renderers.Where(ren =>
                ren is MeshRenderer or SkinnedMeshRenderer).ToList();
            var renderersToRemove = new List<Renderer>();
            foreach (Renderer ren in Renderers)
            {
                if (!renderers.Contains(ren))
                {
                    renderersToRemove.Add(ren);
                }
            }

            foreach (Renderer ren in renderersToRemove)
            {
                Renderers.Remove(ren);
                RendererMatTargetInfoWrapperDict.Remove(ren);
            }

            foreach (Renderer ren in renderers)
            {
                if (!Renderers.Contains(ren))
                {
                    Renderers.Add(ren);
                    RendererMatTargetInfoWrapperDict.TryAdd(ren, new MaterialTargetInfoSDictWrapper());
                }
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
            foreach ((MpbProfile profile, float _) in profileWeightDict)
            {
                MergeGlobalProps(profile, out Dictionary<string, MaterialProps> mergedPropsDict);
                mergedPropsDictDict[profile] = mergedPropsDict;
            }

            // TODO: ここはもっと計算量を減らせるはず。targetごとに一度計算すれば良いので、事前計算して結果をまとめておく
            // ただ、メモリの使用量が増えるので、どちらが良いかは検討が必要
            foreach (Renderer ren in Renderers)
            {
                if(ren == null || !ren.enabled) continue;
                MaterialTargetInfoSDictWrapper wrapper = RendererMatTargetInfoWrapperDict[ren];
                for (var mi = 0; mi < ren.sharedMaterials.Length; mi++)
                {
                    Material mat = ren.sharedMaterials[mi];
                    if (mat == null) continue;
                    TargetInfo targetInfo = wrapper.MatTargetInfoDict[mat];
                    MaterialProps defaultProps = DefaultMaterialPropsDict[mat];
                    _mpb = new MaterialPropertyBlock();
                    // profileごとに扱うpropertyは異なるため、どのプロパティがどのweightで使われたかを保存する
                    Dictionary<int, float> usedPropertyWeightDict = new();
                    foreach ((MpbProfile profile, float weight) in profileWeightDict)
                    {
                        // 同じtargetに対するpropertyの値をマージする
                        if (mergedPropsDictDict[profile].TryGetValue(targetInfo.ID, out MaterialProps props))
                        {
                            AccumulatePropertyAndUpdatePropertyBlock(props, weight, defaultProps, usedPropertyWeightDict, _mpb);
                        }
                    }

                    ren.SetPropertyBlock(_mpb, mi);
                }
            }
        }

        // targetPropsの値にweightをかけあわせた値を既存の値に足し合わせ、それを新たな値としてmpbにセットする
        // ※materialから取得したdefault propertyに存在しないpropertyは無視する
        private static void AccumulatePropertyAndUpdatePropertyBlock(MaterialProps propsToAdd, float weight, MaterialProps defaultProps,
            Dictionary<int, float> usedPropWeightDict, MaterialPropertyBlock mpb)
        {
            foreach (MaterialProp<Color> color in propsToAdd.Colors)
            {
                MaterialProp<Color> defaultProp = defaultProps.Colors.Find(c => c.ID == color.ID);
                if (defaultProp == null) continue;
                // 一度目の場合はdefaultの値を、2回目以降は重み付き和がmpbにセットされてるのでそれを使う
                Color current = usedPropWeightDict.TryAdd(defaultProp.ID, weight) ? defaultProp.Value : mpb.GetColor(defaultProp.ID);
                Color diff = color.Value - defaultProp.Value;
                mpb.SetColor(defaultProp.ID, current + diff * weight);
            }

            foreach (MaterialProp<float> f in propsToAdd.Floats)
            {
                MaterialProp<float> prop = defaultProps.Floats.Find(c => c.ID == f.ID);
                if (prop == null) continue;
                // colorと同様
                float current = usedPropWeightDict.TryAdd(prop.ID, weight) ? prop.Value : mpb.GetFloat(prop.ID);
                float diff = f.Value - prop.Value;
                mpb.SetFloat(prop.ID, current + diff * weight);
            }

            foreach (MaterialProp<int> i in propsToAdd.Ints)
            {
                MaterialProp<int> prop = defaultProps.Ints.Find(c => c.ID == i.ID);
                if (prop == null) continue;
                // int型は重み付き和が求められないため、weightが大きい方を優先する
                // NOTE: 重み付き和をもとめたあとに近い値に丸めるという方法もありそう。ただ、どのタイミングで丸めるかが問題になりそう
                if (usedPropWeightDict.TryGetValue(prop.ID, out float storedWeight))
                {
                    if (weight > storedWeight)
                    {
                        mpb.SetInt(prop.ID, i.Value);
                        usedPropWeightDict[prop.ID] = weight; 
                    }
                }
                else
                {
                    mpb.SetInt(prop.ID, i.Value);
                    usedPropWeightDict.Add(prop.ID, weight);
                }
            }
            
            foreach (MaterialProp<Texture> t in propsToAdd.Textures)
            {
                MaterialProp<Texture> prop = defaultProps.Textures.Find(c => c.ID == t.ID);
                if (prop == null) continue;
                if (usedPropWeightDict.TryGetValue(prop.ID, out float storedWeight))
                {
                    if (weight > storedWeight)
                    {
                        if (t.Value != null)
                        {
                            mpb.SetTexture(prop.ID, t.Value);
                        }
                        usedPropWeightDict[prop.ID] = weight; 
                    }
                }
                else
                {
                    if (t.Value != null)
                    {
                        mpb.SetTexture(prop.ID, t.Value);
                    }
                    usedPropWeightDict.Add(prop.ID, weight);
                }
            }
        }

        // globalと個別の両方で同じPropertyの値が設定されていた場合、個別に設定された値を優先する
        private static void MergeGlobalProps(MpbProfile profile, out Dictionary<string, MaterialProps> mergedPropsDict)
        {
            mergedPropsDict = new Dictionary<string, MaterialProps>();
            foreach ((string id, MaterialProps props) in profile.IdMaterialPropsDict)
            {
                MaterialProps mergedProps = MergeMaterialProps(new MaterialProps[2] { profile.GlobalProps, props });
                mergedPropsDict[id] = mergedProps;
            }
        }


        // mergeするのはpropertyの項目のみ。各propertyの値はmergeしない
        // layerが上(indexが大きい)のを優先する
        private static MaterialProps MergeMaterialProps(in IReadOnlyList<MaterialProps> layeredProps)
        {
            MaterialProps mergedProps = new();
            HashSet<int> addedPropIds = new();

            // 最初のレイヤーは特別扱い
            if (layeredProps.Count > 0)
            {
                MaterialProps firstLayer = layeredProps[^1];
                mergedProps = new MaterialProps(firstLayer.Colors, firstLayer.Floats, firstLayer.Ints, firstLayer.Textures);
                addedPropIds.UnionWith(firstLayer.Colors?.Select(colorProp => colorProp.ID) ?? Enumerable.Empty<int>());
                addedPropIds.UnionWith(firstLayer.Floats?.Select(floatProp => floatProp.ID) ?? Enumerable.Empty<int>());
                addedPropIds.UnionWith(firstLayer.Ints?.Select(intProp => intProp.ID) ?? Enumerable.Empty<int>());
                addedPropIds.UnionWith(firstLayer.Textures?.Select(textureProp => textureProp.ID) ?? Enumerable.Empty<int>());
            }

            // 残りのレイヤー
            for (int li = layeredProps.Count - 2; li >= 0; li--)
            {
                MaterialProps target = layeredProps[li];
                foreach (MaterialProp<Color> colorProp in target.Colors)
                {
                    if(!addedPropIds.Contains(colorProp.ID)) mergedProps.Colors.Add(colorProp);
                }
                foreach (MaterialProp<float> floatProp in target.Floats)
                {
                    if(!addedPropIds.Contains(floatProp.ID)) mergedProps.Floats.Add(floatProp);
                }
                foreach (MaterialProp<int> intProp in target.Ints)
                {
                    if(!addedPropIds.Contains(intProp.ID)) mergedProps.Ints.Add(intProp);
                }

                foreach (MaterialProp<Texture> textureProp in target.Textures)
                {
                    if(!addedPropIds.Contains(textureProp.ID)) mergedProps.Textures.Add(textureProp);
                }
            }

            return mergedProps;
        }

        public void ResetPropertyBlock()
        {
            _mpb = new MaterialPropertyBlock();
            foreach (Renderer ren in Renderers)
            {
                if(ren == null || !ren.enabled) continue;
                for (var mi = 0; mi < ren.sharedMaterials.Length; mi++)
                {
                    if (ren.sharedMaterials[mi] != null)
                    {
                        ren.SetPropertyBlock(_mpb, mi);
                    }
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
            Dictionary<Shader, int> matNumDict = new();
            foreach (Renderer ren in Renderers)
            {
                if(ren == null) continue;
                MaterialTargetInfoSDictWrapper wrapper = RendererMatTargetInfoWrapperDict[ren];
                for (var mi = 0; mi < ren.sharedMaterials.Length; mi++)
                {
                    Material mat = ren.sharedMaterials[mi];
                    if (mat == null)
                    {
                        Debug.LogWarning($"MPB TargetGroup: {ren.name} has null material.", ren);
                        continue;
                    }
                    if (matNumDict.ContainsKey(mat.shader))
                        matNumDict[mat.shader] += 1;
                    else
                        matNumDict[mat.shader] = 1;

                    TargetInfo targetInfo = wrapper.MatTargetInfoDict[mat];
                    if (!asset.IdMaterialPropsDict.ContainsKey(targetInfo.ID))
                    {
                        var matProps = new MaterialProps(mat, false);
                        matProps.ID = targetInfo.ID;
                        asset.MaterialPropsList.Add(matProps);
                        asset.IdMaterialPropsDict.Add(targetInfo.ID, matProps);
                    }
                }
            }

            // 最も数が多いshaderをglobalに設定
            int maxNum = 0;
            foreach ((Shader shader, int num) in matNumDict)
            {
                if (maxNum < num)
                {
                    asset.GlobalProps.Shader = shader;
                    maxNum = num;
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