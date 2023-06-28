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
        
        private MaterialPropSwitcherTrack _parentTrack;
        private MaterialGroups _bindingMaterialGroups;

        [SerializeField] private bool _editable = false;
        
        public bool Editable
        {
            get => _editable;
            set => _editable = value;
        }

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
#if UNITY_EDITOR
            Undo.RecordObject(this, "OnDestroyClip");
            Undo.RecordObject(BakedMaterialProperty, "OnDestroyClip and BakedMaterialProperty");
            
#endif
            DestroyBakedPropertyIfChild();
        }
        
        private void DestroyBakedPropertyIfChild()
        {
#if UNITY_EDITOR
            var bakedProperties = _bakedMaterialProperty;
            if(bakedProperties == null) return;
            
            // _bakedPropertiesのアセットパスを取得

            string bakedPropertiesPath = AssetDatabase.GetAssetPath(bakedProperties);
            

            if (string.IsNullOrEmpty(bakedPropertiesPath))
            {
                DestroyImmediate(bakedProperties);
                bakedProperties = null;
            }
            else
            {
                // このオブジェクト自身のアセットパスを取得
                string thisAssetPath = AssetDatabase.GetAssetPath(this);

                // _bakedPropertiesが自身の子のアセットであるかどうかを確認
                if (!string.IsNullOrEmpty(bakedPropertiesPath) &&
                    bakedPropertiesPath.StartsWith(thisAssetPath))
                {
                    Debug.Log($"Destroy BakedProperties: {_bakedMaterialProperty.name}");
                    Undo.DestroyObjectImmediate(bakedProperties);

                    DestroyImmediate(bakedProperties, true);
                    _bakedMaterialProperty = null;
                }
            }
#endif
        }
    }
}