using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace sui4.MaterialPropertyBaker
{
    [Serializable]
    [CreateAssetMenu(menuName = "MaterialPropertyBaker/BakedProperties")]
    public class BakedProperties: ScriptableObject
    {
        [SerializeField] private MaterialProps _materialProps;
        [SerializeField] private string _shaderName;
        [SerializeField] private ShaderProperties _shaderProperties;
        public string ShaderName
        {
            get => _shaderName;
            set => _shaderName = value;
        }
        public MaterialProps MaterialProps => _materialProps;

        public ShaderProperties ShaderProperties
        {
            get => _shaderProperties;
            set => _shaderProperties = value;
        }

        private void OnEnable()
        {
            UpdateShaderID();
        }

        public void UpdateShaderID()
        {
            if(_materialProps != null)
                _materialProps.UpdateShaderID();
        }

        public void CreatePropsFromMaterial(in Material mat)
        {
            _shaderName = mat.shader.name;
            var colors = new List<MaterialProp<Color>>();
            var floats = new List<MaterialProp<float>>();
            for (int pi = 0; pi < mat.shader.GetPropertyCount(); pi++)
            {
                var propType = mat.shader.GetPropertyType(pi);
                var propName = mat.shader.GetPropertyName(pi);

                switch (propType)
                {
                    case ShaderPropertyType.Color:
                        colors.Add(new MaterialProp<Color>(propName, mat));
                        break; 
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        floats.Add(new MaterialProp<float>(propName, mat));
                        break;
                    default:
                        break;
                }
            }
            _materialProps = new MaterialProps(colors, floats);
            UpdateShaderID();
        }

        public void CraetePropsFromMaterialProps(MaterialProps matProps)
        {
            matProps.GetCopyProperties(out var cList, out var fList);
            _materialProps = new MaterialProps(cList, fList);
        }
        public void GetCopyProperties(out List<MaterialProp<Color>> cList, out List<MaterialProp<float>> fList)
        {
            MaterialProps.GetCopyProperties(out cList, out fList);
        }

        // shader propertiesに含まれない, またはEditableではないプロパティを削除する
        public void DeleteUnEditableProperties()
        {
            if(_shaderProperties == null) return;

            var colorDeleteIndex = new List<int>();
            var floatDeleteIndex = new List<int>();

            // 含まれないプロパティを探す
            for (int ci = 0; ci < _materialProps.Colors.Count; ci++)
            {
                var colorProp = _materialProps.Colors[ci];
                var index = _shaderProperties.PropertyNames.IndexOf(colorProp.Name);
                if (index == -1)
                {
                    colorDeleteIndex.Add(ci);
                }
                else if(_shaderProperties.Editable[index] == false)
                {
                    colorDeleteIndex.Add(ci);
                }
            }
            
            for (int fi = 0; fi < _materialProps.Floats.Count; fi++)
            {
                var floatProp = _materialProps.Floats[fi];
                var index = _shaderProperties.PropertyNames.IndexOf(floatProp.Name);
                if (index == -1)
                {
                    floatDeleteIndex.Add(fi);
                }
                else if(_shaderProperties.Editable[index] == false)
                {
                    floatDeleteIndex.Add(fi);
                }
            }
            
            // 後ろから削除
            for(int di = colorDeleteIndex.Count - 1; di >= 0; di--)
            {
                _materialProps.Colors.RemoveAt(colorDeleteIndex[di]);
            }
            for(int di = floatDeleteIndex.Count - 1; di >= 0; di--)
            {
                _materialProps.Floats.RemoveAt(floatDeleteIndex[di]);
            }
        }
    }
}