using System.Dynamic;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace sui4.MaterialPropertyBaker.Timeline
{
    [CustomTimelineEditor(typeof(MaterialPropSwitcherClip))]
    public class MaterialPropSwitcherClipTimelineEditor: ClipEditor
    {

        public override void OnClipChanged(TimelineClip clip)
        {
            base.OnClipChanged(clip);
            var mClip = clip.asset as MaterialPropSwitcherClip;
            if(mClip.BakedMaterialProperty != null && mClip.BakedMaterialProperty.name != clip.displayName)
            {
                if (mClip.PresetRef)
                {
                    mClip.BakedMaterialProperty.name = clip.asset.name + "_" + mClip.PresetRef.name;
                }
                else
                {
                    mClip.BakedMaterialProperty.name = clip.asset.name + "_" + clip.displayName;
                }
                Undo.RegisterCompleteObjectUndo(mClip.BakedMaterialProperty, "Baked Material Property Changed");
            }
        }

        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            base.OnCreate(clip, track, clonedFrom);
            var mClip = clip.asset as MaterialPropSwitcherClip;
            var mDuplicateClip = clonedFrom?.asset as MaterialPropSwitcherClip;
            if(mDuplicateClip != null && mDuplicateClip.BakedMaterialProperty != null)
            {
                // clone by doing a deepcopy
                mClip.BakedMaterialProperty = Object.Instantiate(mDuplicateClip.BakedMaterialProperty);
                mClip.BakedMaterialProperty.name = clip.displayName;
                AssetDatabase.AddObjectToAsset(mClip.BakedMaterialProperty, mClip);
            }
            else
            {
                // Create a new setting
                mClip.BakedMaterialProperty = ScriptableObject.CreateInstance<BakedMaterialProperty>();
                mClip.BakedMaterialProperty.name = clip.displayName;
                AssetDatabase.AddObjectToAsset(mClip.BakedMaterialProperty, mClip);
            }
        }
    }
}