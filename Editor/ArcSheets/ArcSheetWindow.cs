using System;
using System.Numerics;
using ArcSheets;
using UnityEditor;

public class ArcSheetWindow : EditorWindow
{
    ArcSheet sheet;

    public static void Open(ArcSheet sheet)
    {
        var window = GetWindow<ArcSheetWindow>(sheet.name);
        window.sheet = sheet;
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox($"Type = {sheet.type}", MessageType.Info);
    }
}