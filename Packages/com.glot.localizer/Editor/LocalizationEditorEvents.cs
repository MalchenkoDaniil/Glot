#if UNITY_EDITOR
using System;

namespace Localization.EditorTools
{
    public static class LocalizationEditorEvents
    {
        public static event Action TableChanged;

        public static void RaiseTableChanged() => TableChanged?.Invoke();
    }
}
#endif