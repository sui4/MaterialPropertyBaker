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
        [SerializeField] private string _id;
        [SerializeField] private Shader _shader;
        [SerializeField] private Material _material;
        [SerializeField] private List<MaterialProp<Color>> _colors = new();
        [SerializeField] private List<MaterialProp<float>> _floats = new();

        public MaterialProps()
        {
        }

        public MaterialProps(List<MaterialProp<Color>> c, List<MaterialProp<float>> f)
        {
            Colors.AddRange(c);
            Floats.AddRange(f);
            UpdateShaderID();
        }

        public MaterialProps(Material mat, bool loadValue = true)
        {
            this.Shader = mat.shader;
            this.Material = mat;
            ID = mat.name;
            if (loadValue)
            {
                for (var pi = 0; pi < mat.shader.GetPropertyCount(); pi++)
                {
                    var propType = mat.shader.GetPropertyType(pi);
                    if (!IsSupportedType(propType)) continue;
                    var propName = mat.shader.GetPropertyName(pi);

                    switch (propType)
                    {
                        case ShaderPropertyType.Color:
                            var c = mat.GetColor(propName);
                            Colors.Add(new MaterialProp<Color>(propName, c));
                            break;
                        case ShaderPropertyType.Float:
                        case ShaderPropertyType.Range:
                            var f = mat.GetFloat(propName);
                            Floats.Add(new MaterialProp<float>(propName, f));
                            break;
                        case ShaderPropertyType.Int:
                        case ShaderPropertyType.Vector:
                        case ShaderPropertyType.Texture:
                        default:
                            break;
                    }
                }
            }
        }

        public MaterialProps(Material mat, MaterialPropertyConfig config)
        {
            for (var pi = 0; pi < mat.shader.GetPropertyCount(); pi++)
            {
                var propType = mat.shader.GetPropertyType(pi);
                if (!IsSupportedType(propType)) continue;
                var propName = mat.shader.GetPropertyName(pi);
                if (!config.HasEditableProperty(propName)) continue;

                switch (propType)
                {
                    case ShaderPropertyType.Color:
                        var c = mat.GetColor(propName);
                        Colors.Add(new MaterialProp<Color>(propName, c));
                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        var f = mat.GetFloat(propName);
                        Floats.Add(new MaterialProp<float>(propName, f));
                        break;
                    case ShaderPropertyType.Int:
                    case ShaderPropertyType.Vector:
                    case ShaderPropertyType.Texture:
                    default:
                        break;
                }
            }
        }

        public string ID
        {
            get => _id;
            set => _id = value;
        }
        
        public Shader Shader
        {
            get => _shader;
            set => _shader = value;
        }

        public Material Material
        {
            get => _material;
            set => _material = value;
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

        public bool IsEmpty()
        {
            return Colors.Count == 0 && Floats.Count == 0;
        }

        public static bool IsSupportedType(ShaderPropertyType type)
        {
            return type switch
            {
                ShaderPropertyType.Color => true,
                ShaderPropertyType.Float => true,
                ShaderPropertyType.Range => true,
                ShaderPropertyType.Int => false,
                ShaderPropertyType.Vector => false,
                ShaderPropertyType.Texture => false,
                _ => false
            };
        }

        public void UpdateShaderID()
        {
            foreach (var c in Colors)
                c.UpdateShaderID();

            foreach (var f in Floats)
                f.UpdateShaderID();
        }

        public void AddProperty(string propName, ShaderPropertyType spType)
        {
            switch (spType)
            {
                case ShaderPropertyType.Color:
                    Colors.Add(new MaterialProp<Color>(propName));
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    Floats.Add(new MaterialProp<float>(propName));
                    break;
                case ShaderPropertyType.Int:
                case ShaderPropertyType.Vector:
                case ShaderPropertyType.Texture:
                default:
                    Debug.LogWarning($"Not supported property type. {spType}");
                    break;
            }
        }

        public bool HasProperties(string propName, ShaderPropertyType spType)
        {
            if (!IsSupportedType(spType))
            {
                Debug.LogWarning($"{spType.ToString()} is not supported type.");
                return false;
            }

            return spType switch
            {
                ShaderPropertyType.Color => Colors.Any(materialProp => materialProp.Name == propName),
                ShaderPropertyType.Float => Floats.Any(materialProp => materialProp.Name == propName),
                ShaderPropertyType.Range => Floats.Any(materialProp => materialProp.Name == propName),
                ShaderPropertyType.Int => false,
                ShaderPropertyType.Vector => false,
                ShaderPropertyType.Texture => false,
                _ => false
            };
        }

        public void GetCopyProperties(out List<MaterialProp<Color>> cList, out List<MaterialProp<float>> fList)
        {
            cList = new List<MaterialProp<Color>>();
            fList = new List<MaterialProp<float>>();
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
        }

        public void CopyValuesFromOther(in MaterialProps other)
        {
            other.GetCopyProperties(out var outC, out var outF);
            Colors = outC;
            Floats = outF;
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

        // TextureがIConvertibleを実装していないので、以下のコードは使えない
        // public MaterialProp(string propName, Material mat) : this(propName)
        // {
        //     Value = typeof(T) switch
        //     {
        //         { } t when t == typeof(Color) => (T)Convert.ChangeType(mat.GetColor(ID), typeof(T)),
        //         { } t when t == typeof(Vector4) => (T)Convert.ChangeType(mat.GetVector(ID), typeof(T)),
        //         { } t when t == typeof(float) => (T)Convert.ChangeType(mat.GetFloat(ID), typeof(T)),
        //         { } t when t == typeof(Texture) => (T)Convert.ChangeType(mat.GetTexture(ID), typeof(T)),
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