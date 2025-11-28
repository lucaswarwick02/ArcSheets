using ArcSheets;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dockable editor window for displaying and managing ArcSheet entries in a persistent tab.
/// </summary>
public class ArcSheetWindow : EditorWindow
{
    private ArcSheet _currentSheet;
    private bool _isDirty = false;
    private ArcSheetTableView _tableView;
    private GUIContent _windowTitle;

    /// <summary>
    /// Menu item to open the ArcSheet Editor window from the Window menu.
    /// </summary>
    [MenuItem("Window/ArcSheet Editor")]
    public static void Open()
    {
        GetWindow<ArcSheetWindow>("ArcSheet Editor");
    }

    /// <summary>
    /// Shows the ArcSheet editor window and loads the specified sheet.
    /// </summary>
    public static void Open(ArcSheet sheet)
    {
        var window = GetWindow<ArcSheetWindow>("ArcSheet Editor");
        window.LoadSheet(sheet);
        window.Show();
    }

    private void OnDestroy()
    {
        if (_isDirty && _currentSheet != null)
        {
            if (EditorUtility.DisplayDialog("Unsaved Changes", "You have unsaved edits in the ArcSheet. Do you want to save them?", "Save", "Discard"))
            {
                SaveAllAssets();
            }
        }
    }

    private void LoadSheet(ArcSheet sheet)
    {
        _currentSheet = sheet;
        _isDirty = false;
        _tableView = null; // Force rebuild
        
        if (sheet != null)
        {
            titleContent = new GUIContent($"ArcSheet: {sheet.name}");
        }
    }

    private void SaveAllAssets()
    {
        if (_currentSheet == null)
            return;

        // Mark all entries as dirty
        foreach (var entry in _currentSheet.entries)
        {
            if (entry != null)
            {
                EditorUtility.SetDirty(entry);
            }
        }

        EditorUtility.SetDirty(_currentSheet);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        _isDirty = false;
    }

    private void OnGUI()
    {
        if (_currentSheet == null || _currentSheet.typeReference == null)
        {
            EditorGUILayout.HelpBox("No ArcSheet loaded. Double-click an ArcSheet asset to open it here.", MessageType.Info);
            return;
        }

        // Handle keyboard shortcuts
        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.KeyDown && currentEvent.control && currentEvent.keyCode == KeyCode.S)
        {
            if (_isDirty)
            {
                SaveAllAssets();
            }
            currentEvent.Use();
        }

        GUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField($"Editing: {_currentSheet.name}", EditorStyles.boldLabel);
        
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Add New", GUILayout.Width(100)))
        {
            var newEntry = CreateInstance(_currentSheet.typeReference.Type);
            newEntry.name = $"{_currentSheet.typeReference.Type.Name}_{_currentSheet.entries.Count}";

            AssetDatabase.AddObjectToAsset(newEntry, _currentSheet);
            _currentSheet.entries.Add(newEntry);

            EditorUtility.SetDirty(_currentSheet);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (_tableView != null)
            {
                _tableView.Rebuild();
            }
        }

        // Save button
        GUI.backgroundColor = _isDirty ? Color.yellow : Color.white;
        if (GUILayout.Button("Save", GUILayout.Width(100)))
        {
            SaveAllAssets();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Initialize table view if needed
        if (_tableView == null)
        {
            _tableView = new ArcSheetTableView(_currentSheet);
            _tableView.onDirty += () => _isDirty = true;
        }

        // Draw table
        _tableView.DrawTableGUI();
    }
}
