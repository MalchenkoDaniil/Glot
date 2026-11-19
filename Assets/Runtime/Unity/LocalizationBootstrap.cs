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
                Debug.LogError("[Localization] Config is null.", this);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void OnDestroy()
        {
            if (_service == null)
                return;

            LocalizationLocator.Unregister(_service);
            _service = null;
        }

        private void Initialize()
        {
            var tableLoader = new TextAssetTableLoader(config.Table);
            // var storage = new PlayerPrefsLanguageStorage();

            _service = new LocalizationService(config, tableLoader/*, storage*/);
            LocalizationLocator.Register(_service);
        }
    }
}