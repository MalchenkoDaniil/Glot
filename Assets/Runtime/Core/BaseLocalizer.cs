using Localization.Abstractions;
using Localization.Unity;
using UnityEngine;

namespace Localization.Core
{
    public abstract class BaseLocalizer<TProvider> : MonoBehaviour where TProvider : class, ILocalizationEvents
    {
        protected TProvider Provider { get; private set; }
        
        [SerializeField, LocalizationKey] protected string localizationKey;
        
        // [Inject]
        /*public*/ void Construct(TProvider provider)
        {
            if (isActiveAndEnabled) 
                Unsubscribe();
            
            Provider = provider;
            
            if (isActiveAndEnabled) 
                Subscribe();
        }

        protected virtual void OnEnable()
        {
            // For test
            Construct(LocalizationLocator.Current as  TProvider);
            
            if (Provider == null)
            {
                Debug.LogError($"[{GetType().Name}] Localization non initialized", this);
                return;
            }
            
            Subscribe();
        }

        protected virtual void OnDisable() => Unsubscribe();

        public void SetKey(string newKey)
        {
            localizationKey = newKey;
            Refresh();
        }

        public void Refresh()
        {
            if (Provider == null || string.IsNullOrEmpty(localizationKey)) return;
            Apply(Provider);
        }

        protected abstract void Apply(TProvider provider);

        private void Subscribe()
        {
            if (Provider == null) 
                return;
            
            Provider.LanguageChanged += Refresh;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (Provider != null) 
                Provider.LanguageChanged -= Refresh;
        }
    }
}
