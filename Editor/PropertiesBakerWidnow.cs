#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using File = System.IO.File;

namespace sui4.MaterialPropertyBaker
{
    
    [EditorWindowTitle(title = "Material Property Baker")]
    public class MaterialPropsExporter : EditorWindow
    {
        [SerializeField] private List<Material> _targets = new List<Material>();
        [SerializeField] private List<BakedMaterialProperty> _presets = new List<BakedMaterialProperty>();
        
        private int _selected = 0; 
        private BakedMaterialPropertiesEditor _editor;

        private Vector2 _scrollPosition = Vector2.zero;
        
        private Vector2 _scrollPositionSearched = Vector2.zero;
        private string _searchQuery = ""; // shader name
        private string _filterQuery = "";

        private List<Material> _materials = new List<Material>(); 

        // [MenuItem("tools/Material Property Baker/Baker")]
        private static void Init()
        {
            var window = (MaterialPropsExporter)GetWindow(typeof(MaterialPropsExporter));
            window.Show();
        }


        #region Lifecycle

        private void OnEnable()
        {
            _selected = 0;
            if (_targets.Count == 0)
            {
                _targets.Add(null);
                _presets.Clear();
                _presets.Add(ScriptableObject.CreateInstance<BakedMaterialProperty>());
            }
        }

        private void OnDestroy()
        {
            for(int i = 0; i < _presets.Count; i++)
            {
                var preset = _presets[i];
                DestroyProfile(ref preset);
                _presets[i] = null;
            }
            _presets.Clear();
        }

        #endregion // Lifecycle

        #region GUI
        private void OnGUI()
        {
            GUILayout.Label("MaterialProps Exporter", EditorStyles.boldLabel);
            EditorGUILayout.Separator();

            //--- export button ---//
            var tmpColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Export All"))
            {
                ExportProfilesAll(_targets, _presets);
            }

            GUI.backgroundColor = tmpColor;

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            //--- profile ---//
            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(100));
                TargetMaterialListGUI();
                EditorGUILayout.EndVertical();

