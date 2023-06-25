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
        [SerializeField] private BakedProperties _defaultProfile;
        [SerializeField] private BakedProperties _profileAppliedOnDestroy;

        public BakedProperties DefaultProfile
        {
            get => _defaultProfile;
            set => _defaultProfile = value;
        }
        
        public BakedProperties ProfileAppliedOnDestroy
        {
            get => _profileAppliedOnDestroy;
            set => _profileAppliedOnDestroy = value;
        }
        
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var mixer = ScriptPlayable<MaterialPropSwitcherMixerBehaviour>.Create(graph, inputCount);
            var switcherTimelineMixer = mixer.GetBehaviour();
            switcherTimelineMixer.ParentSwitcherTrack = this;
            return mixer;
        }
    }
}