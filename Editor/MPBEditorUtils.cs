using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace sui4.MaterialPropertyBaker
{
    public static class MPBEditorUtils
    {
        public static bool IsMatchShaderType(ShaderPropertyType s1, ShaderPropertyType s2)
        {
            if (s1 == s2) return true;
            switch (s1)
            {
                case ShaderPropertyType.Float when s2 == ShaderPropertyType.Range:
                case ShaderPropertyType.Range when s2 == ShaderPropertyType.Float:
                    return true;
                case ShaderPropertyType.Color:
                case ShaderPropertyType.Vector:
                case ShaderPropertyType.Texture:
                case ShaderPropertyType.Int:
                default:
                    return false;
            }
        }
        public static void ParseShaderAttribute(IEnumerable<string> propAttributes, out List<string> attribs,
            out List<string> parameters)
        {
            attribs = new List<string>();
            parameters = new List<string>();
            foreach (var attr in propAttributes)
            {
                if (attr.IndexOf("Space", StringComparison.Ordinal) == 0) continue;
                if (attr.IndexOf("Header", StringComparison.Ordinal) == 0) continue;

                var matches = Regex.Matches(attr, @".*(?=\()"); //括弧の前を抽出
                if (matches.Count != 0)
                {
                    attribs.Add(matches[0].Value);
                    var paramMatches = Regex.Matches(attr, @"(?<=\().*(?=\))"); //括弧内を抽出
                    parameters.Add(paramMatches.Count != 0 ? paramMatches[0].Value : null);
                }
                else
                {
                    //括弧がない場合
                    attribs.Add(attr);
                    parameters.Add(null);
                }
            }
        }
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