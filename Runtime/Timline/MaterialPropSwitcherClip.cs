using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace sui4.MaterialPropertyBaker.Timeline
{
    [Serializable]
    public class MaterialPropSwitcherClip : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] private MaterialProps _props;
        [SerializeField] private BakedMaterialProperty _presetRef;
        [SerializeField] private bool _editable;
        private MaterialPropSwitcherTrack _parentTrack;
        private MaterialPropSwitcherBehaviour _template = new();

        public MaterialProps Props
        {
            get => _props;
            set => _props = value;
        }

        public BakedMaterialProperty PresetRef
        {
            get => _presetRef;
            set => _presetRef = value;
        }

        public MaterialPropSwitcherTrack ParentTrack
        {
            get => _parentTrack;
            set => _parentTrack = value;
        }

        public MaterialGroup BindingMaterialGroup { get; private set; }

        public bool Editable
        {
            get => _editable;
            set => _editable = value;
        }

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<MaterialPropSwitcherBehaviour>.Create(graph, _template);
            var behaviour = playable.GetBehaviour();
            behaviour.Clip = this;

            var playableDirector = owner.GetComponent<PlayableDirector>();
            if (playableDirector != null) GetParentTrack(playableDirector);
            return playable;
        }

        private void GetParentTrack(PlayableDirector playableDirector)
        {
            if (!Props.IsEmpty()) return;

            // trackを取得し、trackをkeyにbindingを取得する
            var timelineAsset = playableDirector.playableAsset;
            foreach (var output in timelineAsset.outputs)
            {
                if (output.sourceObject == null) continue;
                if (output.sourceObject.GetType() != typeof(MaterialPropSwitcherTrack)) continue;

                var track = output.sourceObject as TrackAsset;
                var find = SearchThisInTrack(track);
                if (find)
                {
                    ParentTrack = track as MaterialPropSwitcherTrack;
                    var binding = playableDirector.GetGenericBinding(track);
                    if (binding != null)
                    {
                        var materialGroups = binding as MaterialGroup;
                        BindingMaterialGroup = materialGroups;
                        var config = materialGroups == null ? null : materialGroups.MaterialPropertyConfig;
                        if (config != null) AddProperties(materialGroups.MaterialPropertyConfig);
                    }

                    break;
                }
            }
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

        private void AddProperties(MaterialPropertyConfig config)
        {
            for (var i = 0; i < config.PropertyNames.Count; i++)
            {
                var pName = config.PropertyNames[i];
                var pType = config.PropertyTypes[i];
                if (config.Editable[i])
                    Props.AddProperty(pName, pType);
            }
        }
    }
}