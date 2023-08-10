using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace sui4.MaterialPropertyBaker
{
    [Serializable]
    [CreateAssetMenu(menuName = "MaterialPropertyBaker/BakedProperty")]
    public class BakedMaterialProperty : ScriptableObject
    {
        [SerializeField] private string _shaderName;
        [SerializeField] private MaterialProps _materialProps;

        public string ShaderName
        {
            get => _shaderName;
            set => _shaderName = value;
        }

        public MaterialProps MaterialProps => _materialProps;

        private void OnEnable()
        {
            if (_materialProps == null)
                _materialProps = new MaterialProps();
            UpdateShaderID();
        }

        public void UpdateShaderID()
        {
            if (_materialProps != null)
                _materialProps.UpdateShaderIDs();
        }

        public void CreatePropsFromMaterial(in Material mat)
        {
            _shaderName = mat.shader.name;
            _materialProps = new MaterialProps(mat);
            UpdateShaderID();
        }

        public void CreatePropsFromOther(in MaterialProps matProps)
        {
            _materialProps ??= new MaterialProps();
            _materialProps.CopyValuesFromOther(matProps);
        }

        public void GetCopyProperties(out List<MaterialProp<Color>> cList, out List<MaterialProp<float>> fList,
            out List<MaterialProp<Texture>> tList)
        {
            MaterialProps.GetCopyProperties(out cList, out fList, out tList);
        }

        public void CopyValuesFromOther(in BakedMaterialProperty other)
        {
            _materialProps ??= new MaterialProps();
            _materialProps.CopyValuesFromOther(other.MaterialProps);
            _shaderName = other.ShaderName;
        }

        // shader propertiesに含まれない, またはEditableではないプロパティを削除する
        public void DeleteUnEditableProperties(in MaterialPropertyConfig config)
        {
            if (config == null) return;

            var colorToDelete = new List<MaterialProp<Color>>();
            var floatToDelete = new List<MaterialProp<float>>();
            var textureToDelete = new List<MaterialProp<Texture>>();
            // 含まれないプロパティを探す
            foreach (var colorProp in _materialProps.Colors)
            {
                var index = config.PropertyNames.IndexOf(colorProp.Name);
                if (index == -1)
                    colorToDelete.Add(colorProp);
                else if (config.Editable[index] == false)
                    colorToDelete.Add(colorProp);
            }

            foreach (var floatProp in _materialProps.Floats)
            {
                var index = config.PropertyNames.IndexOf(floatProp.Name);
                if (index == -1)
                    floatToDelete.Add(floatProp);
                else if (config.Editable[index] == false)
                    floatToDelete.Add(floatProp);
            }

            foreach (var textureProp in _materialProps.Textures)
            {
                var index = config.PropertyNames.IndexOf(textureProp.Name);
                if (index == -1)
                    textureToDelete.Add(textureProp);
                else if (config.Editable[index] == false)
                    textureToDelete.Add(textureProp);
            }

            // 該当プロパティを削除
            foreach (var c in colorToDelete)
                _materialProps.Colors.Remove(c);
            foreach (var f in floatToDelete)
                _materialProps.Floats.Remove(f);
            foreach (var t in textureToDelete)
                _materialProps.Textures.Remove(t);
        }
    }
}