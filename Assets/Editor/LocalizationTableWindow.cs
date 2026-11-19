using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Localization.Core;
using Localization.Data;
using Localization.Unity;
using Runtime.Data.SpriteData;
using UnityEditor;
using UnityEngine;

namespace Localization.EditorTools
{
    public class LocalizationTableWindow : EditorWindow
    {
        private enum Tab
        {
            Texts = 0,
            Sprites = 1
        }

        private static readonly Color MissingColor = new Color(1f, 0.55f, 0.55f);
        private static readonly string[] TabNames = { "Texts", "Sprites" };

        [SerializeField] private LocalizationConfig _config;
        [SerializeField] private Tab _activeTab = Tab.Texts;
        [SerializeField] private bool _configExpanded = true;

        private LocalizationTextTable _textTable;
        private List<LocalizationSpriteEntry> _spriteEntries = new List<LocalizationSpriteEntry>();

        private Vector2 _scroll;
        private string _filter = "";
        private string _newKey = "";
        private int _selectedLangIndex;
        private bool _isInternalReload;

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
            saveChangesMessage = "The localization data contains unsaved changes. Save them?";
            LocalizationEditorEvents.TableChanged += OnExternalTableChanged;

            if (_config == null)
                _config = LocalizationEditorUtil.FindConfig();

            Reload();
        }

        private void OnDisable()
        {
            LocalizationEditorEvents.TableChanged -= OnExternalTableChanged;
        }

        private void OnExternalTableChanged()
        {
            if (_isInternalReload)
                return;

            Reload();
            Repaint();
        }

        public override void SaveChanges()
        {
            Save();
            base.SaveChanges();
        }

        private void Reload()
        {
            _textTable = LocalizationEditorUtil.LoadTable(_config);
            _spriteEntries = new List<LocalizationSpriteEntry>();

            if (_config != null)
            {
                _textTable.EnsureLanguages(_config.Languages);
                CopySpriteEntriesFromConfig();
                _spriteEntries.EnsureLanguages(_config.Languages);
            }

            Dirty = false;
        }

        private void OnGUI()
        {
            DrawConfigHeader();

            if (_config == null)
            {
                EditorGUILayout.HelpBox("Please create or select LocalizationConfig.", MessageType.Info);
                if (GUILayout.Button("Create LocalizationConfig", GUILayout.Height(26)))
                    CreateConfigAsset();
                return;
            }

            if (_textTable == null)
                Reload();

            DrawConfigPanel();

            EditorGUILayout.Space(4);
            var nextTab = (Tab)GUILayout.Toolbar((int)_activeTab, TabNames, GUILayout.Height(24));
            if (nextTab != _activeTab)
            {
                _activeTab = nextTab;
                _newKey = "";
                GUI.FocusControl(null);
            }

            DrawToolbar();
            DrawAddKeyBar();

            if (_activeTab == Tab.Texts)
                DrawTextsGrid();
            else
                DrawSpritesGrid();
        }

