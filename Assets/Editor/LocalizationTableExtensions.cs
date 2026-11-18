using System.Collections.Generic;
using Localization.Data;

namespace Localization.EditorTools
{
    public static class LocalizationTableExtensions
    {
        public static LocalizationData Find(this LocalizationTable table, string key)
            => table.Entries.Find(e => e.Key == key);

        public static LocalizationData Add(this LocalizationTable table, string key)
        {
            var entry = new LocalizationData { Key = key };
            table.Entries.Add(entry);
            return entry;
        }

        public static LocalizationValue Cell(this LocalizationData entry, string language)
        {
            var cell = entry.Values.Find(v => v.Language == language);
            
            if (cell == null)
            {
                cell = new LocalizationValue { Language = language, Text = "" };
                entry.Values.Add(cell);
            }
            return cell;
        }

        public static void EnsureLanguages(this LocalizationTable table, IReadOnlyList<string> languages)
        {
            foreach (var entry in table.Entries)
                foreach (var language in languages)
                    entry.Cell(language);
        }

        public static void RemoveLanguage(this LocalizationTable table, string language)
        {
            foreach (var entry in table.Entries)
                entry.Values.RemoveAll(v => v.Language == language);
        }
    }
}
