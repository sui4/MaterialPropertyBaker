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
        
        [SerializeField] private BakedProperties _presetRef;
        [SerializeField] private BakedProperties _bakedProperties;
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

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<MaterialPropSwitcherBehaviour>.Create(graph, _template);
            var behaviour = playable.GetBehaviour();
            behaviour.Clip = this;
            return playable;
        }

        private void OnDestroy()
        {
            if(_bakedProperties == null) return;
            
            Undo.RecordObject(this, "Destroy clip and baked properties");
            // _bakedPropertiesのアセットパスを取得
            string bakedPropertiesPath = AssetDatabase.GetAssetPath(_bakedProperties);

            // このオブジェクト自身のアセットパスを取得
            string thisAssetPath = AssetDatabase.GetAssetPath(this);

            // _bakedPropertiesが自身の子のアセットであるかどうかを確認
            if (!string.IsNullOrEmpty(bakedPropertiesPath) &&
                bakedPropertiesPath.StartsWith(thisAssetPath))
            {
                Undo.DestroyObjectImmediate(_bakedProperties);
                DestroyImmediate(_bakedProperties, true);
                _bakedProperties = null;
            }
        }

        private void DestroyBakedPropertiesIfChild()
        {
            // _bakedPropertiesのアセットパスを取得
            string bakedPropertiesPath = AssetDatabase.GetAssetPath(_bakedProperties);

            // このオブジェクト自身のアセットパスを取得
            string thisAssetPath = AssetDatabase.GetAssetPath(this);

            // _bakedPropertiesが自身の子のアセットであるかどうかを確認
            if (!string.IsNullOrEmpty(bakedPropertiesPath) &&
                bakedPropertiesPath.StartsWith(thisAssetPath))
            {
                Debug.Log($"Destroy BakedProperties: {_bakedProperties.name}");
                Undo.DestroyObjectImmediate(_bakedProperties);
                DestroyImmediate(_bakedProperties, true);
                _bakedProperties = null;
            }
        }

        public void CopyValueOfPresetRef()
        {
            _bakedProperties.CraetePropsFromMaterialProps(_presetRef.MaterialProps);
        }

        public void InstantiateBakedPropertiesFromPreset()
        {
            Undo.RecordObject(this, "Create BakedProperties from Preset");
            if (_bakedProperties != null)
            {
                DestroyBakedPropertiesIfChild();
            }
            _bakedProperties = CreateInstance<BakedProperties>();
            LoadValuesFromPreset();
            _bakedProperties.name = this.name + "_Baked_" + _presetRef.name;
            Undo.RegisterCreatedObjectUndo(_bakedProperties, "Instantiate BakedProperties FromPreset");
            AssetDatabase.AddObjectToAsset(_bakedProperties, this);
            Debug.Log($"Created BakedProperties: {_bakedProperties.name}");

        }
        
        public void CreateBakedProperties()
        {
            Undo.RecordObject(this, "Create BakedProperties");
            if (_bakedProperties != null)
            {
                DestroyBakedPropertiesIfChild();
            }
            _bakedProperties = CreateInstance<BakedProperties>();
            _bakedProperties.name = this.name + "_BakedProperties";
            Undo.RegisterCreatedObjectUndo(_bakedProperties, "Create BakedProperties");
            AssetDatabase.AddObjectToAsset(_bakedProperties, this);
            Debug.Log($"Created BakedProperties: {_bakedProperties.name}");
        }
        
        public void LoadValuesFromPreset()
        {
            if (_presetRef != null && _bakedProperties != null)
            {
                _bakedProperties.CopyValuesFromOther(_presetRef);
            }
            else
            {
                Debug.LogError($"Load skipped because presetRef or bakedProperties is null");
            }
        }
    }
}