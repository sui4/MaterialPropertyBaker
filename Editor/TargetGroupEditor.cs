using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace sui4.MaterialPropertyBaker
{
    [CustomEditor(typeof(TargetGroup))]
    public class TargetGroupEditor : Editor
    {
        // serialized property of MaterialGroup(target)
        private SerializedProperty _targetProp;
        private SerializedProperty _renderersProp;

        private SerializedProperty _rendererMatTargetInfoWrapperSDictProp;
        // private SerializedProperty _defaultProfileProp;

        private static class Styles
        {
            public static readonly GUIContent
                OverrideDefaultProfileLabel = new GUIContent("Preset to Override Default");

            public static readonly GUIContent MaterialLabel = GUIContent.none;
            public static readonly GUIContent IDLabel = new GUIContent("ID");
        }

        private TargetGroup Target => (TargetGroup)target;

        private void OnEnable()
        {
            // _defaultProfileProp = serializedObject.FindProperty("_overrideDefaultPreset");
            _targetProp = serializedObject.FindProperty("_target");
            _rendererMatTargetInfoWrapperSDictProp =
                serializedObject.FindProperty("_rendererMatTargetInfoWrapperSDict");
            _renderersProp = serializedObject.FindProperty("_renderers");
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            serializedObject.Update();
            if (Target == null)
                return;

            // default
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(_targetProp);
                // EditorGUILayout.PropertyField(_defaultProfileProp, Styles.OverrideDefaultProfileLabel);

                if (change.changed)
                    serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Separator();

            CreateBakedPropertyGroupGUI();
            EditorGUILayout.Separator();

            if (GUILayout.Button("Validate"))
            {
                Target.OnValidate();
            }

            // renderer list
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Renderers", new GUIStyle("label"));
                EditorUtils.WarningGUI(Target.Warnings);

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

                    EditorGUILayout.Separator();
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