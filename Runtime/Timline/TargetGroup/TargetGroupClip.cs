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
        [SerializeField] private bool _editable;
        private TargetGroupBehaviour _template = new();

        public MpbProfile MpbProfile => _mpbProfile;

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TargetGroupBehaviour>.Create(graph, _template);
            var behaviour = playable.GetBehaviour();
            behaviour.Clip = this;

            return playable;
        }
    }
}