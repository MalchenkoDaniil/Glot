using Localization.Core;
using UnityEngine;

namespace Localization.Unity
{
    public static class LocalizationLocator
    {
        public static LocalizationService Current { get; private set; }

        public static void Register(LocalizationService service) => Current = service;

        public static void Unregister(LocalizationService service)
        {
            if (Current == service) Current = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Current = null;
    }
}
