using System.Collections.Generic;
using Localization.Data;

namespace Localization.EditorTools
{
    public static class LocalizationTextExtensions
    {
        public static LocalizationTextData Find(this LocalizationTextTable textTable, string key)
            => textTable.Entries.Find(e => e.Key == key);

        public static LocalizationTextData Add(this LocalizationTextTable textTable, string key)
        {
            var entry = new LocalizationTextData { Key = key };
            textTable.Entries.Add(entry);
            return entry;
        }

        public static LocalizationTextValue Cell(this LocalizationTextData entry, string language)
        {
            var cell = entry.Values.Find(v => v.Language == language);
            
            if (cell == null)
            {
                cell = new LocalizationTextValue { Language = language, Text = "" };
                entry.Values.Add(cell);
            }
            return cell;
        }

        public static void EnsureLanguages(this LocalizationTextTable textTable, IReadOnlyList<string> languages)
        {
            foreach (var entry in textTable.Entries)
                foreach (var language in languages)
                    entry.Cell(language);
        }

        public static void RemoveLanguage(this LocalizationTextTable textTable, string language)
        {
            foreach (var entry in textTable.Entries)
                entry.Values.RemoveAll(v => v.Language == language);
        }
    }
}
