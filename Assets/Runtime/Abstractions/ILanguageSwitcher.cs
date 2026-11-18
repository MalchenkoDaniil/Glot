using System.Collections.Generic;

namespace Localization.Abstractions
{
    public interface ILanguageSwitcher : ILocalizationEvents
    {
        IReadOnlyList<string> Languages { get; }
        void SetLanguage(string language);
    }
}
