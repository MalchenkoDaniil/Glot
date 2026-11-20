using Localization.Abstractions;
using Localization.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Localization.Components
{
    [RequireComponent(typeof(Image))]
    public class LocalizedImage : BaseLocalizer<ISpriteProvider>
    {
        private Image _image;

        [SerializeField] private bool setNativeSize;

        protected override void Apply(ISpriteProvider provider)
        {
            var sprite = provider.GetSprite(localizationKey);

            if (sprite == null)
                return;

            if (_image == null)
                _image = GetComponent<Image>();

            _image.sprite = sprite;

            if (setNativeSize)
                _image.SetNativeSize();
        }
    }
}