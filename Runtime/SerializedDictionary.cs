using System;
using System.Collections.Generic;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    // Unity recorderを参考にした
    [Serializable]
    public class SerializedDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> _keys = new List<TKey>();
        [SerializeField] private List<TValue> _values = new List<TValue>();

        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        public Dictionary<TKey, TValue> Dictionary => _dictionary;

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
}