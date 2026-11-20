using System.Collections.Generic;
using System.IO;
using Localization.Data;
using Localization.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Localization.EditorTools.Localizer
{
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
}