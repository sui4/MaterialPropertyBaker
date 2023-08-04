#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    public static class MaterialFinder
    {
        // シェーダー名から全てのマテリアルを取得
        public static List<Material> GetMaterialsByShaderName(in string shaderName)
        {
            string[] guids = AssetDatabase.FindAssets("t:Material");
            return GetMaterialsByShaderName(shaderName, guids);
        }

        public static List<Material> GetMaterialsByShaderNameInFolder(string shaderName, string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", new string[] { folderPath });
            return GetMaterialsByShaderName(shaderName, guids);
        }

        public static List<Material> GetMaterialsByShaderName(in string shaderName, in string[] guids)
        {
            List<Material> materialsWithShader = new List<Material>();

            bool filter = !string.IsNullOrEmpty(shaderName);
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

                if (filter)
                {
                    if (material.shader.name == shaderName)
                        materialsWithShader.Add(material);
                }
                else
                {
                    materialsWithShader.Add(material);
                }
            }

            return materialsWithShader;
        }
    }
}

#endif