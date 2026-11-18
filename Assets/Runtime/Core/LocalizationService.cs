using System;
using System.Collections.Generic;
using Localization.Abstractions;
using Runtime.Abstractions;
using UnityEngine;

namespace Localization.Core
{
    public sealed class LocalizationService :  ITextProvider, ISpriteProvider, ILanguageSwitcher
    {
        private readonly ILocalizationSettings _settings;
        private readonly TextStore _texts;
        private readonly ResourcesSpriteLoader _resourcesSprites;
        private readonly PlayerPrefsLanguageStorage _storage;

        public event Action LanguageChanged;
        public string CurrentLanguage { get; private set; }
        public IReadOnlyList<string> Languages => _settings.Languages;

        public LocalizationService(ILocalizationSettings settings, TextAssetTableLoader textAssetTableLoader,
            ResourcesSpriteLoader resourcesSpriteLoader, PlayerPrefsLanguageStorage storage)
        {
            _settings = settings;
            _resourcesSprites = resourcesSpriteLoader;
            _storage = storage;
            
            _texts = new TextStore(textAssetTableLoader.Load());
            
            CurrentLanguage = ResolveStartLanguage();
        }

        public void SetLanguage(string language)
        {
            if (language == CurrentLanguage) 
                return;
            
            if (!IsSupported(language))
            {
                Debug.LogWarning($"Localization: language '{language}' not supported.");
                return;
            }

            CurrentLanguage = language;
            _storage.Save(language);
            
            LanguageChanged?.Invoke();
        }

        public string GetText(string key)
        {
            if (_texts.TryGet(CurrentLanguage, key, out var text)) 
                return text;
            
            if (CurrentLanguage != _settings.DefaultLanguage &&
                _texts.TryGet(_settings.DefaultLanguage, key, out text)) 
                return text;
            
            return key; 
        }

        public Sprite GetSprite(string key)
        {
            var sprite = _resourcesSprites.Load(CurrentLanguage, key);
            
            if (sprite == null && CurrentLanguage != _settings.DefaultLanguage)
                sprite = _resourcesSprites.Load(_settings.DefaultLanguage, key);
            
            return sprite;
        }

        private string ResolveStartLanguage()
        {
            var saved = _storage.Load();
            if (IsSupported(saved)) return saved;

            var system = SystemLanguageCodes.From(Application.systemLanguage);
            return IsSupported(system) ? system : _settings.DefaultLanguage;
        }

        private bool IsSupported(string language)
        {
            if (string.IsNullOrEmpty(language)) return false;
            for (int i = 0; i < _settings.Languages.Count; i++)
                if (_settings.Languages[i] == language) return true;
            return false;
        }
    }
}
