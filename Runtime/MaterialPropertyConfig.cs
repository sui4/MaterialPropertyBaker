using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace sui4.MaterialPropertyBaker
{
    [Serializable]
    // Material Propsで扱うプロパティをユーザが選択的に追加できるように、shaderのプロパティを保持するクラス
    public class MaterialPropertyConfig: ScriptableObject
    {
        [SerializeField] private string _shaderName;
        [SerializeField] private List<string> _propertyNames = new List<string>();
        [SerializeField] private List<ShaderPropertyType> _propertyTypes = new List<ShaderPropertyType>();
        [SerializeField] private List<bool> _editable = new List<bool>();

        public string ShaderName
        {
            get => _shaderName;
            set => _shaderName = value;
        }
        public List<string> PropertyNames => _propertyNames;
        public List<ShaderPropertyType> PropertyTypes => _propertyTypes;
        public List<bool> Editable => _editable;

        public bool HasEditableProperty(string propName)
        {
            var index = PropertyNames.IndexOf(propName);
            if (index < 0)
                return false;
            return Editable[index];
        }
        
        public void LoadProperties(Material mat)
        {
            if(mat == null)
            {
                Debug.LogError("Material is null");
                return;
            }
            var shader = mat.shader;
            if (shader == null)
            {
                Debug.LogError($"Shader of Material`{mat.name}` not found");
                return;
            }
            LoadProperties(shader);
        }

        public void LoadProperties(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if(shader == null)
            {
                Debug.LogError("Shader is null");
                return;
            }

            LoadProperties(shader);
        }

        public void LoadProperties(Shader shader)
        {
            _propertyNames.Clear();
            _propertyTypes.Clear();
            _editable.Clear();
            _shaderName = shader.name;
            for (int pi = 0; pi < shader.GetPropertyCount(); pi++)
            {
                var propType = shader.GetPropertyType(pi);
                var propName = shader.GetPropertyName(pi);
                _propertyNames.Add(propName);
                _propertyTypes.Add(propType);
                _editable.Add(false);
            }
            Debug.Log($"{_shaderName} has {_propertyNames.Count} properties");
        }
        
    }
}