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

        [SerializeField] private BakedProperties _bakedProperties;

        [SerializeField] private BakedProperties _presetRef;
        [SerializeField] private bool _syncWithPreset = true;
        
        public BakedProperties BakedProperties
        {
            get => _bakedProperties;
            set => _bakedProperties = value;
        }
        
        public BakedProperties PresetRef
        {
            get => _presetRef;
            set => _presetRef = value;
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
                _bakedProperties = Instantiate(_presetRef);
            }
            else if(_bakedProperties == null)
            {
                _bakedProperties = CreateInstance<BakedProperties>();
            }
            else
            {
                // do nothing
            }
            _bakedProperties.UpdateShaderID();
        }

        public void CopyValueOfPresetRef()
        {
            _bakedProperties.CraetePropsFromMaterialProps(_presetRef.MaterialProps);
        }

        public void InstantiateBakedPropertiesFromPreset()
        {
            if (_bakedProperties != null)
            {
                DestroyImmediate(_bakedProperties);
                _bakedProperties = null;
            }
            _bakedProperties = Instantiate(_presetRef);
        }
    }
}