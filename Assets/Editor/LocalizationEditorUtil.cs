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

        public static LocalizationTable LoadTable(LocalizationConfig config)
        {
            if (config == null || config.Table == null) 
                return new LocalizationTable();
            
            return JsonUtility.FromJson<LocalizationTable>(config.Table.text) ?? new LocalizationTable();
        }

        public static IEnumerable<string> GetAllKeys()
        {
            foreach (var entry in LoadTable(FindConfig()).Entries)
                if (!string.IsNullOrEmpty(entry.Key)) yield return entry.Key;
        }
    }
}
