using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(BakedPropertyGroup))]
    public class BakedPropertyGroupEditor : Editor
    {
        private BakedPropertyGroup Target => (BakedPropertyGroup)target;
        private List<string> _warnings = new List<string>();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            WarningGUI(Target.Warnings);
        }

        private void PairGUI()
        {
        }

        private void WarningGUI(List<string> warnings)
        {
            // helpBox
            if (warnings.Count > 0)
            {
                foreach (var warning in warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }
        }
    }
}