        private void DrawConfigHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                _config = (LocalizationConfig)EditorGUILayout.ObjectField("Config", _config, typeof(LocalizationConfig), false);
                if (EditorGUI.EndChangeCheck())
                    Reload();
            }
        }

        private void DrawConfigPanel()
        {
            _configExpanded = EditorGUILayout.Foldout(_configExpanded, "Config Settings", true, EditorStyles.foldoutHeader);
            if (!_configExpanded)
                return;

            var so = new SerializedObject(_config);
            so.Update();

            var tableProp = so.FindProperty("table");
            var defaultLangProp = so.FindProperty("defaultLanguage");
            var languagesProp = so.FindProperty("languages");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(tableProp, new GUIContent("Text Table (JSON)"));
                if (EditorGUI.EndChangeCheck())
                {
                    so.ApplyModifiedProperties();
                    Reload();
                    so.Update();
                }

                var langOptions = GetLanguageOptions(languagesProp);
                int currentDefaultIdx = Math.Max(0, Array.IndexOf(langOptions, defaultLangProp.stringValue));
                if (langOptions.Length > 0)
                {
                    EditorGUI.BeginChangeCheck();
                    int nextDefaultIdx = EditorGUILayout.Popup("Default Language", currentDefaultIdx, langOptions);
                    if (EditorGUI.EndChangeCheck())
                    {
                        defaultLangProp.stringValue = langOptions[nextDefaultIdx];
                        so.ApplyModifiedProperties();
                        Dirty = true;
                    }
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Languages", EditorStyles.miniBoldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int i = 0; i < languagesProp.arraySize; i++)
                    {
                        var langElement = languagesProp.GetArrayElementAtIndex(i);
                        string code = langElement.stringValue;
                        bool isDefault = code == defaultLangProp.stringValue;

                        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                        {
                            GUILayout.Label(isDefault ? $"{code} *" : code, EditorStyles.miniButton);
                            if (!isDefault && GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(18)))
                            {
                                RemoveLanguage(code);
                                GUIUtility.ExitGUI();
                            }
                        }
                    }

                    var availableCodes = SystemLanguageCodes.AllCodes
                        .Where(c => !_config.Languages.Contains(c))
                        .ToArray();

                    if (availableCodes.Length > 0)
                    {
                        _selectedLangIndex = Mathf.Clamp(_selectedLangIndex, 0, availableCodes.Length - 1);
                        _selectedLangIndex = EditorGUILayout.Popup(_selectedLangIndex, availableCodes, GUILayout.Width(64));
                        if (GUILayout.Button("+ Lang", EditorStyles.miniButton, GUILayout.Width(54)))
                        {
                            AddLanguage(availableCodes[_selectedLangIndex]);
                            _selectedLangIndex = 0;
                            GUI.FocusControl(null);
                        }
                    }
                    else
                    {
                        GUILayout.Label("All supported added", EditorStyles.miniLabel);
                    }
                }
            }

            so.ApplyModifiedProperties();
        }

        private static string[] GetLanguageOptions(SerializedProperty languagesProp)
        {
            var list = new List<string>();
            for (int i = 0; i < languagesProp.arraySize; i++)
            {
                var val = languagesProp.GetArrayElementAtIndex(i).stringValue;
                if (!string.IsNullOrWhiteSpace(val))
                    list.Add(val);
            }
            return list.Count > 0 ? list.ToArray() : new[] { "en" };
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(Dirty ? "Save *" : "Save", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    Save();
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    Reload();

                if (_activeTab == Tab.Texts)
                    GUILayout.Label($"Keys: {_textTable.Entries.Count}   Missing: {CountMissingTexts()}", EditorStyles.miniLabel);
                else
                    GUILayout.Label($"Sprites: {_spriteEntries.Count}   Missing: {CountMissingSprites()}", EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();
                _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarTextField, GUILayout.Width(180));
            }
        }

        private void DrawAddKeyBar()
        {
            string label = _activeTab == Tab.Texts ? "New text key" : "New sprite key";
            using (new EditorGUILayout.HorizontalScope())
            {
                _newKey = EditorGUILayout.TextField(label, _newKey);
                if (GUILayout.Button("Create", GUILayout.Width(150)))
                {
                    if (_activeTab == Tab.Texts)
                        AddTextKey(_newKey.Trim());
                    else
                        AddSpriteKey(_newKey.Trim());
                }
            }
        }

        private void DrawTextsGrid()
        {
            const float KeyWidth = 200f;
            const float CellWidth = 200f;
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
                        GUILayout.Label(isDefault ? $"{langs[l]} (default)" : langs[l], EditorStyles.boldLabel, GUILayout.Width(CellWidth));
                    }
                }

                int removeIndex = -1;
                for (int i = 0; i < _textTable.Entries.Count; i++)
                {
                    var entry = _textTable.Entries[i];
                    if (_filter.Length > 0 &&
                        (entry.Key ?? "").IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        entry.Key = EditorGUILayout.TextField(entry.Key, GUILayout.Width(KeyWidth));

                        for (int l = 0; l < langs.Count; l++)
                        {
                            var cell = entry.Cell(langs[l]);
                            var oldColor = GUI.backgroundColor;
                            if (string.IsNullOrEmpty(cell.Text))
                                GUI.backgroundColor = MissingColor;

                            cell.Text = EditorGUILayout.TextField(cell.Text, GUILayout.Width(CellWidth));
                            GUI.backgroundColor = oldColor;
                        }

                        if (EditorGUI.EndChangeCheck())
                            Dirty = true;

                        if (GUILayout.Button("✕", GUILayout.Width(22)))
                            removeIndex = i;
                    }
                }

                if (removeIndex >= 0)
                {
                    _textTable.Entries.RemoveAt(removeIndex);
                    Dirty = true;
                }
            }
        }

        private void DrawSpritesGrid()
        {
            const float KeyWidth = 180f;
            const float SlotSize = 64f;
            const float ColumnWidth = 96f;
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
                        GUILayout.Label(isDefault ? $"{langs[l]} *" : langs[l], EditorStyles.boldLabel, GUILayout.Width(ColumnWidth));
                    }
                }

                int removeIndex = -1;
                for (int i = 0; i < _spriteEntries.Count; i++)
                {
                    var entry = _spriteEntries[i];
                    if (_filter.Length > 0 &&
                        (entry.Key ?? "").IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    using (new EditorGUILayout.HorizontalScope(GUILayout.Height(SlotSize + 6f)))
                    {
                        EditorGUI.BeginChangeCheck();
                        entry.Key = EditorGUILayout.TextField(entry.Key, GUILayout.Width(KeyWidth));
                        if (EditorGUI.EndChangeCheck())
                            Dirty = true;

                        for (int l = 0; l < langs.Count; l++)
                        {
                            var cell = entry.Cell(langs[l]);

                            using (new EditorGUILayout.HorizontalScope(GUILayout.Width(ColumnWidth)))
                            {
                                var oldColor = GUI.backgroundColor;
                                if (cell.Sprite == null)
                                    GUI.backgroundColor = MissingColor;

                                EditorGUI.BeginChangeCheck();
                                var nextSprite = (Sprite)EditorGUILayout.ObjectField(
                                    cell.Sprite,
                                    typeof(Sprite),
                                    false,
                                    GUILayout.Width(SlotSize),
                                    GUILayout.Height(SlotSize));

                                if (EditorGUI.EndChangeCheck())
                                {
                                    cell.Sprite = nextSprite;
                                    Dirty = true;
                                }

                                GUI.backgroundColor = oldColor;
                                GUILayout.Space(ColumnWidth - SlotSize);
                            }
                        }

                        if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(22)))
                            removeIndex = i;
                    }
                }

                if (removeIndex >= 0)
                {
                    _spriteEntries.RemoveAt(removeIndex);
                    Dirty = true;
                }
            }
        }

        private void AddTextKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (_textTable.Find(key) != null)
            {
                ShowNotification(new GUIContent("Key already exists"));
                return;
            }

            _textTable.Add(key).Cell(_config.DefaultLanguage);
            _textTable.EnsureLanguages(_config.Languages);

            _newKey = "";
            Dirty = true;
            GUI.FocusControl(null);
        }

        private void AddSpriteKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (_spriteEntries.Find(key) != null)
            {
                ShowNotification(new GUIContent("Sprite key already exists"));
                return;
            }

            _spriteEntries.Add(key);
            _spriteEntries.EnsureLanguages(_config.Languages);

            _newKey = "";
            Dirty = true;
            GUI.FocusControl(null);
        }

        private void AddLanguage(string code)
        {
            if (!SystemLanguageCodes.IsKnownCode(code))
            {
                ShowNotification(new GUIContent("Unsupported language code"));
                return;
            }

            if (_config.Languages.Contains(code))
            {
                ShowNotification(new GUIContent("Language already exists"));
                return;
            }

            var so = new SerializedObject(_config);
            var list = so.FindProperty("languages");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = code;
            so.ApplyModifiedProperties();

            _textTable.EnsureLanguages(_config.Languages);
            _spriteEntries.EnsureLanguages(_config.Languages);

            Dirty = true;
        }

        private void RemoveLanguage(string code)
        {
            if (code == _config.DefaultLanguage)
                return;

            if (!EditorUtility.DisplayDialog("Remove language", $"Remove '{code}' and all its translations?", "Delete", "Cancel"))
                return;

            var so = new SerializedObject(_config);
            var list = so.FindProperty("languages");
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).stringValue != code)
                    continue;
                list.DeleteArrayElementAtIndex(i);
                break;
            }
            so.ApplyModifiedProperties();

            _textTable.RemoveLanguage(code);
            _spriteEntries.RemoveLanguage(code);

            Dirty = true;
        }

        private void CopySpriteEntriesFromConfig()
        {
            if (_config.SpriteEntries == null)
                return;

            foreach (var src in _config.SpriteEntries)
            {
                var copy = new LocalizationSpriteEntry { Key = src.Key };
                foreach (var v in src.Values)
                    copy.Values.Add(new LocalizationSpriteValue { Language = v.Language, Sprite = v.Sprite });
                _spriteEntries.Add(copy);
            }
        }

        private int CountMissingTexts()
        {
            int missing = 0;
            foreach (var entry in _textTable.Entries)
            {
                foreach (var lang in _config.Languages)
                {
                    var cell = entry.Values.Find(v => v.Language == lang);
                    if (cell == null || string.IsNullOrEmpty(cell.Text))
                        missing++;
                }
            }
            return missing;
        }

        private int CountMissingSprites()
        {
            int missing = 0;
            foreach (var entry in _spriteEntries)
            {
                foreach (var lang in _config.Languages)
                {
                    var cell = entry.Values.Find(v => v.Language == lang);
                    if (cell == null || cell.Sprite == null)
                        missing++;
                }
            }
            return missing;
        }

        private void Save()
        {
            if (_config == null)
                return;

            var duplicates = _textTable.Entries
                .GroupBy(e => e.Key)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0)
                Debug.LogWarning("[Localization] Duplicate text keys: " + string.Join(", ", duplicates));

            var tablePath = _config.Table != null
                ? AssetDatabase.GetAssetPath(_config.Table)
                : CreateTableAsset();

            Undo.RecordObject(_config, "Save Localization Config");
            _config.SpriteEntries.Clear();
            foreach (var src in _spriteEntries)
            {
                var copy = new LocalizationSpriteEntry { Key = src.Key };
                foreach (var v in src.Values)
                    copy.Values.Add(new LocalizationSpriteValue { Language = v.Language, Sprite = v.Sprite });
                _config.SpriteEntries.Add(copy);
            }

            _isInternalReload = true;
            try
            {
                if (!string.IsNullOrEmpty(tablePath))
                {
                    File.WriteAllText(tablePath, JsonUtility.ToJson(_textTable, true));
                    AssetDatabase.ImportAsset(tablePath);
                }

                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
                LocalizationEditorEvents.RaiseTableChanged();
            }
            finally
            {
                _isInternalReload = false;
            }

            Dirty = false;
        }

        private string CreateTableAsset()
        {
            var configPath = AssetDatabase.GetAssetPath(_config);
            var dir = string.IsNullOrEmpty(configPath) ? "Assets" : Path.GetDirectoryName(configPath);
            if (string.IsNullOrEmpty(dir))
                dir = "Assets";

            var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(dir, "LocalizationTable.json").Replace('\\', '/'));
            File.WriteAllText(path, "{}");
            AssetDatabase.ImportAsset(path);

            var so = new SerializedObject(_config);
            so.FindProperty("table").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            so.ApplyModifiedProperties();
            return path;
        }

        private void CreateConfigAsset()
        {
            const string defaultDir = "Assets";
            var path = AssetDatabase.GenerateUniqueAssetPath($"{defaultDir}/LocalizationConfig.asset");
            var asset = CreateInstance<LocalizationConfig>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _config = asset;
            Reload();
        }
    }
}