using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace sui4.MaterialPropertyBaker.Timeline
{
   
    [Serializable]
    public class MultiMaterialPropClip : PlayableAsset, ITimelineClipAsset
    {
        private MultiMaterialPropTrack _parentTrack;
        private MultiMaterialPropBehaviour _template = new();
        // [SerializeField] private List<PresetIDPair> _presetIDPairs = new List<PresetIDPair>();
        [SerializeField] private BakedPropertyGroup _bakedPropertyGroup;

        public MultiMaterialPropTrack ParentTrack
        {
            get => _parentTrack;
            set => _parentTrack = value;
        }
        
        // public List<PresetIDPair> PresetIDPairs
        // {
        //     get => _presetIDPairs;
        //     set => _presetIDPairs = value;
        // }

        public MaterialGroup BindingMaterialGroup { get; private set; }
        public BakedPropertyGroup BakedPropertyGroup => _bakedPropertyGroup;

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<MultiMaterialPropBehaviour>.Create(graph, _template);
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