using UnityEngine;

namespace Localization.Abstractions
{
    public interface ISpriteProvider : ILocalizationEvents
    {
        Sprite GetSprite(string key);
    }
}
