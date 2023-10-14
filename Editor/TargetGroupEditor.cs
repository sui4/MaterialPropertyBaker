using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(TargetGroup))]
    public class TargetGroupEditor : Editor
    {
        private readonly List<bool> _rendererFoldoutList = new();

        private SerializedProperty _rendererMatTargetInfoWrapperSDictProp;
        // private SerializedProperty _defaultProfileProp;

        private bool _renderersFoldout = true;

        private SerializedProperty _renderersProp;

        // serialized property of MaterialGroup(target)
        private SerializedProperty _targetProp;
        private string RenderersFoldoutKey => $"{Target.name}_renderersFoldout";

        private TargetGroup Target => (TargetGroup)target;

        private void OnEnable()
        {
            _targetProp = serializedObject.FindProperty("_target");
            _rendererMatTargetInfoWrapperSDictProp =
                serializedObject.FindProperty("_rendererMatTargetInfoWrapperSDict");
            _renderersProp = serializedObject.FindProperty("_renderers");

            _renderersFoldout = SessionState.GetBool(RenderersFoldoutKey, true);
            Validate();
        }

        private string RendererFoldoutKeyAt(int index) => $"{Target.name}_rendererFoldout_{index}";

        private void Validate()
        {
            for (var i = _rendererFoldoutList.Count; i < _renderersProp.arraySize; i++)
            {
                _rendererFoldoutList.Add(SessionState.GetBool(RendererFoldoutKeyAt(i), true));
            }
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            if (Target == null)
                return;
            serializedObject.Update();
            Validate();
            // default
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(_targetProp);

                if (change.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    Validate();
                }
            }

            EditorGUILayout.Separator();

            CreateBakedPropertyGroupGUI();
            EditorGUILayout.Separator();

            if (GUILayout.Button("Validate & Fix"))
            {
                Target.OnValidate();
            }

            // renderer list
            _renderersFoldout = EditorGUILayout.Foldout(_renderersFoldout, "Renderers");
            SessionState.SetBool(RenderersFoldoutKey, _renderersFoldout);

            EditorUtils.WarningGUI(Target.Warnings);

            if (_renderersFoldout)
            {
                EditorGUI.indentLevel++;
                for (int ri = 0; ri < _renderersProp.arraySize; ri++)
                {
                    var rendererProp = _renderersProp.GetArrayElementAtIndex(ri);
                    var (rendererKeysProp, matStatusSDictWrapperValuesProp) =
                        SerializedDictionaryUtil.GetKeyValueListSerializedProperty(
                            _rendererMatTargetInfoWrapperSDictProp);
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        RendererGUI(ri, rendererProp, rendererKeysProp, matStatusSDictWrapperValuesProp);
                    }

                    // EditorGUILayout.Separator();
                }

                EditorGUI.indentLevel--;
            }
        }

        // ri = renderer index
        private void RendererGUI(int ri, SerializedProperty rendererProp, SerializedProperty rendererKeysProp,
            SerializedProperty matStatusSDictWrapperListProps)
        {
            EditorGUILayout.PropertyField(rendererProp, new GUIContent("Renderer"));

            var currentRenderer = rendererProp.objectReferenceValue as Renderer;
            if (currentRenderer == null) return;

            _rendererFoldoutList[ri] = EditorGUILayout.Foldout(_rendererFoldoutList[ri], "Materials");
            SessionState.SetBool(RendererFoldoutKeyAt(ri), _rendererFoldoutList[ri]);
            if (!_rendererFoldoutList[ri]) return;
            EditorGUI.indentLevel++;

            if (Target.RendererMatTargetInfoWrapperDict.TryGetValue(currentRenderer,
                    out var matTargetInfoSDictWrapper))
            {
                var index = Target.RendererMatTargetInfoWrapperSDict.Keys.IndexOf(currentRenderer);
                var (_, materialStatusSDictWrapperProp) =
                    SerializedDictionaryUtil.GetKeyValueSerializedPropertyAt(index, rendererKeysProp,
                        matStatusSDictWrapperListProps);
                var (matListProp, targetInfoListProp) = GetSerializedPropertyFrom(materialStatusSDictWrapperProp);

                // foreachで回すと、要素の変更時にエラーが出るので、forで回す
                // 今回ここでは要素数を変えないため、index out of rangeは起きない
                for (int mi = 0; mi < matTargetInfoSDictWrapper.MatTargetInfoDict.Count; mi++)
                {
                    var (materialProp, targetInfoProp) =
                        SerializedDictionaryUtil.GetKeyValueSerializedPropertyAt(mi, matListProp, targetInfoListProp);

                    MaterialGUI(materialProp, targetInfoProp);
                }
            }
            else
            {
                Debug.LogError(
                    "Renderer is not found in MaterialGroup. This should not happen. Data may be corrupted.");
            }

            EditorGUI.indentLevel--;
        }

        private void MaterialGUI(SerializedProperty materialProp, SerializedProperty targetInfoProp)
        {
            // Caution: 要素数が変わるとエラーが出るので、要素数を変えないようにする
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.PropertyField(targetInfoProp,
                        new GUIContent(materialProp.objectReferenceValue.name));
                    if (change.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(Target);
                    }
                }
            }
        }

        private void CreateBakedPropertyGroupGUI()
        {
            if (GUILayout.Button("Create MPB Profile"))
            {
                Target.CreateMpbProfileAsset();
            }
        }

        // utils
        private static (SerializedProperty keyMaterialListProp, SerializedProperty valueTargetInfoListProp)
            GetSerializedPropertyFrom(SerializedProperty matTargetInfoSDictWrapperProp)
        {
            if (matTargetInfoSDictWrapperProp == null)
                throw new NullReferenceException("matTargetInfoSDictWrapperProp is null");
            var matTargetInfoSDictProp = matTargetInfoSDictWrapperProp.FindPropertyRelative("_matTargetInfoSDict");
            if (matTargetInfoSDictProp == null) throw new NullReferenceException("matTargetInfoSDictProp is null");
            return SerializedDictionaryUtil.GetKeyValueListSerializedProperty(matTargetInfoSDictProp);
        }
    }
}