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
        [SerializeField] private MaterialPropertyConfig _config;

        public string ShaderName
        {
            get => _shaderName;
            set => _shaderName = value;
        }

        public MaterialProps MaterialProps => _materialProps;
        
        public MaterialPropertyConfig Config
        {
            get => _config;
            set => _config = value;
        }

        private void OnEnable()
        {
            if (_materialProps == null)
                _materialProps = new MaterialProps();
            UpdateShaderID();
        }

        public void UpdateShaderID()
        {
            if (_materialProps != null)
                _materialProps.UpdateShaderID();
        }

        public void CreatePropsFrom(in Material mat)
        {
            _shaderName = mat.shader.name;
            _materialProps = new MaterialProps(mat);
            UpdateShaderID();
        }

        public void CreatePropsFrom(in Material mat, in MaterialPropertyConfig config)
        {
            _shaderName = mat.shader.name;
            _materialProps = new MaterialProps(mat, config);
            UpdateShaderID();
        }
        public void CreatePropsFrom(in MaterialProps matProps)
        {
            _materialProps ??= new MaterialProps();
            _materialProps.CopyValuesFromOther(matProps);
        }

        public void GetCopyProperties(out List<MaterialProp<Color>> cList, out List<MaterialProp<float>> fList)
        {
            MaterialProps.GetCopyProperties(out cList, out fList);
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

        public void SyncPropertyWithConfig(MaterialPropertyConfig config)
        {
            DeleteUnEditableProperties(config);

            for (var pi = 0; pi < config.PropertyNames.Count; pi++)
            {
                var editable = config.Editable[pi];
                var propType = config.PropertyTypes[pi];
                if(!editable || !MaterialProps.IsSupportedType(propType)) continue;
                var propName = config.PropertyNames[pi];
                if (!MaterialProps.HasProperties(propName, propType))
                {
                    _materialProps.AddProperty(propName, propType);
                }
            }
        }
    }
}