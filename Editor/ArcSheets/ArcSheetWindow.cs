using System.Linq;
using System.Reflection;
using ArcSheets;
using UnityEditor;
using UnityEngine;

public class ArcSheetWindow : EditorWindow
{
    private ArcSheet _sheet;
    private Vector2 _scroll;
    private FieldInfo[] _fieldInfos;
    private ScriptableObject _selectedEntry;

    public static void Open(ArcSheet sheet)
    {
        var window = GetWindow<ArcSheetWindow>(sheet.name);
        window._sheet = sheet;
        window._fieldInfos = sheet.typeReference.Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
            .OrderBy(f => f.MetadataToken)
            .ToArray();
        window.Show();

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.arcadian.ArcSheets/Editor/Icons/sample.png");
        EditorGUIUtility.SetIconForObject(sheet, icon);
    }

    private void OnGUI()
    {
        if (_sheet == null || _sheet.typeReference == null)
        {
            EditorGUILayout.HelpBox("Sheet or sheet.type is not valid.", MessageType.Error);
            return;
        }

        // Header
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Add new Entry", EditorStyles.toolbarButton))
        {
            var entry = CreateInstance(_sheet.typeReference.Type);
            entry.name = _sheet.typeReference.Type.Name;
            _sheet.entries.Add(entry);
            AssetDatabase.AddObjectToAsset(entry, _sheet);
            EditorUtility.SetDirty(_sheet);
            EditorUtility.SetDirty(entry);
            AssetDatabase.SaveAssets();
        }

        // Handle removing an entry
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

        // Table Headers
        RenderTableHeader();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (var entry in _sheet.entries)
        {
            if (entry == null)
            {
                continue;
            }

            SerializedObject so = new(entry);

            // Render the entry as a row
            RenderTableRow(entry, entry.name ?? _sheet.typeReference.Type.Name, entry == _selectedEntry);

            if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                _selectedEntry = entry;
                GUI.FocusControl(null);
            }

        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Render the header of the table to show the field names.
    /// </summary>
    private void RenderTableHeader()
    {
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField("Name", EditorStyles.boldLabel);

        foreach (var field in _fieldInfos)
        {
            EditorGUILayout.LabelField(field.Name, EditorStyles.boldLabel);
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Render an entry (ScriptableObject) as an editable row.
    /// </summary>
    /// <param name="serializedObject">Serialized version of the entry</param>
    /// <param name="scriptableObject">The entry</param>
    /// <param name="props">All properties of the entry</param>
    /// <param name="rowName">Name of the row (name of the entry)</param>
    /// <param name="isSelected">Is the row selected?</param>
    private void RenderTableRow(ScriptableObject entry, string rowName, bool isSelected)
    {
        // Setup types
        SerializedObject so = new(entry);
        var props = _fieldInfos.Select(f => so.FindProperty(f.Name)).ToArray();

        // Start the row
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField(rowName);

        if (isSelected)
        {
            GUI.backgroundColor = Color.gray;
        }

        // For each property...
        foreach (var prop in props)
        {
            if (prop == null)
            {
                continue;
            }

            // Render 
            EditorGUI.BeginChangeCheck();
            prop.isExpanded = false; // suppress foldouts
            _ = EditorGUILayout.PropertyField(prop, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                _ = so.ApplyModifiedProperties();
                EditorUtility.SetDirty(entry);
            }
        }

        GUI.backgroundColor = Color.white;

        // End the row
        EditorGUILayout.EndHorizontal();
    }
}