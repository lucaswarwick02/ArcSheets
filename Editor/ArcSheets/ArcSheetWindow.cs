using System;
using System.IO;
using ArcSheets;
using UnityEditor;
using UnityEngine;

public class ArcSheetWindow : EditorWindow
{
    ArcSheet sheet;
    Vector2 scroll;

    public static void Open(ArcSheet sheet)
    {
        var window = GetWindow<ArcSheetWindow>(sheet.name);
        window.sheet = sheet;
        window.Show();
    }

    private void OnGUI()
    {
        if (sheet == null || sheet.type == null)
        {
            EditorGUILayout.HelpBox("Sheet or sheet.type is not valid.", MessageType.Error);
            return;
        }

        if (GUILayout.Button("Add New Entry"))
        {
            var entry = CreateInstance(sheet.type);
            entry.name = sheet.type.Name;

            string sheetPath = AssetDatabase.GetAssetPath(sheet);
            string dir = Path.GetDirectoryName(sheetPath);
            string folderName = Path.GetFileNameWithoutExtension(sheetPath);
            string subDir = Path.Combine(dir, folderName);

            if (!AssetDatabase.IsValidFolder(subDir))
            {
                _ = AssetDatabase.CreateFolder(dir, folderName);
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{subDir}/{sheet.type.Name}.asset");

            AssetDatabase.CreateAsset(entry, assetPath);
            AssetDatabase.SaveAssets();

            sheet.entries.Add(entry);
            EditorUtility.SetDirty(sheet);
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var entry in sheet.entries)
        {
            if (entry == null)
            {
                continue;
            }

            EditorGUILayout.BeginVertical("box");
            Editor editor = Editor.CreateEditor(entry);
            editor.OnInspectorGUI();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }
}