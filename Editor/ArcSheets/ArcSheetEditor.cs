using System;
using System.Linq;
using ArcSheets;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ArcSheet))]
public class ArcSheetEditor : Editor
{
    private string[] _typeNames;
    private int _index = -1;

    private void OnEnable()
    {
        _typeNames = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName.StartsWith("Assembly-CSharp"))
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                typeof(ScriptableObject).IsAssignableFrom(t) &&
                t != typeof(ScriptableObject) &&
                !t.IsAbstract)
            .OrderBy(t => t.FullName)
            .Select(t => t.FullName)
            .ToArray();

        ArcSheet sheet = (ArcSheet)target;
        _index = Array.IndexOf(_typeNames, sheet.type.FullName);
    }

    public override void OnInspectorGUI()
    {
        ArcSheet sheet = (ArcSheet)target;

        EditorGUILayout.LabelField("Target Type");

        int newIndex = EditorGUILayout.Popup(_index, _typeNames);
        if (newIndex != _index)
        {
            _index = newIndex;
            sheet.type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == _typeNames[_index]);

            sheet.entries.Clear();
            EditorUtility.SetDirty(sheet);
        }

        if (sheet.type != null && GUILayout.Button("Open Sheet Editor"))
        {
            ArcSheetWindow.Open(sheet);
        }
    }
}
