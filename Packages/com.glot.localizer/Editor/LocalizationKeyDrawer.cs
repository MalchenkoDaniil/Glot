using System.Collections.Generic;
using System.Linq;
using Localization.Abstractions;
using Localization.Components;
using UnityEditor;
using UnityEngine;

namespace Localization.EditorTools
{
    [CustomPropertyDrawer(typeof(LocalizationKeyAttribute))]
    public class LocalizationKeyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
        {
            var field = new Rect(rect.x, rect.y, rect.width - 24, rect.height);
            var button = new Rect(field.xMax + 2, rect.y, 22, rect.height);

            EditorGUI.PropertyField(field, prop, label);

            if (!GUI.Button(button, "▾"))
                return;

            var so = prop.serializedObject;
            var path = prop.propertyPath;
            var current = prop.stringValue;
            var menu = new GenericMenu();
            bool any = false;

            foreach (var key in ResolveKeysForTarget(so.targetObject))
            {
                any = true;
                var k = key;

                menu.AddItem(new GUIContent(k.Replace('.', '/')), k == current, () =>
                {
                    so.Update();
                    so.FindProperty(path).stringValue = k;
                    so.ApplyModifiedProperties();
                });
            }

            if (!any)
                menu.AddDisabledItem(new GUIContent("No keys — Tools/Localization Table Editor"));

            menu.ShowAsContext();
        }

        private static IEnumerable<string> ResolveKeysForTarget(Object targetObject)
        {
            if (targetObject is LocalizedImage)
                return LocalizationEditorUtil.GetAllSpriteKeys();

            if (targetObject is LocalizedText)
                return LocalizationEditorUtil.GetAllTextKeys();

            return LocalizationEditorUtil.GetAllTextKeys()
                .Concat(LocalizationEditorUtil.GetAllSpriteKeys())
                .Distinct();
        }
    }
}