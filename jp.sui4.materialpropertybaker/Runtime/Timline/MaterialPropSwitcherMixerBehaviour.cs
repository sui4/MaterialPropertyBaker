using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace sui4.MaterialPropertyBaker.Timeline
{
    public class MaterialPropSwitcherMixerBehaviour : PlayableBehaviour
    {
        private MaterialGroups _trackBinding;
        public MaterialPropSwitcherTrack ParentSwitcherTrack;

        private MaterialPropertyBlock _mpb;
        
        private Dictionary<int, Color> _cMap = new Dictionary<int, Color>();
        private Dictionary<int, float> _fMap = new Dictionary<int, float>();

        private MaterialProps _matProps;
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // base.ProcessFrame(playable, info, playerData);
            _trackBinding = playerData as MaterialGroups;
            if (_trackBinding == null)
                return;

            int inputCount = playable.GetInputCount();
            float totalWeight = 0;
            _mpb = new MaterialPropertyBlock();

            _cMap.Clear();
            _fMap.Clear();
            for(int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                var sp = (ScriptPlayable<MaterialPropSwitcherBehaviour>)playable.GetInput(i);
                var clip = sp.GetBehaviour().Clip;

                if (clip.SyncWithPreset)
                {
                    _matProps = clip.PresetRef.MaterialProps;
                }
                else
                {
                    if(clip.BakedMaterialProperty == null) continue;
                    _matProps = clip.BakedMaterialProperty.MaterialProps;
                }
                
                if(_matProps == null) continue;
                
                // 各paramの重み付き和
                if (inputWeight > 0)
                {
                    totalWeight += inputWeight;
                    // 重み付き和じゃないといけないので、CreatePropertyBlockFromProfile は使えない
                    foreach (var cProp in _matProps.Colors)
                    {
                        if(_cMap.ContainsKey(cProp.ID))
                            _cMap[cProp.ID] += cProp.Value * inputWeight;
                        else
                            _cMap.Add(cProp.ID, cProp.Value * inputWeight);
                        
                        _mpb.SetColor(cProp.ID, _cMap[cProp.ID]);
                    }

                    foreach (var fProp in _matProps.Floats)
                    {
                        if(_fMap.ContainsKey(fProp.ID))
                            _fMap[fProp.ID] += fProp.Value * inputWeight;
                        else
                            _fMap.Add(fProp.ID, fProp.Value * inputWeight);
                        
                        _mpb.SetFloat(fProp.ID, _fMap[fProp.ID]);
                    }
                }
            }

            if (totalWeight == 0f)
            {
                _mpb = new MaterialPropertyBlock();
                if(_trackBinding.OverrideDefaultPreset != null)
                {
                    Utils.CreatePropertyBlockFromProfile(_trackBinding.OverrideDefaultPreset, out _mpb);
                }
                else
                {
                    _mpb = new MaterialPropertyBlock();
                }
            }
            
            SetPropertyBlock(_mpb);
        }

        public override void OnGraphStart(Playable playable)
        {
            int inputCount = playable.GetInputCount();

            for (int i = 0; i < inputCount; i++)
            {
                var sp = (ScriptPlayable<MaterialPropSwitcherBehaviour>)playable.GetInput(i);
                var clip = sp.GetBehaviour().Clip;
                if (clip.BakedMaterialProperty == null)
                {
                    Debug.LogError($"bakedProperties is null. {clip.name}");
                }
                clip.BakedMaterialProperty.UpdateShaderID();

                // sync with preset
                if (clip.SyncWithPreset)
                {
                    if (clip.PresetRef == null)
                        clip.SyncWithPreset = false;
                    else
                        clip.LoadValuesFromPreset();
                }
            }
            base.OnGraphStart(playable);
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_trackBinding == null)
                return;

            if (_trackBinding.OverrideDefaultPreset != null)
            {
                Utils.CreatePropertyBlockFromProfile(_trackBinding.OverrideDefaultPreset, out _mpb);                
            }
            else
            {
                _mpb = new MaterialPropertyBlock();
            }
            SetPropertyBlock(_mpb);
            _mpb = null;
        }

        private void SetPropertyBlock(in MaterialPropertyBlock mpb)
        {
            for (int lli = 0; lli < _trackBinding.MaterialStatusListList.Count; lli++)
            {
                var list = _trackBinding.MaterialStatusListList[lli];
                var renderer = list.Renderer;
                for (int li = 0; li < list.MaterialStatuses.Count; li++)
                {
                    var matStatus = list.MaterialStatuses[li];
                    if (matStatus.IsTarget)
                    {
                        renderer.SetPropertyBlock(mpb, li);
                    }
                }
            }
        }

    }
}