using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace sui4.MaterialPropertyBaker.Timeline
{
    public class MaterialPropSwitcherMixerBehaviour : PlayableBehaviour
    {
        private readonly Dictionary<int, Color> _cMap = new();
        private readonly Dictionary<int, float> _fMap = new();
        private readonly Dictionary<int, Texture> _tMap = new();

        private MaterialProps _matProps;
        private MaterialGroup _trackBinding;
        public MaterialPropSwitcherTrack ParentSwitcherTrack;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _trackBinding = playerData as MaterialGroup;
            if (_trackBinding == null)
                return;

            var inputCount = playable.GetInputCount();
            float totalWeight = 0;

            _cMap.Clear();
            _fMap.Clear();
            _tMap.Clear();
            for (var i = 0; i < inputCount; i++)
            {
                var inputWeight = playable.GetInputWeight(i);
                var sp = (ScriptPlayable<MaterialPropSwitcherBehaviour>)playable.GetInput(i);
                var clip = sp.GetBehaviour().Clip;

                if (clip.PresetRef)
                {
                    _matProps = clip.PresetRef.MaterialProps;
                }
                else
                {
                    if (clip.Props == null) continue;
                    _matProps = clip.Props;
                }

                if (_matProps == null) continue;

                // 各paramの重み付き和
                if (inputWeight > 0)
                {
                    totalWeight += inputWeight;
                    // 重み付き和じゃないといけないので、CreatePropertyBlockFromProfile は使えない
                    foreach (var cProp in _matProps.Colors)
                    {
                        if (_cMap.ContainsKey(cProp.ID))
                            _cMap[cProp.ID] += cProp.Value * inputWeight;
                        else
                            _cMap.Add(cProp.ID, cProp.Value * inputWeight);
                    }
                    
                    foreach (var fProp in _matProps.Floats)
                    {
                        if (_fMap.ContainsKey(fProp.ID))
                            _fMap[fProp.ID] += fProp.Value * inputWeight;
                        else
                            _fMap.Add(fProp.ID, fProp.Value * inputWeight);
                    }


                    foreach (var tProp in _matProps.Textures)
                    {
                        if (_tMap.ContainsKey(tProp.ID))
                            _tMap[tProp.ID] = inputWeight > 0.5 ? tProp.Value : _tMap[tProp.ID];
                        else
                            _tMap.Add(tProp.ID, tProp.Value);
                    }
                }
            }

            if (totalWeight > 0f)
                _trackBinding.SetPropertyBlock(_cMap, _fMap, _tMap);
            else
                _trackBinding.ResetDefaultPropertyBlock();
        }

        public override void OnGraphStart(Playable playable)
        {
            var inputCount = playable.GetInputCount();

            for (var i = 0; i < inputCount; i++)
            {
                var sp = (ScriptPlayable<MaterialPropSwitcherBehaviour>)playable.GetInput(i);
                var clip = sp.GetBehaviour().Clip;
                if (clip.Props == null)
                {
                    if (clip.PresetRef == null)
                        Debug.LogError($"Both Preset and Local Properties are null. {clip.name}");
                }
                else
                {
                    clip.Props.UpdateShaderIDs();
                }
            }

            base.OnGraphStart(playable);
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_trackBinding == null)
                return;

            _trackBinding.ResetDefaultPropertyBlock();
        }
    }
}