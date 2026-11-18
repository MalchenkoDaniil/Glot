using Localization.Abstractions;
using Localization.Core;
using TMPro;
using UnityEngine;

namespace Localization.Components
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : BaseLocalizer<ITextProvider>
    {
        private TMP_Text _text;

        protected override void Apply(ITextProvider provider)
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
            _text.text = provider.GetText(localizationKey);
        }
    }
}
