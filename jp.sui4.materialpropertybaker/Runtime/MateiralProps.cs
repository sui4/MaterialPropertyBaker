using System;
using System.Collections.Generic;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [Serializable]
    public class MaterialProps
    {
        [SerializeField] private List<MaterialPropColor> _colors = new List<MaterialPropColor>();
        [SerializeField] private List<MaterialPropFloat> _floats = new List<MaterialPropFloat>();
        [SerializeField] private List<MaterialPropInt> _ints = new List<MaterialPropInt>();

        public List<MaterialPropColor> Colors
        {
            get => _colors;
            set => _colors = value;
        }

        public List<MaterialPropFloat> Floats
        {
            get => _floats;
            set => _floats = value;
        }

        public List<MaterialPropInt> Ints
        {
            get => _ints;
            set => _ints = value;
        }
        public MaterialProps() { }
        public MaterialProps(List<MaterialPropColor> c, List<MaterialPropFloat> f, List<MaterialPropInt> i)
        {
            _colors.AddRange(c);
            _floats.AddRange(f);
            _ints.AddRange(i);
            UpdateShaderID();
        }

        public void UpdateShaderID()
        {
            foreach (var c in _colors)
            {
                c.UpdateProperty(c.property);
            }
            foreach (var f in _floats)
            {
                f.UpdateProperty(f.property);
            }
            foreach (var i in _ints)
            {
                i.UpdateProperty(i.property);
            }
        }
        
        public void GetCopyProperties(out List<MaterialPropColor> cList, out List<MaterialPropFloat> fList, out List<MaterialPropInt> iList)
        {
            cList = new List<MaterialPropColor>();
            fList = new List<MaterialPropFloat>();
            iList = new List<MaterialPropInt>();
            // 単純にやると、参照渡しになって、変更が同期されてしまうので、一旦コピー
            // Listになってる各MaterialPropがクラスのため、参照になっちゃう
            foreach (var colors in _colors)
            {
                MaterialPropColor c = new MaterialPropColor
                {
                    value = colors.value,
                    property = colors.property,
                    id = colors.id
                };
                cList.Add(c);
            }
            foreach (var floats in _floats)
            {
                MaterialPropFloat f = new MaterialPropFloat
                {
                    value = floats.value,
                    property = floats.property,
                    id = floats.id
                };
                fList.Add(f);
            }
            foreach (var ints in _ints)
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
    
    // base class
    [Serializable]
    public class MaterialProp
    {
        [NonSerialized] public int id;

        public string property;
        public MaterialProp()
        {
            property = "propName";
            id = Shader.PropertyToID(property);
        }
        public MaterialProp(string propName)
        {
            property = propName;
            id = Shader.PropertyToID(property);
        }
        public virtual void GetProperty(Material mat){}

        public virtual void ApplyProperty(Material mat){}

        public void UpdateProperty(string propName)
        {
            property = propName;
            id = Shader.PropertyToID(propName);
        }
    }

    #region VariableType
    [Serializable]
    public class MaterialPropColor : MaterialProp
    {
        [SerializeField, ColorUsage(false, true)] public Color value = Color.black;
        
        public MaterialPropColor(): base() { }
        public MaterialPropColor(string propName) : base(propName)
        {
            value = Color.black;
        }
        public MaterialPropColor(string propName, Color color) : base(propName)
        {
            value = color;
        }
        
        public MaterialPropColor(string propName, Material mat) : base(propName)
        {
            value = mat.GetColor(id);
        }
        
        public override void GetProperty(Material mat)
        {
            value  = mat.GetColor(id);
        }
    
        public override void ApplyProperty(Material mat)
        {
            mat.SetColor(id, value);
        }
    }
    [Serializable]
    public class MaterialPropFloat : MaterialProp
    {
        public float value = 0f;
        
        public MaterialPropFloat(): base() { }
        public MaterialPropFloat(string propName) :base(propName) { }

        public MaterialPropFloat(string propName, float v) : base(propName)
        {
            value = v;
        }
        
        public MaterialPropFloat(string propName, Material mat) : base(propName)
        {
            value = mat.GetFloat(id);
        }

        public override void GetProperty(Material mat)
        {
            value  = mat.GetFloat(id);
        }
    
        public override void ApplyProperty(Material mat)
        {
            mat.SetFloat(id, value);
        }
    }
    
    [Serializable]
    public class MaterialPropInt : MaterialProp
    {
        public int value = 0;
        
        public MaterialPropInt(): base() { }
        public MaterialPropInt(string propName) :base(propName) { }

        public MaterialPropInt(string propName, int v) : base(propName)
        {
            value = id;
        }
        
        public MaterialPropInt(string propName, Material mat) : base(propName)
        {
            value = mat.GetInt(id);
        }

        public override void GetProperty(Material mat)
        {
            value  = mat.GetInt(id);
        }

        public override void ApplyProperty(Material mat)
        {
            mat.SetInt(id, value);
        }
    }
    
    #endregion // VariableType
}