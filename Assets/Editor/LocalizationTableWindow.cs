using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Localization.Data;
using Localization.Unity;
using UnityEditor;
using UnityEngine;

namespace Localization.EditorTools
{
    public class LocalizationTableWindow : EditorWindow
    {
        private static readonly Color MissingColor = new Color(1f, 0.55f, 0.55f);

        [SerializeField] private LocalizationConfig _config;

        private LocalizationTable _table;
        private Vector2 _scroll;
        private string _filter = "", _newKey = "";
        private int _sourceIndex;
        private bool _busy;

        private bool Dirty { get => hasUnsavedChanges; set => hasUnsavedChanges = value; }

        [MenuItem("Tools/Localization Table Editor")]
        private static void OpenMenu() => GetWindow<LocalizationTableWindow>("Localization");

        public static void Open(LocalizationConfig config)
        {
            var window = GetWindow<LocalizationTableWindow>("Localization");
            
            window._config = config;
            window.Reload();
        }

        private void OnEnable()
        {
            saveChangesMessage = "The localization table contains unsaved changes. Save them?";
            
            if (_config == null) 
                _config = LocalizationEditorUtil.FindConfig();
            
            Reload();
        }

        public override void SaveChanges() { Save(); base.SaveChanges(); }

        private void Reload()
        {
            _table = LocalizationEditorUtil.LoadTable(_config);
            
            if (_config != null) 
                _table.EnsureLanguages(_config.Languages);
            
            Dirty = false;
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _config = (LocalizationConfig)EditorGUILayout.ObjectField("Config", _config, typeof(LocalizationConfig), false);
            
            if (EditorGUI.EndChangeCheck()) 
                Reload();

            if (_config == null)
            {
                EditorGUILayout.HelpBox("Please create or select config!", MessageType.Info);
                return;
            }
            if (_table == null) 
                Reload();

            DrawToolbar();
            DrawAdd();
            DrawGrid();
        }


        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(Dirty ? "Save *" : "Save", EditorStyles.toolbarButton, GUILayout.Width(70))) Save();
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60))) Reload();

                GUILayout.Label($"Keys: {_table.Entries.Count}   Missing: {CountMissing()}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarTextField, GUILayout.Width(180));
            }
        }

        private void DrawAdd()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _newKey = EditorGUILayout.TextField("New key", _newKey);
                if (GUILayout.Button("Create", GUILayout.Width(150))) AddKey(_newKey.Trim());
            }
        }

        private void DrawGrid()
        {
            const float KeyWidth = 200, CellWidth = 200;
            var langs = _config.Languages;

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Key", EditorStyles.boldLabel, GUILayout.Width(KeyWidth));
                    for (int l = 0; l < langs.Count; l++)
                    {
                        bool isDefault = langs[l] == _config.DefaultLanguage;
                        using (new EditorGUILayout.HorizontalScope(GUILayout.Width(CellWidth)))
                        {
                            GUILayout.Label(isDefault ? langs[l] + " (default)" : langs[l], EditorStyles.boldLabel);
                            
                            if (!isDefault && GUILayout.Button("✕", GUILayout.Width(20)))
                            {
                                RemoveLanguage(langs[l]);
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                }

                int removeIndex = -1;
                for (int i = 0; i < _table.Entries.Count; i++)
                {
                    var entry = _table.Entries[i];
                    if (_filter.Length > 0 &&
                        (entry.Key ?? "").IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        entry.Key = EditorGUILayout.TextField(entry.Key, GUILayout.Width(KeyWidth));
                        for (int l = 0; l < langs.Count; l++)
                        {
                            var cell = entry.Cell(langs[l]);
                            var old = GUI.backgroundColor;
                            if (string.IsNullOrEmpty(cell.Text)) GUI.backgroundColor = MissingColor;
                            cell.Text = EditorGUILayout.TextField(cell.Text, GUILayout.Width(CellWidth));
                            GUI.backgroundColor = old;
                        }
                        if (EditorGUI.EndChangeCheck()) Dirty = true;
                        if (GUILayout.Button("✕", GUILayout.Width(22))) removeIndex = i;
                    }
                }
                if (removeIndex >= 0) { _table.Entries.RemoveAt(removeIndex); Dirty = true; }
            }
        }
        
        private void AddKey(string key)
        {
            if (string.IsNullOrEmpty(key)) 
                return;
            
            if (_table.Find(key) != null) 
            { 
                ShowNotification(new GUIContent("Error key")); 
                return; 
            }

            _table.Add(key).Cell(_config.DefaultLanguage);
            _table.EnsureLanguages(_config.Languages);
            
            _newKey = "";
            Dirty = true;
            
            GUI.FocusControl(null);
        }

        private void AddLanguage(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Contains(" ")) return;
            if (_config.Languages.Contains(code)) { ShowNotification(new GUIContent("The language has already been added")); return; }

            var so = new SerializedObject(_config);
            var list = so.FindProperty("languages");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = code;
            so.ApplyModifiedProperties();

            _table.EnsureLanguages(_config.Languages);
            Dirty = true;
            GUI.FocusControl(null);
        }

        private void RemoveLanguage(string code)
        {
            if (!EditorUtility.DisplayDialog("Remove language", $"Remove '{code}' and all his translations?", "Delete", "Cancel"))
                return;

            var so = new SerializedObject(_config);
            var list = so.FindProperty("languages");
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).stringValue != code) continue;
                list.DeleteArrayElementAtIndex(i);
                break;
            }
            so.ApplyModifiedProperties();
            _table.RemoveLanguage(code);
            Dirty = true;
        }

        private int CountMissing()
        {
            int missing = 0;
            foreach (var entry in _table.Entries)
                foreach (var lang in _config.Languages)
                {
                    var cell = entry.Values.Find(v => v.Language == lang);
                    if (cell == null || string.IsNullOrEmpty(cell.Text)) missing++;
                }
            return missing;
        }

        private void Save()
        {
            var duplicates = _table.Entries.GroupBy(e => e.Key).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Count > 0)
                Debug.LogWarning("[Localization] key duplicate: " + string.Join(", ", duplicates));

            var path = _config.Table != null ? AssetDatabase.GetAssetPath(_config.Table) : CreateTableAsset();
            File.WriteAllText(path, JsonUtility.ToJson(_table, true));
            AssetDatabase.ImportAsset(path);
            Dirty = false;
        }

        private string CreateTableAsset()
        {
            var dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(_config));
            var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(dir, "LocalizationTable.json").Replace('\\', '/'));
            File.WriteAllText(path, "{}");
            AssetDatabase.ImportAsset(path);

            var so = new SerializedObject(_config);
            so.FindProperty("table").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            so.ApplyModifiedProperties();
            return path;
        }

        private static bool PlaceholdersPreserved(string source, string result)
        {
            foreach (Match m in Regex.Matches(source, @"\{\d+\}"))
                if (!result.Contains(m.Value)) return false;
            return true;
        }
    }
}
