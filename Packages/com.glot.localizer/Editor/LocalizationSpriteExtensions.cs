using System.Collections.Generic;
using Runtime.Data.SpriteData;

namespace Localization.EditorTools
{
    public static class LocalizationSpriteExtensions
    {
        public static LocalizationSpriteEntry Find(this List<LocalizationSpriteEntry> entries, string key)
            => entries.Find(e => e.Key == key);

        public static LocalizationSpriteEntry Add(this List<LocalizationSpriteEntry> entries, string key)
        {
            var entry = new LocalizationSpriteEntry { Key = key };
            entries.Add(entry);
            return entry;
        }

        public static LocalizationSpriteValue Cell(this LocalizationSpriteEntry entry, string language)
        {
            var cell = entry.Values.Find(v => v.Language == language);

            if (cell == null)
            {
                cell = new LocalizationSpriteValue { Language = language, Sprite = null };
                entry.Values.Add(cell);
            }
            return cell;
        }

        public static void EnsureLanguages(this List<LocalizationSpriteEntry> entries, IReadOnlyList<string> languages)
        {
            foreach (var entry in entries)
            foreach (var language in languages)
                entry.Cell(language);
        }

        public static void RemoveLanguage(this List<LocalizationSpriteEntry> entries, string language)
        {
            foreach (var entry in entries)
                entry.Values.RemoveAll(v => v.Language == language);
        }
    }
}