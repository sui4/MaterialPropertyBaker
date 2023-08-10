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
        [SerializeField] private List<MaterialProp<Color>> _colors = new();
        [SerializeField] private List<MaterialProp<float>> _floats = new();
        [SerializeField] private List<MaterialProp<Texture>> _textures = new();

        public MaterialProps()
        {
        }

        public MaterialProps(List<MaterialProp<Color>> c, List<MaterialProp<float>> f, List<MaterialProp<Texture>> t)
        {
            Colors.AddRange(c);
            Floats.AddRange(f);
            Textures.AddRange(t);
            UpdateShaderIDs();
        }

        public MaterialProps(in Material mat)
        {
            for (var pi = 0; pi < mat.shader.GetPropertyCount(); pi++)
            {
                var propType = mat.shader.GetPropertyType(pi);
                var propName = mat.shader.GetPropertyName(pi);

                switch (propType)
                {
                    case ShaderPropertyType.Color:
                        var colorValue = mat.GetColor(propName);
                        Colors.Add(new MaterialProp<Color>(propName, colorValue));
                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        var floatValue = mat.GetFloat(propName);
                        Floats.Add(new MaterialProp<float>(propName, floatValue));
                        break;
                    case ShaderPropertyType.Texture:
                        var texValue = mat.GetTexture(propName);
                        Textures.Add(new MaterialProp<Texture>(propName, texValue));
                        break;
                    case ShaderPropertyType.Int:
                    case ShaderPropertyType.Vector:
                        break;
                }
            }
        }

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

        public List<MaterialProp<Texture>> Textures
        {
            get => _textures;
            set => _textures = value;
        }

        public bool IsEmpty()
        {
            return Colors.Count == 0 && Floats.Count == 0 && Textures.Count == 0;
        }

        public void UpdateShaderIDs()
        {
            foreach (var c in Colors)
                c.UpdateShaderID();

            foreach (var f in Floats)
                f.UpdateShaderID();

            foreach (var t in Textures)
                t.UpdateShaderID();
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
                case ShaderPropertyType.Texture:
                    _textures.Add(new MaterialProp<Texture>(propName));
                    break;
                case ShaderPropertyType.Int:
                case ShaderPropertyType.Vector:
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
                    hasProp = Colors.Any(materialProp => materialProp.Name == propName);
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    hasProp = Floats.Any(materialProp => materialProp.Name == propName);
                    break;
                case ShaderPropertyType.Texture:
                    hasProp = Textures.Any(materialProp => materialProp.Name == propName);
                    break;
                case ShaderPropertyType.Int:
                case ShaderPropertyType.Vector:
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

        public void GetCopyProperties(out List<MaterialProp<Color>> cList, out List<MaterialProp<float>> fList,
            out List<MaterialProp<Texture>> tList)
        {
            cList = new List<MaterialProp<Color>>();
            fList = new List<MaterialProp<float>>();
            tList = new List<MaterialProp<Texture>>();
            // 単純にやると、参照渡しになって、変更が同期されてしまうので、一旦コピー
            // Listになってる各MaterialPropがクラスのため、参照になっちゃう
            foreach (var colors in Colors)
            {
                var c = new MaterialProp<Color>
                {
                    Value = colors.Value,
                    Name = colors.Name,
                    ID = colors.ID
                };
                cList.Add(c);
            }

            foreach (var floats in Floats)
            {
                var f = new MaterialProp<float>
                {
                    Value = floats.Value,
                    Name = floats.Name,
                    ID = floats.ID
                };
                fList.Add(f);
            }

            foreach (var tex in Textures)
            {
                var t = new MaterialProp<Texture>
                {
                    Value = tex.Value,
                    Name = tex.Name,
                    ID = tex.ID
                };
                tList.Add(t);
            }
        }

        public void CopyValuesFromOther(in MaterialProps other)
        {
            other.GetCopyProperties(out var outC, out var outF, out var outT);
            Colors = outC;
            Floats = outF;
            Textures = outT;
        }
    }

    // base class
    [Serializable]
    public class MaterialProp<T>
    {
        [SerializeField] private string _name;
        [SerializeField] private T _value;
        [NonSerialized] public int ID;

        public MaterialProp()
        {
            Name = "";
        }

        public MaterialProp(string propName)
        {
            Name = propName;
            UpdateShaderID();
        }

        public MaterialProp(string propName, T value) : this(propName)
        {
            Value = value;
        }

        // public MaterialProp(string propName, Material mat) : this(propName)
        // {
        //     Value = typeof(T) switch
        //     {
        //         { } t when t == typeof(Color) => (T)Convert.ChangeType(mat.GetColor(ID), typeof(T)),
        //         { } t when t == typeof(Vector4) => (T)Convert.ChangeType(mat.GetVector(ID), typeof(T)),
        //         { } t when t == typeof(float) => (T)Convert.ChangeType(mat.GetFloat(ID), typeof(T)),
        //         { } t when t == typeof(Texture) => (T)Convert.ChangeType(mat.GetTexture(ID), typeof(T)), // textureはIConvertibleじゃないので、Convert.ChangeTypeできない
        //         _ => Value
        //     };
        // }

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

        public void UpdateShaderID()
        {
            ID = Shader.PropertyToID(_name);
        }
    }
}