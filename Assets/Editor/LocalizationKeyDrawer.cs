using Localization.Abstractions;
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

            foreach (var key in LocalizationEditorUtil.GetAllKeys())
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
                menu.AddDisabledItem(new GUIContent("No keys — Tools/Localization Text Table"));
            
            menu.ShowAsContext();
        }
    }
}
