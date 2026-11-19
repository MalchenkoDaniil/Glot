#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Localization.Abstractions;
using Localization.Components;
using Localization.Core;
using Localization.Data;
using Localization.Unity;
using Runtime.Data.SpriteData;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Localization.EditorTools
{
    public interface ILocalizationEntryAdapter<TValue>
    {
        bool Exists(LocalizationConfig config, string key);
        void CreateKey(LocalizationConfig config, string key, IReadOnlyDictionary<string, TValue> values);
        TValue GetValue(LocalizationConfig config, string key, string language);
        void SetValue(LocalizationConfig config, string key, string language, TValue value);
        void ApplyPreview(Component targetComponent, LocalizationConfig config, string key);
    }

    public abstract class BaseLocalizerEditor<TProvider, TValue> : Editor
        where TProvider : class, ILocalizationEvents
    {
        private readonly Dictionary<string, TValue> _draftValues = new Dictionary<string, TValue>();
        private SerializedProperty _keyProp;
        private string _draftKey = "";

        protected abstract ILocalizationEntryAdapter<TValue> Adapter { get; }

        protected virtual void OnEnable()
        {
            _keyProp = serializedObject.FindProperty("localizationKey");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_keyProp != null)
                EditorGUILayout.PropertyField(_keyProp);

            DrawExtraFields();

            serializedObject.ApplyModifiedProperties();

            var config = LocalizationEditorUtil.FindConfig();
            string rawKey = _keyProp != null ? _keyProp.stringValue : "";

            if (config == null)
            {
                EditorGUILayout.HelpBox("LocalizationConfig not found.", MessageType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(rawKey))
                return;

            string key = rawKey.Trim();
            var adapter = Adapter;

            if (adapter.Exists(config, key))
                DrawExistingValuesEditor(config, adapter, key);
            else
                DrawCreateKeySection(config, adapter, key);
        }

        protected virtual void DrawExtraFields()
        {
        }

        protected abstract TValue DrawValueField(string language, TValue currentValue);

        protected virtual TValue GetDefaultDraftValue() => default;

        private void DrawExistingValuesEditor(LocalizationConfig config, ILocalizationEntryAdapter<TValue> adapter, string key)
        {
            bool modified = false;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Values ({key})", EditorStyles.miniBoldLabel);

                for (int i = 0; i < config.Languages.Count; i++)
                {
                    string lang = config.Languages[i];
                    TValue current = adapter.GetValue(config, key, lang);

                    EditorGUI.BeginChangeCheck();
                    TValue next = DrawValueField(lang, current);
                    if (EditorGUI.EndChangeCheck())
                    {
                        adapter.SetValue(config, key, lang, next);
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                adapter.ApplyPreview((Component)target, config, key);
                LocalizationEditorEvents.RaiseTableChanged();
            }
        }

        private void DrawCreateKeySection(LocalizationConfig config, ILocalizationEntryAdapter<TValue> adapter, string key)
        {
            SyncDraft(config, key);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox($"Key \"{key}\" does not exist.", MessageType.Info);

                if (GUILayout.Button("Create Key", GUILayout.Height(24)))
                {
                    adapter.CreateKey(config, key, _draftValues);
                    ClearDraft();
                    adapter.ApplyPreview((Component)target, config, key);
                    LocalizationEditorEvents.RaiseTableChanged();
                    GUI.FocusControl(null);
                }
            }
        }

        private void SyncDraft(LocalizationConfig config, string key)
        {
            if (_draftKey == key)
                return;

            _draftKey = key;
            _draftValues.Clear();

            for (int i = 0; i < config.Languages.Count; i++)
                _draftValues[config.Languages[i]] = GetDefaultDraftValue();
        }

        private void ClearDraft()
        {
            _draftKey = "";
            _draftValues.Clear();
        }
    }

    public sealed class TextLocalizationAdapter : ILocalizationEntryAdapter<string>
    {
        public bool Exists(LocalizationConfig config, string key)
        {
            var table = LocalizationEditorUtil.LoadTable(config);
            return table.Find(key) != null;
        }

        public void CreateKey(LocalizationConfig config, string key, IReadOnlyDictionary<string, string> values)
        {
            var table = LocalizationEditorUtil.LoadTable(config);
            table.EnsureLanguages(config.Languages);

            var entry = table.Find(key) ?? table.Add(key);
            foreach (var lang in config.Languages)
                entry.Cell(lang).Text = values != null && values.TryGetValue(lang, out var v) ? v ?? "" : "";

            SaveTable(config, table);
        }

        public string GetValue(LocalizationConfig config, string key, string language)
        {
            var table = LocalizationEditorUtil.LoadTable(config);
            table.EnsureLanguages(config.Languages);
            var entry = table.Find(key);
            return entry != null ? entry.Cell(language).Text : "";
        }

        public void SetValue(LocalizationConfig config, string key, string language, string value)
        {
            var table = LocalizationEditorUtil.LoadTable(config);
            table.EnsureLanguages(config.Languages);
            var entry = table.Find(key);
            if (entry == null)
                return;

            entry.Cell(language).Text = value ?? "";
            SaveTable(config, table);
        }

        public void ApplyPreview(Component targetComponent, LocalizationConfig config, string key)
        {
            if (targetComponent == null)
                return;

            var tmp = targetComponent.GetComponent<TMP_Text>();
            if (tmp == null)
                return;

            var table = LocalizationEditorUtil.LoadTable(config);
            tmp.text = ResolveText(table, config.DefaultLanguage, key);
            EditorUtility.SetDirty(tmp);
        }

        private static string ResolveText(LocalizationTextTable table, string defaultLang, string key)
        {
            var entry = table.Find(key);
            if (entry != null)
            {
                var defVal = entry.Values.Find(v => v.Language == defaultLang);
                if (defVal != null && !string.IsNullOrEmpty(defVal.Text))
                    return defVal.Text;

                var anyVal = entry.Values.Find(v => !string.IsNullOrEmpty(v.Text));
                if (anyVal != null)
                    return anyVal.Text;
            }

            return key;
        }

        private static void SaveTable(LocalizationConfig config, LocalizationTextTable table)
        {
            var path = config.Table != null
                ? AssetDatabase.GetAssetPath(config.Table)
                : CreateTableAsset(config);

            if (string.IsNullOrEmpty(path))
                return;

            File.WriteAllText(path, JsonUtility.ToJson(table, true));
            AssetDatabase.ImportAsset(path);
        }

        private static string CreateTableAsset(LocalizationConfig config)
        {
            var configPath = AssetDatabase.GetAssetPath(config);
            var dir = string.IsNullOrEmpty(configPath) ? "Assets" : Path.GetDirectoryName(configPath);
            if (string.IsNullOrEmpty(dir))
                dir = "Assets";

            var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(dir, "LocalizationTable.json").Replace('\\', '/'));
            File.WriteAllText(path, "{}");
            AssetDatabase.ImportAsset(path);

            var so = new SerializedObject(config);
            so.FindProperty("table").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            so.ApplyModifiedProperties();
            return path;
        }
    }

    public sealed class SpriteLocalizationAdapter : ILocalizationEntryAdapter<Sprite>
    {
        private readonly Func<bool> _getSetNativeSize;

        public SpriteLocalizationAdapter(Func<bool> getSetNativeSize = null)
        {
            _getSetNativeSize = getSetNativeSize;
        }

        public bool Exists(LocalizationConfig config, string key)
        {
            return config.SpriteEntries.Any(e => e.Key == key);
        }

        public void CreateKey(LocalizationConfig config, string key, IReadOnlyDictionary<string, Sprite> values)
        {
            EnsureLanguages(config);
            var entry = config.SpriteEntries.Find(e => e.Key == key);
            if (entry == null)
            {
                entry = new LocalizationSpriteEntry { Key = key };
                config.SpriteEntries.Add(entry);
            }

            foreach (var lang in config.Languages)
            {
                var cell = GetOrCreateCell(entry, lang);
                cell.Sprite = values != null && values.TryGetValue(lang, out var sp) ? sp : null;
            }

            SaveConfig(config);
        }

        public Sprite GetValue(LocalizationConfig config, string key, string language)
        {
            EnsureLanguages(config);
            var entry = config.SpriteEntries.Find(e => e.Key == key);
            if (entry == null)
                return null;

            return GetOrCreateCell(entry, language).Sprite;
        }

        public void SetValue(LocalizationConfig config, string key, string language, Sprite value)
        {
            EnsureLanguages(config);
            var entry = config.SpriteEntries.Find(e => e.Key == key);
            if (entry == null)
                return;

            GetOrCreateCell(entry, language).Sprite = value;
            SaveConfig(config);
        }

        public void ApplyPreview(Component targetComponent, LocalizationConfig config, string key)
        {
            if (targetComponent == null)
                return;

            var img = targetComponent.GetComponent<Image>();
            if (img == null)
                return;

            var sp = ResolveSprite(config, config.DefaultLanguage, key);
            if (sp != null)
            {
                img.sprite = sp;
                if (_getSetNativeSize != null && _getSetNativeSize())
                    img.SetNativeSize();
                EditorUtility.SetDirty(img);
            }
        }

        private static Sprite ResolveSprite(LocalizationConfig config, string defaultLang, string key)
        {
            var entry = config.SpriteEntries.Find(e => e.Key == key);
            if (entry == null)
                return null;

            var defVal = entry.Values.Find(v => v.Language == defaultLang);
            if (defVal != null && defVal.Sprite != null)
                return defVal.Sprite;

            var anyVal = entry.Values.Find(v => v.Sprite != null);
            return anyVal?.Sprite;
        }

        private static void EnsureLanguages(LocalizationConfig config)
        {
            foreach (var entry in config.SpriteEntries)
            {
                foreach (var lang in config.Languages)
                    GetOrCreateCell(entry, lang);
            }
        }

        private static LocalizationSpriteValue GetOrCreateCell(LocalizationSpriteEntry entry, string lang)
        {
            var cell = entry.Values.Find(v => v.Language == lang);
            if (cell == null)
            {
                cell = new LocalizationSpriteValue { Language = lang, Sprite = null };
                entry.Values.Add(cell);
            }
            return cell;
        }

        private static void SaveConfig(LocalizationConfig config)
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }

    [CustomEditor(typeof(LocalizedText))]
    public sealed class LocalizedTextEditor : BaseLocalizerEditor<ITextProvider, string>
    {
        private readonly TextLocalizationAdapter _adapter = new TextLocalizationAdapter();

        protected override ILocalizationEntryAdapter<string> Adapter => _adapter;

        protected override string GetDefaultDraftValue() => "";

        protected override string DrawValueField(string language, string currentValue)
        {
            return EditorGUILayout.TextField(language, currentValue);
        }
    }

    [CustomEditor(typeof(LocalizedImage))]
    public sealed class LocalizedImageEditor : BaseLocalizerEditor<ISpriteProvider, Sprite>
    {
        private SerializedProperty _setNativeSizeProp;
        private SpriteLocalizationAdapter _adapter;

        protected override ILocalizationEntryAdapter<Sprite> Adapter
        {
            get
            {
                if (_adapter == null)
                    _adapter = new SpriteLocalizationAdapter(() => _setNativeSizeProp != null && _setNativeSizeProp.boolValue);
                return _adapter;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _setNativeSizeProp = serializedObject.FindProperty("setNativeSize");
        }

        protected override void DrawExtraFields()
        {
            if (_setNativeSizeProp != null)
                EditorGUILayout.PropertyField(_setNativeSizeProp);
        }

        protected override Sprite DrawValueField(string language, Sprite currentValue)
        {
            return (Sprite)EditorGUILayout.ObjectField(language, currentValue, typeof(Sprite), false, GUILayout.Height(48));
        }
    }
}
#endif