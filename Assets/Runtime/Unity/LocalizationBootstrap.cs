using Localization.Abstractions;
using Localization.Core;
using UnityEngine;

namespace Localization.Unity
{
    [DefaultExecutionOrder(-100)]
    public sealed class LocalizationBootstrap : MonoBehaviour
    {
        [SerializeField] private LocalizationConfig config;

        private LocalizationService _service;

        private void Awake()
        {
            if (LocalizationLocator.Current != null) 
            { 
                Destroy(gameObject);
                return; 
            }

            if (config == null)
            {
                Debug.LogError("Localization config is null!", this); return;
            }

            _service = new LocalizationService(
                config,
                new TextAssetTableLoader(config.Table),
                new ResourcesSpriteLoader(),
                new PlayerPrefsLanguageStorage());

            LocalizationLocator.Register(_service);
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_service != null) LocalizationLocator.Unregister(_service);
        }
    }
}
