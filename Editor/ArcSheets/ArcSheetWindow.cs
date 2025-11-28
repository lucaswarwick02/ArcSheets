using System.Linq;
using System.Reflection;
using ArcSheets;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

/// <summary>
/// Editor window for displaying and managing ArcSheet entries in a tabular format.
/// </summary>
public class ArcSheetWindow : EditorWindow
    {
        private ArcSheet _sheet;
        private Vector2 _scroll;
        private FieldInfo[] _fieldInfos;
        private bool _isDirty = false;

        private MultiColumnHeader multiColumnHeader;
        private MultiColumnHeaderState multiColumnHeaderState;

        private const bool allowDelete = true;    /// <summary>
    /// Open the ArcSheet window for a specific ArcSheet asset.
    /// </summary>
    public static void Open(ArcSheet sheet)
    {
        var window = GetWindow<ArcSheetWindow>(sheet.name);
        window._sheet = sheet;
        window._isDirty = false;
        window._fieldInfos = sheet.typeReference.Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
            .OrderBy(f => f.MetadataToken)
            .ToArray();
        window.Show();
    }

    private void OnDestroy()
    {
        if (_isDirty && _sheet != null)
        {
            if (EditorUtility.DisplayDialog("Unsaved Changes", "You have unsaved edits in the ArcSheet. Do you want to save them?", "Save", "Discard"))
            {
                SaveAllAssets();
            }
        }
    }

    private void SaveAllAssets()
    {
        if (_sheet == null)
            return;

        // Mark all entries as dirty
        foreach (var entry in _sheet.entries)
        {
            if (entry != null)
            {
                EditorUtility.SetDirty(entry);
            }
        }

        EditorUtility.SetDirty(_sheet);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        _isDirty = false;
    }

    private void InitColumnData()
    {
        var columns = new System.Collections.Generic.List<MultiColumnHeaderState.Column>();
        
        // Name/Content column (for custom row rendering or asset name)
        columns.Add(new MultiColumnHeaderState.Column
        {
            headerContent = new GUIContent("Name"),
            width = 250,
            autoResize = true,
        });
        
        // Edit button column
        columns.Add(new MultiColumnHeaderState.Column
        {
            headerContent = new GUIContent("Edit"),
            width = 100,
            autoResize = true,
        });
        
        if (allowDelete)
        {
            columns.Add(new MultiColumnHeaderState.Column                     
            {
                headerContent = new GUIContent("Delete"),
                width = 100,
                autoResize = true,
                canSort = true,
                sortingArrowAlignment = TextAlignment.Right,
                headerTextAlignment = TextAlignment.Left,
            });
        }
        
        multiColumnHeaderState = new MultiColumnHeaderState(columns.ToArray());
        multiColumnHeader = new MultiColumnHeader(multiColumnHeaderState);
    }


    private void OnGUI()
    {
        if (_sheet == null || _sheet.typeReference == null)
        {
            EditorGUILayout.HelpBox("Sheet or sheet.type is not valid.", MessageType.Error);
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
        if (GUILayout.Button("Add New", GUILayout.Width(100)))
        {
            var newEntry = CreateInstance(_sheet.typeReference.Type);
            newEntry.name = $"{_sheet.typeReference.Type.Name}_{_sheet.entries.Count}";

            AssetDatabase.AddObjectToAsset(newEntry, _sheet);
            _sheet.entries.Add(newEntry);

            EditorUtility.SetDirty(_sheet);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Save button
        GUI.backgroundColor = _isDirty ? Color.yellow : Color.white;
        if (GUILayout.Button("Save", GUILayout.Width(100)))
        {
            SaveAllAssets();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndHorizontal();

        // Slight gab between toolbar and the table
        GUILayout.Space(10); // Add slight gap

        // Initialize the columns (this must happen before RenderHeader)
        if (multiColumnHeader == null)
        {
            InitColumnData();
        }

        // Draw the entire header
        if (multiColumnHeader != null)
        {
            RenderHeader(multiColumnHeader);
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        {
            // For each entry... (use ToList() to avoid collection modified exception during deletion)
            foreach (var entry in _sheet.entries.ToList())
            {
                // Skip null/destroyed entries
                if (entry == null)
                    continue;
                    
                // Draw the row
                RenderRow(multiColumnHeader, entry);
            }

        }
        EditorGUILayout.EndScrollView();
    }


    private void RenderHeader(MultiColumnHeader multiColumnHeader)
    {
        Rect headerRect = GUILayoutUtility.GetRect(0, 20);
        
        try
        {
            // Always render dynamic headers for all assets
            // Split the header rect manually without using GetColumnRect
            float editWidth = 100;
            float deleteWidth = 100;
            float buttonsWidth = editWidth + (allowDelete ? deleteWidth : 0);
            
            // Draw dynamic header for the Name column - takes up remaining space after buttons
            Rect nameHeaderRect = new Rect(headerRect.x, headerRect.y, headerRect.width - buttonsWidth, headerRect.height);
            RenderDynamicRowHeader(_sheet.typeReference.Type, nameHeaderRect);

            // Draw Edit header
            Rect editHeaderRect = new Rect(nameHeaderRect.xMax, headerRect.y, editWidth, headerRect.height);
            GUI.Label(editHeaderRect, "Edit", EditorStyles.boldLabel);

            // Draw Delete header if enabled
            if (allowDelete)
            {
                Rect deleteHeaderRect = new Rect(editHeaderRect.xMax, headerRect.y, deleteWidth, headerRect.height);
                GUI.Label(deleteHeaderRect, "Delete", EditorStyles.boldLabel);
            }
        }
        catch
        {
            // Fallback: just draw simple labels
            GUI.Label(headerRect, "Name");
        }
    }


    private void RenderRow(MultiColumnHeader multiColumnHeader, ScriptableObject scriptableObject)
    {
        // Skip null/destroyed objects
        if (scriptableObject == null)
            return;
            
        // Draw the row background
        var rowRect = GUILayoutUtility.GetRect(0, 20);
        
        try
        {
            // Split the row rect manually without using GetColumnRect
            float editWidth = 100;
            float deleteWidth = 100;
            float buttonsWidth = editWidth + (allowDelete ? deleteWidth : 0);
            
            // Draw the content area (Name/Custom column) - takes up remaining space after buttons
            Rect nameCellRect = new Rect(rowRect.x, rowRect.y, rowRect.width - buttonsWidth, rowRect.height);
            
            if (nameCellRect.width > 0)
            {
                EditorGUI.BeginChangeCheck();
                
                // Always render dynamic row content for all assets
                RenderDynamicRow(scriptableObject, nameCellRect);
                
                // If there were changes in the renderer, mark dirty
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(scriptableObject);
                    _isDirty = true;
                }
            }
            
            // Edit button
            Rect editCellRect = new Rect(nameCellRect.xMax, rowRect.y, editWidth, rowRect.height);
            if (GUI.Button(editCellRect, "Edit"))
            {
                EditorGUIUtility.PingObject(scriptableObject);
                Selection.activeObject = scriptableObject;
            }

            // Delete button
            if (allowDelete)
            {
                Rect deleteCellRect = new Rect(editCellRect.xMax, rowRect.y, deleteWidth, rowRect.height);
                if (GUI.Button(deleteCellRect, "Delete"))
                {
                    // Remove from the entries list
                    _sheet.entries.Remove(scriptableObject);
                    
                    // Destroy the asset itself
                    DestroyImmediate(scriptableObject, true);

                    EditorUtility.SetDirty(_sheet);
                    _isDirty = true;
                    GUIUtility.ExitGUI();
                }
            }
        }
        catch
        {
            // Fallback: just show a label (don't access destroyed object properties)
            GUI.Label(rowRect, "Item");
        }
    }

    private void RenderDynamicRow(ScriptableObject asset, Rect rowRect)
    {
        // Get all public properties dynamically, excluding Unity built-in properties
        var allProperties = asset.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var publicProperties = allProperties.Where(p => p.Name != "name" && p.Name != "hideFlags").ToArray();
        
        // Calculate column width based on number of fields (name + properties)
        int totalColumns = 1 + publicProperties.Length; // 1 for name + number of properties
        float columnWidth = rowRect.width / totalColumns;
        float currentX = rowRect.x;
        
        // Draw editable name
        Rect nameRect = new Rect(currentX, rowRect.y, columnWidth, rowRect.height);
        EditorGUI.BeginChangeCheck();
        string newName = EditorGUI.TextField(nameRect, asset.name);
        if (EditorGUI.EndChangeCheck())
        {
            asset.name = newName;
        }
        currentX += columnWidth;
        
        foreach (var prop in publicProperties)
        {
            if (prop == null || !prop.CanRead)
                continue;
            
            Rect fieldRect = new Rect(currentX, rowRect.y, columnWidth, rowRect.height);
            
            try
            {
                var value = prop.GetValue(asset);
                RenderPropertyField(asset, prop, fieldRect, value);
            }
            catch
            {
                // Skip properties we can't render
            }
            
            currentX += columnWidth;
        }
    }

    private void RenderDynamicRowHeader(System.Type assetType, Rect rowRect)
    {
        // Get all public properties dynamically, excluding Unity built-in properties
        var allProperties = assetType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var publicProperties = allProperties.Where(p => p.Name != "name" && p.Name != "hideFlags").ToArray();
        
        // Calculate column width based on number of fields (name + properties)
        int totalColumns = 1 + publicProperties.Length; // 1 for name + number of properties
        float columnWidth = rowRect.width / totalColumns;
        float currentX = rowRect.x;
        
        // Draw "Name" header
        Rect nameHeaderRect = new Rect(currentX, rowRect.y, columnWidth, rowRect.height);
        GUI.Label(nameHeaderRect, "Name", EditorStyles.boldLabel);
        currentX += columnWidth;
        
        // Draw property headers
        foreach (var prop in publicProperties)
        {
            Rect headerRect = new Rect(currentX, rowRect.y, columnWidth, rowRect.height);
            GUI.Label(headerRect, prop.Name, EditorStyles.boldLabel);
            currentX += columnWidth;
        }
    }

    private void RenderPropertyField(ScriptableObject asset, System.Reflection.PropertyInfo prop, Rect fieldRect, object value)
    {
        // Handle different property types
        if (prop.PropertyType == typeof(Color))
        {
            EditorGUI.BeginChangeCheck();
            Color newColor = EditorGUI.ColorField(fieldRect, (Color)value);
            if (EditorGUI.EndChangeCheck())
            {
                var backingField = asset.GetType().GetField($"<{prop.Name}>k__BackingField", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (backingField != null)
                {
                    backingField.SetValue(asset, newColor);
                }
            }
        }
        else if (prop.PropertyType == typeof(string))
        {
            EditorGUI.BeginChangeCheck();
            string newString = EditorGUI.TextField(fieldRect, (string)value ?? "");
            if (EditorGUI.EndChangeCheck() && prop.CanWrite)
            {
                prop.SetValue(asset, newString);
            }
        }
        else if (prop.PropertyType == typeof(float))
        {
            EditorGUI.BeginChangeCheck();
            float newFloat = EditorGUI.FloatField(fieldRect, (float)value);
            if (EditorGUI.EndChangeCheck() && prop.CanWrite)
            {
                prop.SetValue(asset, newFloat);
            }
        }
        else if (prop.PropertyType == typeof(int))
        {
            EditorGUI.BeginChangeCheck();
            int newInt = EditorGUI.IntField(fieldRect, (int)value);
            if (EditorGUI.EndChangeCheck() && prop.CanWrite)
            {
                prop.SetValue(asset, newInt);
            }
        }
        else if (typeof(UnityEngine.Object).IsAssignableFrom(prop.PropertyType))
        {
            EditorGUI.BeginChangeCheck();
            var newObj = EditorGUI.ObjectField(fieldRect, (UnityEngine.Object)value, prop.PropertyType, false);
            if (EditorGUI.EndChangeCheck() && prop.CanWrite)
            {
                prop.SetValue(asset, newObj);
            }
        }
        else if (prop.PropertyType.IsArray)
        {
            // Handle arrays - show count and type
            System.Array arrayValue = value as System.Array;
            int count = arrayValue != null ? arrayValue.Length : 0;
            string elementType = prop.PropertyType.GetElementType().Name;
            GUI.Label(fieldRect, $"{elementType}[{count}]");
        }
        else
        {
            // For other complex types, show a label with the type name
            GUI.Label(fieldRect, value != null ? value.ToString() : "null");
        }
    }
}
