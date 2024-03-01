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
        [SerializeField] private List<MaterialProp<Texture>> _textures = new();

        public MaterialProps()
        {
        }

        public MaterialProps(List<MaterialProp<Color>> c, List<MaterialProp<float>> f, List<MaterialProp<int>> i, List<MaterialProp<Texture>> t)
        {
            CopyProperties(c, out List<MaterialProp<Color>> deepCopiedColors);
            CopyProperties(f, out List<MaterialProp<float>> deepCopiedFloats);
            CopyProperties(i, out List<MaterialProp<int>> deepCopiedInts);
            CopyProperties(t, out List<MaterialProp<Texture>> deepCopiedTextures);
            _colors = deepCopiedColors;
            _floats = deepCopiedFloats;
            _ints = deepCopiedInts;
            _textures = deepCopiedTextures;
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
                            break;
                        case ShaderPropertyType.Texture:
                            Texture t = mat.GetTexture(propName);
                            Textures.Add(new MaterialProp<Texture>(propName, t));
                            break;
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

        public List<MaterialProp<Color>> Colors => _colors;

        public List<MaterialProp<float>> Floats => _floats;

        public List<MaterialProp<int>> Ints => _ints;
        
        public List<MaterialProp<Texture>> Textures => _textures;

        public bool IsEmpty()
        {
            return Colors.Count == 0 && Floats.Count == 0 && Ints.Count == 0 && Textures.Count == 0;
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
                ShaderPropertyType.Texture => true,
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
            
            foreach (MaterialProp<Texture> t in Textures)
                t.UpdateShaderID();
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
                    break;
                case ShaderPropertyType.Texture:
                    Textures.Add(new MaterialProp<Texture>(propName));
                    break;
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
        
        public void SetTexture(string propName, Texture value)
        {
            if (HasProperty(propName, ShaderPropertyType.Texture))
            {
                UpdateProperty(Textures, propName, value);
            }
            else
            {
                Textures.Add(new MaterialProp<Texture>(propName, value));
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
                    ShaderPropertyType.Texture => Textures.Any(materialProp => materialProp.Name == propName),
                    _ => false
                };
            }
            else
            {
                Debug.LogWarning($"{spType.ToString()} is not supported type.");
                return false;
            }
        }

        public void GetCopyProperties(out List<MaterialProp<Color>> cList, out List<MaterialProp<float>> fList, out List<MaterialProp<int>> iList, out List<MaterialProp<Texture>> tList)
        {
            CopyProperties(Colors, out cList);
            CopyProperties(Floats, out fList);
            CopyProperties(Ints, out iList);
            CopyProperties(Textures, out tList);
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
                out List<MaterialProp<int>> outI,
                out List<MaterialProp<Texture>> outT);
            _colors = outC;
            _floats = outF;
            _ints = outI;
            _textures = outT;
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