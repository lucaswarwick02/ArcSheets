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
        _typeNames = TypeExtensions.ListUserTypes().Select(t => t.FullName).ToArray();

        ArcSheet sheet = (ArcSheet)target;

        if (sheet.type == null)
        {
            _index = -1;
        }
        else
        {
            _index = Array.IndexOf(_typeNames, sheet.type);
        }
    }

    public override void OnInspectorGUI()
    {
        ArcSheet sheet = (ArcSheet)target;

        EditorGUILayout.LabelField("Target Type");

        int newIndex = EditorGUILayout.Popup(_index, _typeNames);
        if (newIndex != _index)
        {
            _index = newIndex;
            sheet.type = _typeNames[_index];

            sheet.entries.Clear();
            EditorUtility.SetDirty(sheet);
        }

        if (sheet.type != null && GUILayout.Button("Open Sheet Editor"))
        {
            ArcSheetWindow.Open(sheet);
        }
    }
}