                SelectedBakedPropertiesGUI(_selected);
            }
        }

        private void TargetMaterialListGUI()
        {
            if (GUILayout.Button("Add Target Material"))
            {
                _targets.Add(null);
                _presets.Add(ScriptableObject.CreateInstance<BakedMaterialProperty>());
            }
            
            EditorGUILayout.LabelField("Target Materials");
            for (int i = 0; i < _targets.Count; i++)
            {
                var style = _selected == i ? "OL SelectedRow" : "box";
                using (new EditorGUILayout.HorizontalScope(style))
                {
                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        bool isCurrentSelected = GUILayout.Toggle(_selected == i, "", GUILayout.Width(20));
                        if (isCurrentSelected)
                        {
                            _selected = i; // Select a new item otherwise.
                            Repaint();  // Force the editor window to repaint.
                        }
                        _targets[i] = (Material)EditorGUILayout.ObjectField(_targets[i], typeof(Material), true);

                        if (change.changed)
                        {
                            var prev = _presets[i];
                            if(prev != null)
                                DestroyProfile(ref prev);
                            CreateProfile(_targets[i], out var preset);
                            _presets[i] = preset;
                        }
                    }
                    using(new EditorGUI.DisabledScope(_targets.Count == 1))
                    {
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            _targets.RemoveAt(i);
                        }
                    }
                }
                // Check if the last layout control (our ObjectField) was clicked.
                if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    _selected = i;  // Set selected to current index.
                    Repaint();  // Force the editor window to repaint.
                }
            }
        }

        private void SelectedBakedPropertiesGUI(int selected)
        {
            
            using (new EditorGUILayout.VerticalScope())
            {
                //--- profile ---//
                if (selected == -1 || _targets[selected] == null)
                {
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        EditorGUILayout.LabelField("Material is not set.");

                        SearchMaterialButtonGUI();
                        EditorGUILayout.Separator();
                        EditorGUILayout.Separator();
                        SearchedMaterialListWithFilterGUI();
                    }
                }
                else
                {
                    var mat = _targets[selected];
                    var preset = _presets[selected];
                    MaterialPropsGUI(ref mat, ref preset);
                }
            }
        }

        private void MaterialPropsGUI(ref Material material, ref BakedMaterialProperty preset)
        {
            if (_editor == null)
            {
                if(preset == null)
                    return;
                _editor = Editor.CreateEditor(preset) as BakedMaterialPropertiesEditor;
            }
            else if (_editor.target != preset)
            {
                DestroyImmediate(_editor);
                _editor = null;
                if(preset == null)
                    return;
                _editor = Editor.CreateEditor(preset) as BakedMaterialPropertiesEditor;
            }

            if (_editor != null)
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                EditorGUILayout.LabelField("Profile");
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledScope())
                {
                    _editor.OnInspectorGUI();
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.EndScrollView();
                        
                Color tmp = GUI.backgroundColor;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button($"Export \"{material.name}\""))
                {
                    ExportProfile(material, preset);
                }
                GUI.backgroundColor = tmp;

            }
        }

        private void SearchMaterialButtonGUI()
        {
            //--- search material ---//
            _searchQuery = EditorGUILayout.TextField("Shader Name", _searchQuery);
            
            if (GUILayout.Button("Search Material in Folder"))
            {
                var absolutePath = EditorUtility.SaveFolderPanel("Select Folder", Application.dataPath, "");
                string relativePath = "";

                if (absolutePath.StartsWith(Application.dataPath))
                {
                    relativePath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
                }

                _materials = MaterialFinder.GetMaterialsByShaderNameInFolder(_searchQuery, relativePath);
                
                EditorUtility.DisplayDialog("Search Material", $"Found {_materials.Count} materials of {_searchQuery}", "OK");
                Debug.Log(_materials.Count);
            }
        }
        
        private void SearchedMaterialListWithFilterGUI()
        {
            // search result
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Search Result");
                
                // Filter field
                _filterQuery = EditorGUILayout.TextField("Filter", _filterQuery);
                EditorGUILayout.Separator();

                _scrollPositionSearched = EditorGUILayout.BeginScrollView(_scrollPositionSearched);

                foreach (var mat in _materials)
                {
                    // Filter materials based on search query
                    if (string.IsNullOrEmpty(_filterQuery) || mat.name.IndexOf(_filterQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        // Add button to add material to target list
                        if (GUILayout.Button("Add", GUILayout.Width(50)))
                        {
                            _targets.Add(mat);
                            CreateProfile(mat, out var preset);
                            _presets.Add(preset);
                        }
                        // Use non-editable ObjectField to display and select materials
                        EditorGUILayout.LabelField(Utils.UnderscoresToSpaces(mat.name));
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.ObjectField("", mat, typeof(Material), false);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        #endregion // GUI

        #region Export
        // 単体でExportする場合
        private bool ExportProfile(in Material targetMat, BakedMaterialProperty preset)
        {
            if (targetMat == null)
            {
                Debug.LogWarning($"Export BakedProperties: target material is null.: export skipped");
                return false;
            }
            if (preset == null)
            {
                CreateProfile(targetMat, out preset);
            }
            var assetName = $"MaterialProps_{targetMat.name}";
            
            var profileToSave = Instantiate(preset);
            
            EditorUtility.SetDirty(profileToSave);
            var defaultPath = Application.dataPath;
            var path = EditorUtility.SaveFilePanelInProject("Save profile", assetName, "asset", "Save BakedProperties ScriptableObject", defaultPath);
            if (path.Length != 0)
            {
                var fullPath = Path.Join(Application.dataPath, path.Replace("Assets/", "\\"));
                if (File.Exists(fullPath))
                {
                    Debug.Log($"{GetType()}: delete existing: {path}");
                    var sucess = AssetDatabase.DeleteAsset(path);
                    if (!sucess)
                    {
                        Debug.LogError($"{GetType()}: failed to delete existing: {path}");
                        return false;
                    }
                }
                AssetDatabase.CreateAsset(profileToSave, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"Saved : {path}");
            }

            return true;
        }
        
        // path: Assets以下のパス
        // パスを指定してExportする場合
        private bool ExportProfile(in Material targetMat, BakedMaterialProperty preset, string path, bool refresh = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"Export BakedProperties: path is null or empty.: failed to export preset of{targetMat.name}");
                return false;
            }

            if (targetMat == null)
            {
                Debug.LogWarning($"Export BakedProperties: target material is null.: export skipped");
                return false;
            }
            if (preset == null)
            {
                CreateProfile(targetMat, out preset);
            }

            var profileToSave = Instantiate(preset);
            EditorUtility.SetDirty(profileToSave);

            // Combine the save directory with the asset name to create the full path
            var assetFileName = $"MaterialProps_{targetMat.name}.asset";
            path = $"{path}/{assetFileName}";

            if (File.Exists(path))
            {
                Debug.Log($"{GetType()}: delete existing: {path}");
                var sucess = AssetDatabase.DeleteAsset(path);
                if (!sucess)
                {
                    Debug.LogError($"{GetType()}: failed to delete existing: {path}");
                    return false;
                }
            }

            AssetDatabase.CreateAsset(profileToSave, path);
            if (refresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"Saved : {path}");
            return true;
        }

        public void ExportProfilesAll(List<Material> materials, List<BakedMaterialProperty> presets)
        {
            var defaultPath = Application.dataPath; 
            var defaultFolderName = $"MaterialProfileData";
            
            string relativeFolderPath = EditorUtility.SaveFolderPanel("Select Folder to Save Profiles", defaultPath, defaultFolderName);
            var absoluteFolderPath = relativeFolderPath.Replace(Application.dataPath, "Assets");
            if (string.IsNullOrEmpty(absoluteFolderPath))
            {
                Debug.Log($"Export BakedProperties: Export Canceled");
                return;
            }

            int skipped = 0, failed = 0, exported = 0;
            if(EditorUtility.DisplayDialog("Export BakedProperties", $"Export BakedProperties Preset to {absoluteFolderPath}. すでに存在している同名のプリセットは上書きされますがよろしいですか？", "OK", "Cancel"))
            {
                for (int i = 0; i < materials.Count; i++)
                {
                    if (materials[i] == null)
                    {
                        skipped++;
                        continue;
                    }
                    var sucess = ExportProfile(materials[i], presets[i], absoluteFolderPath, false);
                    exported += sucess ? 1 : 0;
                    failed += sucess ? 0 : 1;
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("BakedProperties Preset Exported",
                $"Suceeded: {exported}\nSkipped: {skipped}\nFailed: {failed}",
                "OK", "Cancel");
        }

        #endregion // Export

        private static void DestroyProfile(ref BakedMaterialProperty preset)
        {
            if (preset != null)
            {
                ScriptableObject.DestroyImmediate(preset);
                preset = null;
            }
        }
        private static void CreateProfile(Material mat, out BakedMaterialProperty preset)
        {
            preset = ScriptableObject.CreateInstance<BakedMaterialProperty>();
            preset.name = mat.name;
            preset.ShaderName = mat.shader.name;
            preset.CreatePropsFromMaterial(mat);
        }
    } 
}

#endif
