using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace sui4.MaterialPropertyBaker.Timeline
{
    [Serializable]
    public class MaterialPropSwitcherClip: PlayableAsset, ITimelineClipAsset
    {
        private MaterialPropSwitcherBehaviour _template = new MaterialPropSwitcherBehaviour();

        [SerializeField] private BakedProperties _presetRef;
        [SerializeField] private MaterialProps _materialProps = new MaterialProps();
        [SerializeField] private bool _syncWithPreset = true;
        
        public BakedProperties PresetRef
        {
            get => _presetRef;
            set => _presetRef = value;
        }
        
        public MaterialProps MaterialProps
        {
            get => _materialProps;
            set => _materialProps = value;
        }
        
        public bool SyncWithPreset
        {
            get => _syncWithPreset;
            set => _syncWithPreset = value;
        }
        
        public ClipCaps clipCaps => ClipCaps.Blending;

        private void OnEnable()
        {
            Initialize();
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<MaterialPropSwitcherBehaviour>.Create(graph, _template);
            var behaviour = playable.GetBehaviour();
            behaviour.Clip = this;

            return playable;
        }

        public void Initialize()
        {
            if (_presetRef != null && _syncWithPreset)
            {
                LoadProfile(_presetRef);
            }
            else if(_materialProps.Colors.Count == 0 && _materialProps.Floats.Count == 0)
            {
                var profile = CreateInstance<BakedProperties>();
                LoadProfile(profile);
            }
            else
            {
                // do nothing
            }
            _materialProps.UpdateShaderID();
        }

        public void LoadProfile(BakedProperties profile)
        {
            if (profile == null)
            {
                Debug.LogWarning("preset profile is null. ");
                return;
            }
            profile.GetCopyProperties(out var colors, out var floats);
            _materialProps.Colors = colors;
            _materialProps.Floats = floats;
        }
    }
}