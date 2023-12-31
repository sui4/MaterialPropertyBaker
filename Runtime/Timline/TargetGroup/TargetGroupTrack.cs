﻿using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace sui4.MaterialPropertyBaker.Timeline
{
    [TrackClipType(typeof(TargetGroupClip))]
    [TrackBindingType(typeof(TargetGroup))]
    [DisplayName("Material Property Baker/TargetGroup Track")]
    public class TargetGroupTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject director, int inputCount)
        {
            ScriptPlayable<TargetGroupMixerBehaviour> mixer = ScriptPlayable<TargetGroupMixerBehaviour>.Create(graph, inputCount);
            TargetGroupMixerBehaviour timelineMixer = mixer.GetBehaviour();
            var targetGroup = GetBindingComponent<TargetGroup>(this, director);
            if (targetGroup != null)
            {
                targetGroup.OnValidate();
                timelineMixer.BindingTargetGroup = targetGroup;
            }

            return mixer;
        }

        private static T GetBindingComponent<T>(TrackAsset asset, GameObject gameObject) where T : class
        {
            if (gameObject == null) return default;

            var director = gameObject.GetComponent<PlayableDirector>();
            if (director == null) return default;

            var binding = director.GetGenericBinding(asset) as T;

            return binding switch
            {
                { } component => component,
                _ => default
            };
        }
    }
}