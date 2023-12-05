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
        [SerializeField] private List<MaterialProp<int>> _ints = new();

        public MaterialProps()
        {
        }

        public MaterialProps(List<MaterialProp<Color>> c, List<MaterialProp<float>> f, List<MaterialProp<int>> i)
        {
            Colors = new List<MaterialProp<Color>>(c);
            Floats = new List<MaterialProp<float>>(f);
            Ints = new List<MaterialProp<int>>(i);
            UpdateShaderID();
        }

        public MaterialProps(Material mat, bool loadValue = true)
        {
            Shader = mat.shader;
            Material = mat;
            ID = mat.name;
            if (loadValue)
            {
                for (var pi = 0; pi < mat.shader.GetPropertyCount(); pi++)
                {
                    ShaderPropertyType propType = mat.shader.GetPropertyType(pi);
                    if (!IsSupportedType(propType)) continue;
                    string propName = mat.shader.GetPropertyName(pi);

                    switch (propType)
                    {
                        case ShaderPropertyType.Color:
                            Color c = mat.GetColor(propName);
                            Colors.Add(new MaterialProp<Color>(propName, c));
                            break;
                        case ShaderPropertyType.Float:
                        case ShaderPropertyType.Range:
                            float f = mat.GetFloat(propName);
                            Floats.Add(new MaterialProp<float>(propName, f));
                            break;
                        case ShaderPropertyType.Int:
                            int i = mat.GetInteger(propName);
                            Ints.Add(new MaterialProp<int>(propName, i));
                            break;
                        case ShaderPropertyType.Vector:
                        case ShaderPropertyType.Texture:
                        default:
                            break;
                    }
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
        
        public List<MaterialProp<int>> Ints
        {
            get => _ints;
            set => _ints = value;
        }

        public bool IsEmpty()
        {
            return Colors.Count == 0 && Floats.Count == 0 && Ints.Count == 0;
        }

        public static bool IsSupportedType(ShaderPropertyType type)
        {
            return type switch
            {
                ShaderPropertyType.Color => true,
                ShaderPropertyType.Float => true,
                ShaderPropertyType.Range => true,
                ShaderPropertyType.Int => true,
                ShaderPropertyType.Vector => false,
                ShaderPropertyType.Texture => false,
                _ => false
            };
        }

        public void UpdateShaderID()
        {
            foreach (MaterialProp<Color> c in Colors)
                c.UpdateShaderID();

            foreach (MaterialProp<float> f in Floats)
                f.UpdateShaderID();
            
            foreach (MaterialProp<int> i in Ints)
                i.UpdateShaderID();
        }

        // add property with default value
        public void AddProperty(string propName, ShaderPropertyType spType)
        {
            if (HasProperty(propName, spType)) return;
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
                    Ints.Add(new MaterialProp<int>(propName));
                    break;
                case ShaderPropertyType.Vector:
                case ShaderPropertyType.Texture:
                default:
                    Debug.LogWarning($"Not supported property type. {spType}");
                    break;
            }
        }

        public void SetFloat(string propName, float value)
        {
            if (HasProperty(propName, ShaderPropertyType.Float))
            {
                UpdateProperty(Floats, propName, value);
            }
            else
            {
                Floats.Add(new MaterialProp<float>(propName, value));
            }
        }

        public void SetColor(string propName, Color value)
        {
            if (HasProperty(propName, ShaderPropertyType.Color))
            {
                UpdateProperty(Colors, propName, value);
            }
            else
            {
                Colors.Add(new MaterialProp<Color>(propName, value));
            }
        }
        
        public void SetInt(string propName, int value)
        {
            if (HasProperty(propName, ShaderPropertyType.Int))
            {
                UpdateProperty(Ints, propName, value);
            }
            else
            {
                Ints.Add(new MaterialProp<int>(propName, value));
            }
        }

        private void UpdateProperty<T>(List<MaterialProp<T>> props, string propName, T value)
        {
            MaterialProp<T> prop = props.FirstOrDefault(p => p.Name == propName);
            if (prop != null)
            {
                prop.Value = value;
            }
        }


        public bool HasProperty(string propName, ShaderPropertyType spType)
        {
            if (IsSupportedType(spType))
            {
                return spType switch
                {
                    ShaderPropertyType.Color => Colors.Any(materialProp => materialProp.Name == propName),
                    ShaderPropertyType.Float => Floats.Any(materialProp => materialProp.Name == propName),
                    ShaderPropertyType.Range => Floats.Any(materialProp => materialProp.Name == propName),
                    ShaderPropertyType.Int => Ints.Any(materialProp => materialProp.Name == propName),
                    ShaderPropertyType.Vector => false,
                    ShaderPropertyType.Texture => false,
                    _ => false
                };
            }
            else
            {
                Debug.LogWarning($"{spType.ToString()} is not supported type.");
                return false;
            }
        }

        public void GetCopyProperties(out List<MaterialProp<Color>> cList, out List<MaterialProp<float>> fList, out List<MaterialProp<int>> iList)
        {
            // Listになってる各MaterialPropがクラスのため、単純にやると参照渡しになって、変更が同期されてしまう
            CopyProperties(Colors, out cList);
            CopyProperties(Floats, out fList);
            CopyProperties(Ints, out iList);
        }

        private void CopyProperties<T>(in List<MaterialProp<T>> baseProps, out List<MaterialProp<T>> targetProps)
        {
            targetProps = new List<MaterialProp<T>>();
            foreach (MaterialProp<T> baseProp in baseProps)
            {
                var prop = new MaterialProp<T>
                {
                    Value = baseProp.Value,
                    Name = baseProp.Name,
                    ID = baseProp.ID
                };
                targetProps.Add(prop);
            }
        }

        public void CopyValuesFromOther(in MaterialProps other)
        {
            other.GetCopyProperties(
                out List<MaterialProp<Color>> outC,
                out List<MaterialProp<float>> outF,
                out List<MaterialProp<int>> outI);
            Colors = outC;
            Floats = outF;
            Ints = outI;
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