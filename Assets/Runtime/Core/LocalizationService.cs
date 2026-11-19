using System;
using System.Collections.Generic;
using Localization.Abstractions;
using Localization.Unity;
using UnityEngine;

namespace Localization.Core
{
    public sealed class LocalizationService : ITextProvider, ISpriteProvider, ILanguageSwitcher
    {
        private readonly LocalizationConfig _config;
        private readonly TextStore _texts;
        private readonly SpriteStore _sprites;
        // private readonly PlayerPrefsLanguageStorage _storage;

        public event Action LanguageChanged;
        public string CurrentLanguage { get; private set; }
        public IReadOnlyList<string> Languages => _config.Languages;

        public LocalizationService(
            LocalizationConfig config,
            TextAssetTableLoader textAssetTableLoader
            /*PlayerPrefsLanguageStorage storage*/)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            // _storage = storage;

            var table = textAssetTableLoader != null ? textAssetTableLoader.Load() : new Data.LocalizationTextTable();
            _texts = new TextStore(table);
            _sprites = new SpriteStore(_config.SpriteEntries);

            CurrentLanguage = ResolveStartLanguage();
        }

        public void SetLanguage(string language)
        {
            if (language == CurrentLanguage)
                return;

            if (!IsSupported(language))
            {
                Debug.LogWarning($"[Localization] Language '{language}' is not supported.");
                return;
            }

            CurrentLanguage = language;
            // _storage?.Save(language);

            LanguageChanged?.Invoke();
        }

        public string GetText(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            if (_texts.TryGet(CurrentLanguage, key, out var text))
                return text;

            if (CurrentLanguage != _config.DefaultLanguage &&
                _texts.TryGet(_config.DefaultLanguage, key, out text))
                return text;

            return key;
        }

        public Sprite GetSprite(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            if (_sprites.TryGet(CurrentLanguage, key, out var sprite))
                return sprite;

            if (CurrentLanguage != _config.DefaultLanguage &&
                _sprites.TryGet(_config.DefaultLanguage, key, out sprite))
                return sprite;

            return null;
        }

        private string ResolveStartLanguage()
        {
            // var saved = _storage?.Load();
            var saved = _config.DefaultLanguage;
            
            if (IsSupported(saved))
                return saved;

            var system = SystemLanguageCodes.From(Application.systemLanguage);
            if (IsSupported(system))
                return system;

            if (IsSupported(_config.DefaultLanguage))
                return _config.DefaultLanguage;

            return _config.Languages.Count > 0 ? _config.Languages[0] : "en";
        }

        private bool IsSupported(string language)
        {
            if (string.IsNullOrEmpty(language))
                return false;

            for (int i = 0; i < _config.Languages.Count; i++)
            {
                if (_config.Languages[i] == language)
                    return true;
            }

            return false;
        }
    }
}