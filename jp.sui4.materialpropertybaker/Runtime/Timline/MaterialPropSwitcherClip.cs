using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace sui4.MaterialPropertyBaker.Timeline
{
    [Serializable]
    public class MaterialPropSwitcherClip: PlayableAsset, ITimelineClipAsset
    {
        private MaterialPropSwitcherBehaviour _template = new MaterialPropSwitcherBehaviour();
        
        [SerializeField] private BakedMaterialProperty _presetRef;
        [SerializeField] private BakedMaterialProperty _bakedMaterialProperty;
        [SerializeField] private bool _syncWithPreset = true;
        
        private MaterialPropSwitcherTrack _parentTrack;
        private MaterialGroups _bindingMaterialGroups;

        public MaterialGroups BindingMaterialGroups => _bindingMaterialGroups;
        public MaterialPropSwitcherTrack ParentTrack
        {
            get => _parentTrack;
            set => _parentTrack = value;
        }

        public BakedMaterialProperty BakedMaterialProperty
        {
            get => _bakedMaterialProperty;
            set => _bakedMaterialProperty = value;
        }
        
        public BakedMaterialProperty PresetRef
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

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<MaterialPropSwitcherBehaviour>.Create(graph, _template);
            var behaviour = playable.GetBehaviour();
            behaviour.Clip = this;
            
            var playableDirector = owner.GetComponent<PlayableDirector>();
            if (playableDirector != null)
            {
                GetParentTrack(playableDirector);
            }
            return playable;
        }

        private void GetParentTrack(PlayableDirector playableDirector)
        {
            if(!_bakedMaterialProperty.MaterialProps.IsEmpty()) return;
            
            // trackを取得し、trackをkeyにbindingを取得する
            var timelineAsset = playableDirector.playableAsset;
            foreach (var output in timelineAsset.outputs)
            {
                if(output.sourceObject == null) continue;
                if (output.sourceObject.GetType() != typeof(MaterialPropSwitcherTrack)) continue;
                
                var track = output.sourceObject as TrackAsset;
                var find = SearchThisInTrack(track);
                if (find)
                {
                    ParentTrack = track as MaterialPropSwitcherTrack;
                    var binding = playableDirector.GetGenericBinding(track);
                    if (binding != null)
                    {
                        var materialGroups = binding as MaterialGroups;
                        _bindingMaterialGroups = materialGroups;
                        var config = materialGroups == null ? null : materialGroups.MaterialPropertyConfig;
                        if (config != null)
                        {
                            AddProperties(materialGroups.MaterialPropertyConfig);
                        }
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
                if (mpsClip == this)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddProperties(MaterialPropertyConfig config)
        {
            _bakedMaterialProperty.MaterialPropertyConfig = config;
            _bakedMaterialProperty.ShaderName = config.ShaderName;
            for(int i = 0; i < config.PropertyNames.Count; i++)
            {
                var pName = config.PropertyNames[i];
                var pType = config.PropertyTypes[i];
                if(config.Editable[i])
                    _bakedMaterialProperty.MaterialProps.AddProperty(pName, pType);
                
            }
        }

        private void OnDestroy()
        {
            if(_bakedMaterialProperty == null) return;
            
            Undo.RecordObject(this, "Destroy clip and baked properties");
            // _bakedPropertiesのアセットパスを取得
            string bakedPropertiesPath = AssetDatabase.GetAssetPath(_bakedMaterialProperty);

            // このオブジェクト自身のアセットパスを取得
            string thisAssetPath = AssetDatabase.GetAssetPath(this);

            // _bakedPropertiesが自身の子のアセットであるかどうかを確認
            if (!string.IsNullOrEmpty(bakedPropertiesPath) &&
                bakedPropertiesPath.StartsWith(thisAssetPath))
            {
                Undo.DestroyObjectImmediate(_bakedMaterialProperty);
                DestroyImmediate(_bakedMaterialProperty, true);
                _bakedMaterialProperty = null;
            }
        }

        private void DestroyBakedPropertiesIfChild()
        {
            // _bakedPropertiesのアセットパスを取得
            string bakedPropertiesPath = AssetDatabase.GetAssetPath(_bakedMaterialProperty);

            // このオブジェクト自身のアセットパスを取得
            string thisAssetPath = AssetDatabase.GetAssetPath(this);

            // _bakedPropertiesが自身の子のアセットであるかどうかを確認
            if (!string.IsNullOrEmpty(bakedPropertiesPath) &&
                bakedPropertiesPath.StartsWith(thisAssetPath))
            {
                // Debug.Log($"Destroy BakedProperties: {_bakedProperties.name}");
                Undo.DestroyObjectImmediate(_bakedMaterialProperty);
                DestroyImmediate(_bakedMaterialProperty, true);
                _bakedMaterialProperty = null;
            }
        }

        public void CopyValueOfPresetRef()
        {
            _bakedMaterialProperty.CraetePropsFromMaterialProps(_presetRef.MaterialProps);
        }

        public void InstantiateBakedPropertiesFromPreset()
        {
            Undo.RecordObject(this, "Create BakedProperties from Preset");
            if (_bakedMaterialProperty != null)
            {
                DestroyBakedPropertiesIfChild();
            }

            _bakedMaterialProperty = Instantiate(_presetRef);
            // CopyValueOfPresetRef();
            _bakedMaterialProperty.name = this.name + "_Baked_" + _presetRef.name;
            Undo.RegisterCreatedObjectUndo(_bakedMaterialProperty, "Instantiate BakedProperties FromPreset");
            AssetDatabase.AddObjectToAsset(_bakedMaterialProperty, this);
            // Debug.Log($"Created BakedProperties: {_bakedProperties.name}");

        }
        
        public void CreateBakedProperties()
        {
            Undo.RecordObject(this, "Create BakedProperties");
            if (_bakedMaterialProperty != null)
            {
                DestroyBakedPropertiesIfChild();
            }
            _bakedMaterialProperty = CreateInstance<BakedMaterialProperty>();
            _bakedMaterialProperty.name = this.name + "_BakedProperties";
            Undo.RegisterCreatedObjectUndo(_bakedMaterialProperty, "Create BakedProperties");
            AssetDatabase.AddObjectToAsset(_bakedMaterialProperty, this);
            // Debug.Log($"Created BakedProperties: {_bakedProperties.name}");
        }
        
        public void LoadValuesFromPreset()
        {
            if (_presetRef == null)
            {
                Debug.LogError($"Load skipped because presetRef is null");
                return;
            }
            if (_bakedMaterialProperty == null)
            {
                InstantiateBakedPropertiesFromPreset();
            }
            else
            {
                _bakedMaterialProperty.CopyValuesFromOther(_presetRef);
            }

        }
    }
}