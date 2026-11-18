using System.Collections.Generic;
using Localization.Data;

namespace Localization.Core
{
    internal sealed class TextStore
    {
        private readonly Dictionary<string, Dictionary<string, string>> _texts =
            new Dictionary<string, Dictionary<string, string>>();

        public TextStore(LocalizationTable table)
        {
            foreach (var entry in table.Entries)
            {
                if (string.IsNullOrEmpty(entry.Key)) 
                    continue;
                
                foreach (var value in entry.Values)
                {
                    if (string.IsNullOrEmpty(value.Language)) 
                        continue;
                    
                    if (!_texts.TryGetValue(value.Language, out var dict))
                        _texts[value.Language] = dict = new Dictionary<string, string>();
                    
                    dict[entry.Key] = value.Text;
                }
            }
        }

        public bool TryGet(string language, string key, out string text)
        {
            text = null;
            
            return language != null && key != null
                   && _texts.TryGetValue(language, out var dict)
                   && dict.TryGetValue(key, out text)
                   && !string.IsNullOrEmpty(text);
        }
    }
}
