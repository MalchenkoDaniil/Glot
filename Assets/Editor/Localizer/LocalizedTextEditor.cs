using Localization.Abstractions;
using Localization.Components;
using UnityEditor;

namespace Localization.EditorTools.Localizer
{
    [CustomEditor(typeof(LocalizedText))]
    public sealed class LocalizedTextEditor : BaseLocalizerEditor<ITextProvider, string>
    {
        private readonly TextLocalizationAdapter _adapter = new TextLocalizationAdapter();

        protected override ILocalizationEntryAdapter<string> Adapter => _adapter;

        protected override string GetDefaultDraftValue() => "";

        protected override string DrawValueField(string language, string currentValue)
        {
            return EditorGUILayout.TextField(language, currentValue);
        }
    }
}