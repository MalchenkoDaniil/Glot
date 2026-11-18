using Localization.Data;
using UnityEngine;

namespace Localization.Abstractions
{
    public sealed class TextAssetTableLoader
    {
        private readonly TextAsset _asset;

        public TextAssetTableLoader(TextAsset asset) { _asset = asset; }

        public LocalizationTable Load()
        {
            if (_asset == null)
            {
                Debug.LogError("Localization: в конфиге не назначен JSON с переводами");
                return new LocalizationTable();
            }
            return JsonUtility.FromJson<LocalizationTable>(_asset.text) ?? new LocalizationTable();
        }
    }
}
