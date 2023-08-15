using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace sui4.MaterialPropertyBaker.Timeline
{
    [TrackClipType(typeof(MultiMaterialPropClip))]
    [TrackBindingType(typeof(MaterialGroupList))]
    [DisplayName("Material Property Baker/Multi Material Prop Track")]
    public class MultiMaterialPropTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var mixer = ScriptPlayable<MultiMaterialPropMixerBehaviour>.Create(graph, inputCount);
            var switcherTimelineMixer = mixer.GetBehaviour();
            switcherTimelineMixer.ParentSwitcherTrack = this;
            return mixer;
        }
    }
}