using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    // Unity recorderを参考にした
    [Serializable]
    public class SerializedDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        public ReadOnlyCollection<TKey> Keys { get; }
        [SerializeField] private List<TKey> _keys = new List<TKey>();
        public ReadOnlyCollection<TValue> Values { get; }
        [SerializeField] private List<TValue> _values = new List<TValue>();

        public Dictionary<TKey, TValue> Dictionary => _dictionary;
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        public SerializedDictionary()
        {
            Keys = new ReadOnlyCollection<TKey>(_keys);
            Values = new ReadOnlyCollection<TValue>(_values);
        }

        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();

            foreach (var keyPair in _dictionary)
            {
                _keys.Add(keyPair.Key);
                _values.Add(keyPair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            _dictionary.Clear();

            for (var i = 0; i < _keys.Count; ++i)
                _dictionary.Add(_keys[i], _values[i]);
        }
        

    }

    public static class SerializedDictionaryUtil
    {
        public static (SerializedProperty keyListProp, SerializedProperty valueListProp) GetKeyValueListSerializedProperty(SerializedProperty serializedDictProp)
        {
            return (serializedDictProp.FindPropertyRelative("_keys"), serializedDictProp.FindPropertyRelative("_values"));
        }
        
        public static (SerializedProperty keyProp, SerializedProperty valueProp) GetKeyValueSerializedPropertyAt(int index, SerializedProperty keyListProp, SerializedProperty valueListProp)
        {
            return (keyListProp.GetArrayElementAtIndex(index), valueListProp.GetArrayElementAtIndex(index));
        }
    }
}