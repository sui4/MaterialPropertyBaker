using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(MaterialGroupList))]
    public class MaterialGroupListEditor : Editor
    {
        private List<MaterialGroup> _materialGroupList = new List<MaterialGroup>();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}