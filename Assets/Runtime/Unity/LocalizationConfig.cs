using System.Collections.Generic;
using Runtime.Abstractions;
using UnityEngine;

namespace Localization.Unity
{
    [CreateAssetMenu(menuName = "Configs/LocalizationConfig", fileName = "LocalizationConfig")]
    public class LocalizationConfig : ScriptableObject, ILocalizationSettings
    {
        [SerializeField] private TextAsset table;
        [SerializeField] private string defaultLanguage = "en";
        [SerializeField] private List<string> languages = new List<string> { "en", "ru" };

        public TextAsset Table => table;
        public string DefaultLanguage => defaultLanguage;
        public IReadOnlyList<string> Languages => languages;
    }
}
