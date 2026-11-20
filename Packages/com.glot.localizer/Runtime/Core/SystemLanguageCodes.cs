using System.Collections.Generic;
using UnityEngine;

namespace Localization.Core
{
    public static class SystemLanguageCodes
    {
        private static readonly string[] SupportedCodes =
        {
            "en", "ru", "uk", "be", "de", "fr", "es", "pt", "it", "tr", "pl", "ja", "ko", "zh"
        };

        public static IReadOnlyList<string> AllCodes => SupportedCodes;

        public static bool IsKnownCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            for (int i = 0; i < SupportedCodes.Length; i++)
            {
                if (SupportedCodes[i] == code)
                    return true;
            }

            return false;
        }

        public static string From(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Russian: return "ru";
                case SystemLanguage.Ukrainian: return "uk";
                case SystemLanguage.Belarusian: return "be";
                case SystemLanguage.German: return "de";
                case SystemLanguage.French: return "fr";
                case SystemLanguage.Spanish: return "es";
                case SystemLanguage.Portuguese: return "pt";
                case SystemLanguage.Italian: return "it";
                case SystemLanguage.Turkish: return "tr";
                case SystemLanguage.Polish: return "pl";
                case SystemLanguage.Japanese: return "ja";
                case SystemLanguage.Korean: return "ko";
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified: return "zh";
                default: return "en";
            }
        }
    }
}