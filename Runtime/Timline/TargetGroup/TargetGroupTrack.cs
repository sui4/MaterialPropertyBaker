using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace sui4.MaterialPropertyBaker.Timeline
{
    [TrackClipType(typeof(MultiMaterialPropClip))]
    [TrackBindingType(typeof(MaterialGroupList))]
    [DisplayName("Material Property Baker/TargetGroup Track")]
    public class TargetGroupTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var mixer = ScriptPlayable<TargetGroupMixerBehaviour>.Create(graph, inputCount);
            var switcherTimelineMixer = mixer.GetBehaviour();
            switcherTimelineMixer.ParentSwitcherTrack = this;
            return mixer;
        }
    }
}