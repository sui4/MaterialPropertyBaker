using System;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(MaterialPropertyConfig))]
    public class MaterialPropertyConfigEditor: Editor
    {
        private SerializedProperty _shaderName;
        private SerializedProperty _propertyNames;
        private SerializedProperty _propertyTypes;
        private SerializedProperty _editable;

        private SerializedProperty _propertyName;
        private SerializedProperty _propertyType;
        private SerializedProperty _propertyEditable;

        private Vector2 _scrollPos = Vector2.zero;
        
        private string _filterQuery = "";
        private void OnEnable()
        {
            if (target == null)
                return;
            _shaderName = serializedObject.FindProperty("_shaderName");
            _propertyNames = serializedObject.FindProperty("_propertyNames");
            _propertyTypes = serializedObject.FindProperty("_propertyTypes");
            _editable = serializedObject.FindProperty("_editable");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var sp = (MaterialPropertyConfig)target;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_shaderName, new GUIContent("Shader Name"));
            }
            
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                _filterQuery = EditorGUILayout.TextField("Filter by name",_filterQuery);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Editable", GUILayout.Width(60));
                    EditorGUILayout.LabelField("Properties");
                    EditorGUILayout.LabelField("Types");
                }
                using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scrollPos))
                {
                    _scrollPos = scrollScope.scrollPosition; 
                    for (int pi = 0; pi < _propertyNames.arraySize; pi++)
                    {
                        if (string.IsNullOrEmpty(_filterQuery) || 
                            sp.PropertyNames[pi].IndexOf(_filterQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _propertyName = _propertyNames.GetArrayElementAtIndex(pi);
                            _propertyType = _propertyTypes.GetArrayElementAtIndex(pi);
                            _propertyEditable = _editable.GetArrayElementAtIndex(pi);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.PropertyField(_propertyEditable, new GUIContent(), GUILayout.Width(60));
                                EditorGUILayout.LabelField(_propertyName.stringValue);
                                using (new EditorGUI.DisabledScope(true))
                                {
                                    EditorGUILayout.PropertyField(_propertyType, new GUIContent());
                                }
                            }
                        }
                    }
                }

                if (change.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

        }
    }
}