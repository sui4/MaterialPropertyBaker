using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    public class Utils
    {
        public static void CreatePropertyBlockFromProps(out MaterialPropertyBlock mpb, in MaterialProps props)
        {
            mpb = new MaterialPropertyBlock();
            UpdatePropertyBlockFromProps(ref mpb, props);
        }

        // 既存のPropertyBlockに追加する, プロパティが重複している場合は上書きする
        public static void UpdatePropertyBlockFromProps(ref MaterialPropertyBlock mpb, in MaterialProps props)
        {
            foreach (MaterialProp<Color> c in props.Colors)
                mpb.SetColor(c.ID, c.Value);
            foreach (MaterialProp<float> f in props.Floats)
                mpb.SetFloat(f.ID, f.Value);
            foreach (MaterialProp<int> i in props.Ints)
                mpb.SetInteger(i.ID, i.Value);
            foreach (MaterialProp<Texture> t in props.Textures)
                mpb.SetTexture(t.ID, t.Value);
        }

        public static void UpdatePropertyBlockFromDict(ref MaterialPropertyBlock mpb, Dictionary<int, Color> cPropDict,
            Dictionary<int, float> fPropDict, Dictionary<int, int> iPropDict, Dictionary<int, Texture> tPropDict)
        {
            foreach ((int shaderID, Color value) in cPropDict) mpb.SetColor(shaderID, value);

            foreach ((int shaderID, float value) in fPropDict) mpb.SetFloat(shaderID, value);
            
            foreach ((int shaderID, int value) in iPropDict) mpb.SetInteger(shaderID, value);
            
            foreach ((int shaderID, Texture value) in tPropDict) mpb.SetTexture(shaderID, value);
        }

        public static string UnderscoresToSpaces(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // アンダースコアをスペースに置き換え
            var replaced = input.Replace('_', ' ');

            // 先頭がスペースの場合は消去
            if (replaced.Length > 0 && replaced[0] == ' ') replaced = replaced.Substring(1);

            return replaced;
        }

        public static string MakeFileNameSafe(string fileName)
        {
            // ファイル名に使えない記号: \ / : * ? " < > |
            const string pattern = "[\\\\/:*?\"<>|]";

            // 正規表現を使って、指定した記号をハイフンに置き換える
            return Regex.Replace(fileName, pattern, "-");
        }

#if UNITY_EDITOR
        public static void CreateAsset(ScriptableObject assetToSave, string defaultName, string title, string message)
        {
            string path = EditorUtility.SaveFilePanelInProject(title, defaultName, "asset", message);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Failed to Create Asset: Invalid Path");
                return;
            }

            AssetDatabase.CreateAsset(assetToSave, path);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}