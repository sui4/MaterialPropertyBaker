using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace sui4.MaterialPropertyBaker.Timeline
{
    [CustomEditor(typeof(TargetGroupTrack))]
    public class TargetGroupTrackEditor : Editor
    {
        private Editor _profileEditor;
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (target == null) return;
            serializedObject.Update();
            var targetTrack = (TargetGroupTrack) target;
            EditorGUILayout.HelpBox("このProfileはTimelineのDestroy時に適用されます。", MessageType.Info);
            EditorGUILayout.HelpBox("最後のクリップのProfileを設定することで、ControlTrack遷移時のちらつきをなくせます", MessageType.Info);
            PropertyGroupEditor(targetTrack.ProfileToOverrideDefault);
        }
        
        private void PropertyGroupEditor(MpbProfile mpbProfileProp)
        {
            if (mpbProfileProp == null)
                return;
            if (_profileEditor == null)
            {
                _profileEditor = CreateEditor(mpbProfileProp);
            }
            else if (_profileEditor.target != mpbProfileProp)
            {
                DestroyImmediate(_profileEditor);
                _profileEditor = null;
                _profileEditor = CreateEditor(mpbProfileProp);
            }

            if (_profileEditor != null)
            {
                _profileEditor.OnInspectorGUI();
            }
        }
    }
}