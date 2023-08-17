using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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

        public static void DestroyScriptableObjectIfExist<T>(ref T scriptableObject) where T : ScriptableObject
        {
            if (scriptableObject != null)
            {
                if (!AssetDatabase.IsMainAsset(scriptableObject))
                {
                    Object.DestroyImmediate(scriptableObject);
                }

                scriptableObject = null;
            }
        }

        public static void CreateAsset(in ScriptableObject assetToSave, out ScriptableObject saved, Type type,
            string defaultName, string title, string message)
        {
            var path = EditorUtility.SaveFilePanelInProject(title, defaultName, "asset",
                message);
            ExportScriptableObject(assetToSave, path, out saved, type);
        }

        // path: Assets以下のパス, ファイル名込み
        public static bool ExportScriptableObject(in ScriptableObject scriptableObject, string path,
            out ScriptableObject exported, Type type, bool refresh = true)
        {
            exported = null;
            if (string.IsNullOrEmpty(path))
            {
                // Debug.LogError($"Failed to export : path is null or empty.");
                return false;
            }

            if (scriptableObject == null)
            {
                Debug.LogError("Failed to export : target object is null.");
            }

            exported = Object.Instantiate(scriptableObject);
            EditorUtility.SetDirty(exported);

            if (File.Exists(path))
            {
                Debug.Log($"{type}: delete existing: {path}");
                var success = AssetDatabase.DeleteAsset(path);
                if (!success)
                {
                    Debug.LogError($"{type}: failed to delete existing: {path}");
                    return false;
                }
            }

            AssetDatabase.CreateAsset(exported, path);
            if (refresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"Saved : {path}");
            return true;
        }
    }
}