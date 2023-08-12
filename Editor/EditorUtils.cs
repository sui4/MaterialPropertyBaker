using System.Collections.Generic;
using UnityEditor;

namespace sui4.MaterialPropertyBaker
{
    public static class EditorUtils
    {
        public static void WarningGUI(List<string> warnings)
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