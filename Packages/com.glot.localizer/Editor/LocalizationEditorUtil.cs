using System.Collections.Generic;
using Localization.Data;
using Localization.Unity;
using UnityEditor;
using UnityEngine;

namespace Localization.EditorTools
{
    public static class LocalizationEditorUtil
    {
        public static LocalizationConfig FindConfig()
        {
            var guids = AssetDatabase.FindAssets("t:LocalizationConfig");
            return guids.Length == 0
                ? null
                : AssetDatabase.LoadAssetAtPath<LocalizationConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        public static LocalizationTextTable LoadTable(LocalizationConfig config)
        {
            if (config == null || config.Table == null)
                return new LocalizationTextTable();

            return JsonUtility.FromJson<LocalizationTextTable>(config.Table.text) ?? new LocalizationTextTable();
        }

        public static IEnumerable<string> GetAllTextKeys()
        {
            foreach (var entry in LoadTable(FindConfig()).Entries)
                if (!string.IsNullOrEmpty(entry.Key)) yield return entry.Key;
        }

        public static IEnumerable<string> GetAllSpriteKeys()
        {
            var config = FindConfig();
            if (config == null || config.SpriteEntries == null)
                yield break;

            foreach (var entry in config.SpriteEntries)
                if (entry != null && !string.IsNullOrEmpty(entry.Key)) yield return entry.Key;
        }

        public static IEnumerable<string> GetAllKeys() => GetAllTextKeys();
    }
}