using System.Collections.Generic;
using System.Linq;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.Playables;

namespace sui4.MaterialPropertyBaker.Timeline
{
    public class PropShaderIDDict
    {
        public readonly Dictionary<int, MaterialProp<Color>> ColorPropDict = new();
        public readonly Dictionary<int, MaterialProp<float>> FloatPropDict = new();
        
        public List<MaterialProp<Color>> ColorProps => ColorPropDict.Values.ToList();
        public List<MaterialProp<float>> FloatProps => FloatPropDict.Values.ToList();
    }
    public class MultiMaterialPropMixerBehaviour : PlayableBehaviour
    {
        private readonly Dictionary<string, PropShaderIDDict> _propDict = new();

        private MaterialGroupList _trackBinding;
        public MultiMaterialPropTrack ParentSwitcherTrack;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _trackBinding = playerData as MaterialGroupList;
            if (_trackBinding == null)
                return;

            var inputCount = playable.GetInputCount();
            float totalWeight = 0;
            
            _propDict.Clear();

            for (var i = 0; i < inputCount; i++)
            {
                var inputWeight = playable.GetInputWeight(i);
                var sp = (ScriptPlayable<MultiMaterialPropBehaviour>)playable.GetInput(i);
                var clip = sp.GetBehaviour().Clip;

                // 各paramの重み付き和
                if (inputWeight > 0)
                {
                    totalWeight += inputWeight;
                    
                    foreach (var presetIDPair in clip.PresetIDPairs)
                    {
                        var presetId = presetIDPair.ID;
                        var propShaderIDDict = new PropShaderIDDict();
                        foreach (var cProp in presetIDPair.Preset.MaterialProps.Colors)
                        {
                            if (propShaderIDDict.ColorPropDict.TryGetValue(cProp.ID, out var value))
                                value.Value += cProp.Value * inputWeight;
                            else
                                propShaderIDDict.ColorPropDict.Add(cProp.ID ,new MaterialProp<Color>(cProp.Name, cProp.Value));
                        }

                        foreach (var fProps in presetIDPair.Preset.MaterialProps.Floats)
                        {
                            if(propShaderIDDict.FloatPropDict.ContainsKey(fProps.ID))
                                propShaderIDDict.FloatPropDict[fProps.ID].Value += fProps.Value * inputWeight;
                            else
                                propShaderIDDict.FloatPropDict.Add(fProps.ID, new MaterialProp<float>(fProps.Name ,fProps.Value * inputWeight));
                        }
                        _propDict.TryAdd(presetId, propShaderIDDict);
                    }
                }
            }

            if (totalWeight > 0f)
            {
                var prop = new Dictionary<string, MaterialProps>();
                foreach (var (presetID, propShaderIDDict ) in _propDict)
                {
                    prop.Add(presetID, new MaterialProps(propShaderIDDict.ColorProps, propShaderIDDict.FloatProps));
                }
                _trackBinding.SetPropertyBlock(prop);
            }
            else
            {
                _trackBinding.ResetPropertyBlockToDefault();
            }
        }

        public override void OnGraphStart(Playable playable)
        {
            var inputCount = playable.GetInputCount();

            for (var i = 0; i < inputCount; i++)
            {
                var sp = (ScriptPlayable<MultiMaterialPropBehaviour>)playable.GetInput(i);
                var clip = sp.GetBehaviour().Clip;
                foreach (var presetIDPair in clip.PresetIDPairs)
                {
                    if(presetIDPair.Preset != null)
                        presetIDPair.Preset?.UpdateShaderID();
                }
            }

            base.OnGraphStart(playable);
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_trackBinding == null)
                return;

            _trackBinding.ResetPropertyBlockToDefault();
        }
    }
}