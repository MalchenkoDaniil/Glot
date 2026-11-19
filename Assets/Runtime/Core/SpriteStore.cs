using System.Collections.Generic;
using Localization.Unity;
using Runtime.Data.SpriteData;
using UnityEngine;

namespace Localization.Core
{
    internal sealed class SpriteStore
    {
        private readonly Dictionary<string, Dictionary<string, Sprite>> _sprites =
            new Dictionary<string, Dictionary<string, Sprite>>();

        public SpriteStore(IEnumerable<LocalizationSpriteEntry> entries)
        {
            if (entries == null)
                return;

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Key) || entry.Values == null)
                    continue;

                foreach (var value in entry.Values)
                {
                    if (value == null || string.IsNullOrEmpty(value.Language) || value.Sprite == null)
                        continue;

                    if (!_sprites.TryGetValue(value.Language, out var dict))
                        _sprites[value.Language] = dict = new Dictionary<string, Sprite>();

                    dict[entry.Key] = value.Sprite;
                }
            }
        }

        public bool TryGet(string language, string key, out Sprite sprite)
        {
            sprite = null;

            return language != null && key != null
                                    && _sprites.TryGetValue(language, out var dict)
                                    && dict.TryGetValue(key, out sprite)
                                    && sprite != null;
        }
    }
}