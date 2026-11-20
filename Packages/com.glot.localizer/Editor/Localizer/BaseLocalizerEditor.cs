#if UNITY_EDITOR
using System.Collections.Generic;
using Localization.Abstractions;
using Localization.Unity;
using UnityEditor;
using UnityEngine;

namespace Localization.EditorTools.Localizer
{
    public abstract class BaseLocalizerEditor<TProvider, TValue> : Editor where TProvider : class, ILocalizationEvents
    {
        private readonly Dictionary<string, TValue> _draftValues = new Dictionary<string, TValue>();
        private SerializedProperty _keyProp;
        private string _draftKey = "";

        protected abstract ILocalizationEntryAdapter<TValue> Adapter { get; }

        protected virtual void OnEnable()
        {
            _keyProp = serializedObject.FindProperty("localizationKey");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_keyProp != null)
                EditorGUILayout.PropertyField(_keyProp);

            DrawExtraFields();

            serializedObject.ApplyModifiedProperties();

            var config = LocalizationEditorUtil.FindConfig();
            string rawKey = _keyProp != null ? _keyProp.stringValue : "";

            if (config == null)
            {
                EditorGUILayout.HelpBox("LocalizationConfig not found.", MessageType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(rawKey))
                return;

            string key = rawKey.Trim();
            var adapter = Adapter;

            if (adapter.Exists(config, key))
                DrawExistingValuesEditor(config, adapter, key);
            else
                DrawCreateKeySection(config, adapter, key);
        }

        protected virtual void DrawExtraFields()
        {
        }

        protected abstract TValue DrawValueField(string language, TValue currentValue);

        protected virtual TValue GetDefaultDraftValue() => default;

        private void DrawExistingValuesEditor(LocalizationConfig config, ILocalizationEntryAdapter<TValue> adapter, string key)
        {
            bool modified = false;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Values ({key})", EditorStyles.miniBoldLabel);

                for (int i = 0; i < config.Languages.Count; i++)
                {
                    string lang = config.Languages[i];
                    TValue current = adapter.GetValue(config, key, lang);

                    EditorGUI.BeginChangeCheck();
                    TValue next = DrawValueField(lang, current);
                    if (EditorGUI.EndChangeCheck())
                    {
                        adapter.SetValue(config, key, lang, next);
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                adapter.ApplyPreview((Component)target, config, key);
                LocalizationEditorEvents.RaiseTableChanged();
            }
        }

        private void DrawCreateKeySection(LocalizationConfig config, ILocalizationEntryAdapter<TValue> adapter, string key)
        {
            SyncDraft(config, key);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox($"Key \"{key}\" does not exist.", MessageType.Info);

                if (GUILayout.Button("Create Key", GUILayout.Height(24)))
                {
                    adapter.CreateKey(config, key, _draftValues);
                    ClearDraft();
                    adapter.ApplyPreview((Component)target, config, key);
                    LocalizationEditorEvents.RaiseTableChanged();
                    GUI.FocusControl(null);
                }
            }
        }

        private void SyncDraft(LocalizationConfig config, string key)
        {
            if (_draftKey == key)
                return;

            _draftKey = key;
            _draftValues.Clear();

            for (int i = 0; i < config.Languages.Count; i++)
                _draftValues[config.Languages[i]] = GetDefaultDraftValue();
        }

        private void ClearDraft()
        {
            _draftKey = "";
            _draftValues.Clear();
        }
    }
}
#endif