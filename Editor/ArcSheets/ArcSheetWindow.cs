using System;
using ArcSheets;
using UnityEditor;
using UnityEngine;

public class ArcSheetWindow : EditorWindow
{
    private ArcSheet _sheet;
    private Vector2 _scroll;
    private ScriptableObject _selectedEntry;

    public static void Open(ArcSheet sheet)
    {
        var window = GetWindow<ArcSheetWindow>(sheet.name);
        window._sheet = sheet;
        window.Show();

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.arcadian.ArcSheets/Editor/Icons/sample.png");
        EditorGUIUtility.SetIconForObject(sheet, icon);
    }

    private void OnGUI()
    {
        if (_sheet == null || _sheet.type == null)
        {
            EditorGUILayout.HelpBox("Sheet or sheet.type is not valid.", MessageType.Error);
            return;
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Add new Entry", EditorStyles.toolbarButton))
        {
            var entry = CreateInstance(_sheet.type);
            _sheet.entries.Add(entry);
            AssetDatabase.AddObjectToAsset(entry, _sheet);
            EditorUtility.SetDirty(_sheet);
            EditorUtility.SetDirty(entry);
            AssetDatabase.SaveAssets();
        }

        using (new EditorGUI.DisabledScope(_selectedEntry == null))
        {
            if (GUILayout.Button("Removed Selected", EditorStyles.toolbarButton))
            {
                _sheet.entries.Remove(_selectedEntry);
                DestroyImmediate(_selectedEntry, true);
                _selectedEntry = null;
                AssetDatabase.SaveAssets();
            }
        }

        EditorGUILayout.EndHorizontal();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (var entry in _sheet.entries)
        {
            if (entry == null)
            {
                continue;
            }

            GUI.backgroundColor = entry == _selectedEntry ? Color.cyan : Color.white;
            EditorGUILayout.BeginVertical("box");

            if (GUILayout.Button(entry.name ?? entry.GetType().Name, EditorStyles.boldLabel))
            {
                _selectedEntry = entry;
            }

            Editor editor = Editor.CreateEditor(entry);
            editor.OnInspectorGUI();

            EditorGUILayout.EndVertical();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndScrollView();
    }
}