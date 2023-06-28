using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace sui4.MaterialPropertyBaker
{
    [Serializable]
    public class MaterialProps
    {
        [SerializeField] private List<MaterialProp<Color>> _colors = new List<MaterialProp<Color>>();
        [SerializeField] private List<MaterialProp<float>> _floats = new List<MaterialProp<float>>();

        public List<MaterialProp<Color>> Colors
        {
            get => _colors;
            set => _colors = value;
        }

        public List<MaterialProp<float>> Floats
        {
            get => _floats;
            set => _floats = value;
        }

        public MaterialProps() { }
        public MaterialProps(List<MaterialProp<Color>> c, List<MaterialProp<float>> f)
        {
            _colors.AddRange(c);
            _floats.AddRange(f);
            UpdateShaderID();
        }
        
        public bool IsEmpty()
        {
            return _colors.Count == 0 && _floats.Count == 0;
        }

        public void UpdateShaderID()
        {
            foreach (var c in _colors)
                c.UpdateShaderID();

            foreach (var f in _floats)
                f.UpdateShaderID();
        }

        public void AddProperty(string propName, ShaderPropertyType spType)
        {
            switch (spType)
            {
                case ShaderPropertyType.Color:
                    _colors.Add(new MaterialProp<Color>(propName));
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    _floats.Add(new MaterialProp<float>(propName));
                    break;
                case ShaderPropertyType.Int:
                case ShaderPropertyType.Vector:
                case ShaderPropertyType.Texture:
                    Debug.LogWarning($"Not supported property type. {spType}");
                    break;
                default:
                    Debug.LogWarning($"Not supported property type. {spType}");
                    break;
            }
        }

        public bool HasProperties(string propName, ShaderPropertyType spType)
        {
            var hasProp = false;
            switch (spType)
            {
                case ShaderPropertyType.Color:
                    // var foundMaterialProp = _colors.FirstOrDefault(materialProp => materialProp.Name == propName);
                    hasProp = _colors.Any(materialProp => materialProp.Name == propName);
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    hasProp = _floats.Any(materialProp => materialProp.Name == propName);
                    break;
                case ShaderPropertyType.Int:
                case ShaderPropertyType.Vector:
                case ShaderPropertyType.Texture:
                    hasProp = false;
                    Debug.LogWarning($"Not supported property type. {spType}");
                    break;
                default:
                    hasProp = false;
                    Debug.LogWarning($"Not supported property type. {spType}");
                    break;
            }

            return hasProp;
        } 

        public void GetCopyProperties(out List<MaterialProp<Color>> cList, out List<MaterialProp<float>> fList)
        {
            cList = new List<MaterialProp<Color>>();
            fList = new List<MaterialProp<float>>();
            // 単純にやると、参照渡しになって、変更が同期されてしまうので、一旦コピー
            // Listになってる各MaterialPropがクラスのため、参照になっちゃう
            foreach (var colors in _colors)
            {
                MaterialProp<Color> c = new MaterialProp<Color>
                {
                    Value = colors.Value,
                    Name = colors.Name,
                    ID = colors.ID
                };
                cList.Add(c);
            }
            foreach (var floats in _floats)
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

        public void CopyValuesFromOther(MaterialProps other)
        {
            other.GetCopyProperties(out var outC, out var outF);
            _colors = outC;
            _floats = outF;
        }
    }
    
    // base class
    [Serializable]
    public class MaterialProp<T>
    {
        [NonSerialized] public int ID;

        [SerializeField] private string _name;
        [SerializeField] private T _value;
        
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                ID = Shader.PropertyToID(_name);
            }
        }
        public T Value
        {
            get => _value;
            set => _value = value;
        }
        
        public MaterialProp()
        {
            Name = "";
        }
        public MaterialProp(string propName)
        {
            Name = propName;
        }
        public MaterialProp(string propName, T value) :this(propName)
        {
            Value = value;
        }
        public MaterialProp(string propName, Material mat) : this(propName)
        {
            Value = typeof(T) switch
            {
                { } t when t == typeof(Color) => (T)Convert.ChangeType(mat.GetColor(ID), typeof(T)),
                { } t when t == typeof(Vector4) => (T)Convert.ChangeType(mat.GetVector(ID), typeof(T)),
                { } t when t == typeof(float) => (T)Convert.ChangeType(mat.GetFloat(ID), typeof(T)),
                { } t when t == typeof(Texture) => (T)Convert.ChangeType(mat.GetTexture(ID), typeof(T)),
                _ => Value
            };
        }

        public void UpdateShaderID()
        {
            ID = Shader.PropertyToID(_name);
        }
    }

}