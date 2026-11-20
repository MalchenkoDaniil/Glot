using Localization.Abstractions;
using Localization.Components;
using UnityEditor;
using UnityEngine;

namespace Localization.EditorTools.Localizer
{
    [CustomEditor(typeof(LocalizedImage))]
    public sealed class LocalizedImageEditor : BaseLocalizerEditor<ISpriteProvider, Sprite>
    {
        private SerializedProperty _setNativeSizeProp;
        private SpriteLocalizationAdapter _adapter;

        protected override ILocalizationEntryAdapter<Sprite> Adapter
        {
            get
            {
                if (_adapter == null)
                    _adapter = new SpriteLocalizationAdapter(() => _setNativeSizeProp != null && _setNativeSizeProp.boolValue);
                return _adapter;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _setNativeSizeProp = serializedObject.FindProperty("setNativeSize");
        }

        protected override void DrawExtraFields()
        {
            if (_setNativeSizeProp != null)
                EditorGUILayout.PropertyField(_setNativeSizeProp);
        }

        protected override Sprite DrawValueField(string language, Sprite currentValue)
        {
            return (Sprite)EditorGUILayout.ObjectField(language, currentValue, typeof(Sprite), false, GUILayout.Height(48));
        }
    }
}