using System;

namespace Localization.Abstractions
{
    public interface ILocalizationEvents
    {
        event Action LanguageChanged;
        string CurrentLanguage { get; }
    }
}
