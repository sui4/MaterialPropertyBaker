using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(MaterialPropertyConfig))]
    public class MaterialPropertyConfigEditor : Editor
    {
        private SerializedProperty _shaderNameProp;
        private SerializedProperty _propertyNamesProp;
        private SerializedProperty _propertyTypesProp;
        private SerializedProperty _editableProp;

        private SerializedProperty _propertyNameProp;
        private SerializedProperty _propertyTypeProp;
        private SerializedProperty _propertyEditableProp;

        private Vector2 _scrollPos = Vector2.zero;

        private string _filterQuery = "";

        private void OnEnable()
        {
            if (target == null)
                return;
            _shaderNameProp = serializedObject.FindProperty("_shaderName");
            _propertyNamesProp = serializedObject.FindProperty("_propertyNames");
            _propertyTypesProp = serializedObject.FindProperty("_propertyTypes");
            _editableProp = serializedObject.FindProperty("_editable");
        }

        public override void OnInspectorGUI()
        {
            if (target == null) return;

            serializedObject.Update();
            var sp = (MaterialPropertyConfig)target;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_shaderNameProp, new GUIContent("Shader Name"));
            }
            _filterQuery = EditorGUILayout.TextField("Filter by name", _filterQuery);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Editable", GUILayout.Width(60));
                EditorGUILayout.LabelField("Properties");
                EditorGUILayout.LabelField("Types");
            }

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scrollPos))
                {
                    _scrollPos = scrollScope.scrollPosition;
                    for (var pi = 0; pi < _propertyNamesProp.arraySize; pi++)
                    {
                        if (string.IsNullOrEmpty(_filterQuery) ||
                            sp.PropertyNames[pi].IndexOf(_filterQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _propertyNameProp = _propertyNamesProp.GetArrayElementAtIndex(pi);
                            _propertyTypeProp = _propertyTypesProp.GetArrayElementAtIndex(pi);
                            _propertyEditableProp = _editableProp.GetArrayElementAtIndex(pi);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                using (new EditorGUI.DisabledScope(!MaterialProps.IsSupportedType(sp.PropertyTypes[pi])))
                                {
                                    EditorGUILayout.PropertyField(_propertyEditableProp, GUIContent.none, GUILayout.Width(60));
                                    EditorGUILayout.LabelField(_propertyNameProp.stringValue);
                                }
                                using (new EditorGUI.DisabledScope(true))
                                {
                                    EditorGUILayout.PropertyField(_propertyTypeProp, GUIContent.none);
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