using Localization.Unity;
using UnityEditor;
using UnityEngine;

namespace Localization.EditorTools
{
    [CustomEditor(typeof(LocalizationConfig))]
    public class LocalizationConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Open Table Editor", GUILayout.Height(28)))
                LocalizationTableWindow.Open((LocalizationConfig)target);
        }
    }
}
