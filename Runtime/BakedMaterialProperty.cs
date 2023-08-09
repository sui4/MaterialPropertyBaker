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

        public void GetCopyProperties(out List<MaterialProp<Color>> cList, out List<MaterialProp<float>> fList, out List<MaterialProp<Texture>> tList)
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

            var colorDeleteIndex = new List<int>();
            var floatDeleteIndex = new List<int>();

            // 含まれないプロパティを探す
            for (var ci = 0; ci < _materialProps.Colors.Count; ci++)
            {
                var colorProp = _materialProps.Colors[ci];
                var index = config.PropertyNames.IndexOf(colorProp.Name);
                if (index == -1)
                    colorDeleteIndex.Add(ci);
                else if (config.Editable[index] == false) colorDeleteIndex.Add(ci);
            }

            for (var fi = 0; fi < _materialProps.Floats.Count; fi++)
            {
                var floatProp = _materialProps.Floats[fi];
                var index = config.PropertyNames.IndexOf(floatProp.Name);
                if (index == -1)
                    floatDeleteIndex.Add(fi);
                else if (config.Editable[index] == false) floatDeleteIndex.Add(fi);
            }

            // 後ろから削除
            for (var di = colorDeleteIndex.Count - 1; di >= 0; di--)
                _materialProps.Colors.RemoveAt(colorDeleteIndex[di]);
            for (var di = floatDeleteIndex.Count - 1; di >= 0; di--)
                _materialProps.Floats.RemoveAt(floatDeleteIndex[di]);
        }
    }
}