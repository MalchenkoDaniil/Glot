using Localization.Data;
using UnityEngine;

namespace Localization.Abstractions
{
    public sealed class TextAssetTableLoader
    {
        private readonly TextAsset _asset;

        public TextAssetTableLoader(TextAsset asset) { _asset = asset; }

        public LocalizationTextTable Load()
        {
            if (_asset == null)
            {
                Debug.LogError("Localization: в конфиге не назначен JSON с переводами");
                return new LocalizationTextTable();
            }
            return JsonUtility.FromJson<LocalizationTextTable>(_asset.text) ?? new LocalizationTextTable();
        }
    }
}
