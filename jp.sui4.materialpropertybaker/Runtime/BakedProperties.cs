using System;
using System.Collections.Generic;
using UnityEditor;
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
        
        public void UpdateShaderID()
        {
            _materialProps.UpdateShaderID();
        }

        public void CreatePropsFromMaterial(in Material mat)
        {
            _shaderName = mat.shader.name;
            var colors = new List<MaterialPropColor>();
            var floats = new List<MaterialPropFloat>();
            var ints = new List<MaterialPropInt>();
            for (int pi = 0; pi < mat.shader.GetPropertyCount(); pi++)
            {
                var propType = mat.shader.GetPropertyType(pi);
                var propName = mat.shader.GetPropertyName(pi);

                switch (propType)
                {
                    case ShaderPropertyType.Color:
                        colors.Add(new MaterialPropColor(propName, mat));
                        break; 
                    case ShaderPropertyType.Float:
                        floats.Add(new MaterialPropFloat(propName, mat));
                        break;
                    case ShaderPropertyType.Int:
                        ints.Add(new MaterialPropInt(propName, mat));
                        break;
                    default:
                        break;
                }
            }
            _materialProps = new MaterialProps(colors, floats, ints);
        }

        public void GetCopyProperties(out List<MaterialPropColor> cList, out List<MaterialPropFloat> fList, out List<MaterialPropInt> iList)
        {
            cList = new List<MaterialPropColor>();
            fList = new List<MaterialPropFloat>();
            iList = new List<MaterialPropInt>();
            var mp = _materialProps;
            // 単純にやると、参照渡しになって、変更が同期されてしまうので、一旦コピー
            // Listになってる各MaterialPropがクラスのため、参照になっちゃう
            foreach (var colors in mp.Colors)
            {
                MaterialPropColor c = new MaterialPropColor
                {
                    value = colors.value,
                    property = colors.property,
                    id = colors.id
                };
                cList.Add(c);
            }
            foreach (var floats in mp.Floats)
            {
                MaterialPropFloat f = new MaterialPropFloat
                {
                    value = floats.value,
                    property = floats.property,
                    id = floats.id
                };
                fList.Add(f);
            }
            foreach (var ints in mp.Ints)
            {
                MaterialPropInt i = new MaterialPropInt
                {
                    value = ints.value,
                    property = ints.property,
                    id = ints.id
                };
                iList.Add(i);
            }
        }
        
    }
}