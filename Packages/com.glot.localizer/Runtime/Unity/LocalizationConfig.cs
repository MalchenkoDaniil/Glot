using System.Collections.Generic;
using Runtime.Data.SpriteData;
using UnityEngine;

namespace Localization.Unity
{
    [CreateAssetMenu(menuName = "Configs/LocalizationConfig", fileName = "LocalizationConfig")]
    public class LocalizationConfig : ScriptableObject
    {
        [SerializeField, HideInInspector] private TextAsset table;
        [SerializeField, HideInInspector] private string defaultLanguage = "en";
        [SerializeField, HideInInspector] private List<string> languages = new List<string> { "en", "ru" };
        [SerializeField, HideInInspector] private List<LocalizationSpriteEntry> spriteEntries = new List<LocalizationSpriteEntry>();

        public TextAsset Table => table;
        public string DefaultLanguage => defaultLanguage;
        public IReadOnlyList<string> Languages => languages;
        public List<LocalizationSpriteEntry> SpriteEntries => spriteEntries;
    }
}