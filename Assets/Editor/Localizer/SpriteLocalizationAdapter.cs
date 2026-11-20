using System;
using System.Collections.Generic;
using System.Linq;
using Localization.Unity;
using Runtime.Data.SpriteData;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Localization.EditorTools.Localizer
{
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
}