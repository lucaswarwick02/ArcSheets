using ScriptableObjectTables;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dockable editor window for displaying and managing ScriptableObjectTable entries in a persistent tab.
/// </summary>
public class ScriptableObjectTableWindow : EditorWindow
{
    private ScriptableObjectTable _table;
    private bool _isDirty = false;
    private ScriptableObjectTableView _tableView;
    private readonly GUIContent _windowTitle;

    /// <summary>
    /// Menu item to open the ScriptableObjectTable Editor window from the Window menu.
    /// </summary>
    [MenuItem("Window/ScriptableObjectTable Editor")]
    public static void Open()
    {
        GetWindow<ScriptableObjectTableWindow>("ScriptableObjectTable Editor");
    }

    /// <summary>
    /// Shows the ScriptableObjectTable editor window and loads the specified table.
    /// </summary>
    public static void Open(ScriptableObjectTable table)
    {
        var window = GetWindow<ScriptableObjectTableWindow>("ScriptableObjectTable Editor");
        window.LoadTable(table);
        window.Show();
    }

    private void OnDestroy()
    {
        if (_isDirty && _table != null)
        {
            if (EditorUtility.DisplayDialog("Unsaved Changes", "You have unsaved edits in the ScriptableObjectTable. Do you want to save them?", "Save", "Discard"))
            {
                SaveAllAssets();
            }
        }
    }

    private void LoadTable(ScriptableObjectTable table)
    {
        _table = table;
        _isDirty = false;
        _tableView = null; // Force rebuild

        if (_table != null)
        {
            titleContent = new GUIContent($"ScriptableObjectTable: {_table.name}");
        }
        else
        {
            titleContent = new GUIContent("ScriptableObjectTable: None");
        }
    }

    private void SaveAllAssets()
    {
        if (_table == null)
            return;

        // Mark all entries as dirty
        foreach (var entry in _table.entries)
        {
            if (entry != null)
            {
                EditorUtility.SetDirty(entry);
            }
        }

        EditorUtility.SetDirty(_table);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        _isDirty = false;
    }

    private void OnGUI()
    {
        if (_table == null || _table.typeReference == null)
        {
            EditorGUILayout.HelpBox("No ScriptableObjectTable loaded. Double-click a ScriptableObjectTable asset to open it here.", MessageType.Info);
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

        EditorGUILayout.LabelField($"Editing: {_table.name}", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Add New", GUILayout.Width(100)))
        {
            var newEntry = CreateInstance(_table.typeReference.Type) as SerializedScriptableObject;
            newEntry.name = $"{_table.typeReference.Type.Name}_{_table.entries.Count}";

            AssetDatabase.AddObjectToAsset(newEntry, _table);
            _table.entries.Add(newEntry);

            EditorUtility.SetDirty(_table);
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
            _tableView = new ScriptableObjectTableView(_table);
            _tableView.onDirty += () => _isDirty = true;
        }

        // Draw table
        _tableView.DrawTableGUI();
    }
}
