using System.Collections.Generic;
using System.Linq;
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
        private Dictionary<int, bool> _isWarningLogged = new();

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
                // 各paramの重み付き和
                if (inputWeight > 0)
                {
                    var sp = (ScriptPlayable<MultiMaterialPropBehaviour>)playable.GetInput(i);
                    var clip = sp.GetBehaviour().Clip;
                    if (clip.BakedPropertyGroup == null)
                    {
                        if (inputWeight < 1 && !_isWarningLogged[i])
                        {
                            Debug.LogWarning(
                                $"{clip.name} has no BakedPropertyGroup.\n This can lead to unexpected behavior when blending.");
                            _isWarningLogged[i] = true;
                        }

                        continue;
                    }

                    totalWeight += inputWeight;

                    foreach (var presetIDPair in clip.BakedPropertyGroup.PresetIDPairs)
                    {
                        var preset = presetIDPair.Preset;
                        if (preset == null || string.IsNullOrWhiteSpace(presetIDPair.ID)) continue;
                        if (!_propDict.TryGetValue(presetIDPair.ID, out var propShaderIDDict))
                        {
                            propShaderIDDict = new PropShaderIDDict();
                        }

                        foreach (var cProp in preset.MaterialProps.Colors)
                        {
                            if (propShaderIDDict.ColorPropDict.ContainsKey(cProp.ID))
                                propShaderIDDict.ColorPropDict[cProp.ID].Value += cProp.Value * inputWeight;
                            else
                                propShaderIDDict.ColorPropDict.Add(cProp.ID,
                                    new MaterialProp<Color>(cProp.Name, cProp.Value * inputWeight));
                        }

                        foreach (var fProps in preset.MaterialProps.Floats)
                        {
                            if (propShaderIDDict.FloatPropDict.ContainsKey(fProps.ID))
                                propShaderIDDict.FloatPropDict[fProps.ID].Value += fProps.Value * inputWeight;
                            else
                                propShaderIDDict.FloatPropDict.Add(fProps.ID,
                                    new MaterialProp<float>(fProps.Name, fProps.Value * inputWeight));
                        }

                        _propDict.TryAdd(presetIDPair.ID, propShaderIDDict);
                    }
                }
            }

            if (totalWeight > 0f)
            {
                var prop = new Dictionary<string, MaterialProps>();
                foreach (var (presetID, propShaderIDDict) in _propDict)
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
            _isWarningLogged.Clear();

            for (var i = 0; i < inputCount; i++)
            {
                _isWarningLogged.Add(i, false);
                var sp = (ScriptPlayable<MultiMaterialPropBehaviour>)playable.GetInput(i);
                var clip = sp.GetBehaviour().Clip;
                if (clip.BakedPropertyGroup == null) continue;
                foreach (var presetIDPair in clip.BakedPropertyGroup.PresetIDPairs)
                {
                    if (presetIDPair.Preset != null)
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