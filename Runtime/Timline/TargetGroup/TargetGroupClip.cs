using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace sui4.MaterialPropertyBaker.Timeline
{
    [Serializable]
    public class TargetGroupClip : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] private MpbProfile _mpbProfile;
        [SerializeField] private bool _editable; // for editor GUI
        private TargetGroupBehaviour _template = new();

        public MpbProfile MpbProfile
        {
            get => _mpbProfile;
            set => _mpbProfile = value;
        }

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            ScriptPlayable<TargetGroupBehaviour> playable = ScriptPlayable<TargetGroupBehaviour>.Create(graph, _template);
            TargetGroupBehaviour behaviour = playable.GetBehaviour();
            behaviour.Clip = this;

            return playable;
        }
    }
}