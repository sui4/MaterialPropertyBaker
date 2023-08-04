using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(ChildRenderersAdderToMaterialGroup))]
    public class ChildRenderersAdderToMaterialGroupEditor : Editor
    {
        private ChildRenderersAdderToMaterialGroup Target => (ChildRenderersAdderToMaterialGroup) target;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Separator();
            
            if (GUILayout.Button("Update"))
            {
                Target.OnValidate();
            }
        }
    }
}