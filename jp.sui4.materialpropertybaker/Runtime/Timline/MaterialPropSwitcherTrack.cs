using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace sui4.MaterialPropertyBaker.Timeline
{
    [TrackClipType(typeof(MaterialPropSwitcherClip))]
    [TrackBindingType(typeof(MaterialGroups))]
    [DisplayName("Material Property Baker/Material Prop Switcher Track")]
    public class MaterialPropSwitcherTrack : TrackAsset
    {

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var mixer = ScriptPlayable<MaterialPropSwitcherMixerBehaviour>.Create(graph, inputCount);
            var switcherTimelineMixer = mixer.GetBehaviour();
            switcherTimelineMixer.ParentSwitcherTrack = this;
            return mixer;
        }
    }
}