using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace sui4.MaterialPropertyBaker.Timeline
{
    [Serializable]
    public class TargetGroupClip : PlayableAsset, ITimelineClipAsset
    {
        private TargetGroupTrack _parentTrack;
        private TargetGroupBehaviour _template = new();
        [SerializeField] private MpbProfile _mpbProfile;
        [SerializeField] private bool _editable;
        public TargetGroupTrack ParentTrack
        {
            get => _parentTrack;
            set => _parentTrack = value;
        }

        public MaterialGroup BindingMaterialGroup { get; private set; }
        public MpbProfile MpbProfile => _mpbProfile;

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TargetGroupBehaviour>.Create(graph, _template);
            var behaviour = playable.GetBehaviour();
            behaviour.Clip = this;

            return playable;
        }

        private bool SearchThisInTrack(in TrackAsset track)
        {
            foreach (var clip in track.GetClips())
            {
                var mpsClip = clip.asset as MaterialPropSwitcherClip;
                if (mpsClip == this) return true;
            }

            return false;
        }
    }
}