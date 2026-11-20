using System.Collections.Generic;
using Localization.Unity;
using UnityEngine;

namespace Localization.EditorTools.Localizer
{
    public interface ILocalizationEntryAdapter<TValue>
    {
        bool Exists(LocalizationConfig config, string key);
        void CreateKey(LocalizationConfig config, string key, IReadOnlyDictionary<string, TValue> values);
        TValue GetValue(LocalizationConfig config, string key, string language);
        void SetValue(LocalizationConfig config, string key, string language, TValue value);
        void ApplyPreview(Component targetComponent, LocalizationConfig config, string key);
    }
}