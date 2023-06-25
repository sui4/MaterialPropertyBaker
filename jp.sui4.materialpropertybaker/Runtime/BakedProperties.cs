using System;
using System.Collections.Generic;
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
        public string ShaderName
        {
            get => _shaderName;
            set => _shaderName = value;
        }
        public MaterialProps MaterialProps => _materialProps;

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

        public void GetCopyProperties(out List<MaterialProp<Color>> cList, out List<MaterialProp<float>> fList)
        {
            cList = new List<MaterialProp<Color>>();
            fList = new List<MaterialProp<float>>();
            var mp = _materialProps;
            // 単純にやると、参照渡しになって、変更が同期されてしまうので、一旦コピー
            // Listになってる各MaterialPropがクラスのため、参照になっちゃう
            foreach (var colors in mp.Colors)
            {
                MaterialProp<Color> c = new MaterialProp<Color>
                {
                    Value = colors.Value,
                    Name = colors.Name,
                    ID = colors.ID
                };
                cList.Add(c);
            }
            foreach (var floats in mp.Floats)
            {
                MaterialProp<float> f = new MaterialProp<float>
                {
                    Value = floats.Value,
                    Name = floats.Name,
                    ID = floats.ID
                };
                fList.Add(f);
            }

        }
        
    }
}