using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;

namespace sui4.MaterialPropertyBaker.Timeline
{

    public class TargetGroupMixerBehaviour : PlayableBehaviour
    {
        private readonly Dictionary<MpbProfile, float> _profileWeightDict = new();

        private TargetGroup _trackBinding;
        public TargetGroupTrack ParentSwitcherTrack;
        private Dictionary<int, bool> _isWarningLogged = new();

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _trackBinding = playerData as TargetGroup;
            if (_trackBinding == null)
                return;

            var inputCount = playable.GetInputCount();
            float totalWeight = 0;

            _profileWeightDict.Clear();

            for (var i = 0; i < inputCount; i++)
            {
                var inputWeight = playable.GetInputWeight(i);
                // 各paramの重み付き和
                if (inputWeight > 0)
                {
                    var sp = (ScriptPlayable<TargetGroupBehaviour>)playable.GetInput(i);
                    var clip = sp.GetBehaviour().Clip;
                    if (clip.BakedPropertyGroup == null)
                    {
                        if (inputWeight < 1 && !_isWarningLogged[i])
                        {
                            Debug.LogWarning(
                                $"{clip.name} has no MPBProfile.\n This can lead to unexpected behavior when blending.");
                            _isWarningLogged[i] = true;
                        }

                        continue;
                    }

                    totalWeight += inputWeight;
                    if (_profileWeightDict.TryGetValue(clip.BakedPropertyGroup, out var weight))
                    {
                        _profileWeightDict[clip.BakedPropertyGroup] = weight + inputWeight;
                    }
                    else
                    {
                        _profileWeightDict.Add(clip.BakedPropertyGroup, inputWeight);
                    }
                    
                }
            }

            if (totalWeight > 0f)
            {
                _trackBinding.SetPropertyBlock(_profileWeightDict);
            }
            else
            {
                _trackBinding.ResetToDefault();
            }
        }

        public override void OnGraphStart(Playable playable)
        {
            var inputCount = playable.GetInputCount();
            _isWarningLogged.Clear();

            for (var i = 0; i < inputCount; i++)
            {
                _isWarningLogged.Add(i, false);
                var sp = (ScriptPlayable<TargetGroupBehaviour>)playable.GetInput(i);
                var clip = sp.GetBehaviour().Clip;
                if (clip.BakedPropertyGroup == null) continue;
                
                
                foreach (var matProps in clip.BakedPropertyGroup.MaterialPropsList)
                {
                    if(matProps == null) continue;
                    matProps.UpdateShaderID();
                }
            }

            base.OnGraphStart(playable);
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_trackBinding == null)
                return;

            _trackBinding.ResetToDefault();
        }
    }
}