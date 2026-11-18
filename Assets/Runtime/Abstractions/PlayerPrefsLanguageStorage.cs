using UnityEngine;

namespace Localization.Abstractions
{
    public sealed class PlayerPrefsLanguageStorage
    {
        private const string PrefKey = "Localization.Language";

        public string Load() => PlayerPrefs.GetString(PrefKey, "");
        public void Save(string language) => PlayerPrefs.SetString(PrefKey, language);
    }
}
