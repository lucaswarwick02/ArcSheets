using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ScriptableObjectTables;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

/// <summary>
/// Table view for rendering ScriptableObjectTable entries with proper column handling.
/// </summary>
public class ScriptableObjectTableView
{
    private ScriptableObjectTable _table;
    private List<ScriptableObject> _entries;
    private PropertyInfo[] _displayProperties;
    
    private MultiColumnHeaderState _multiColumnHeaderState;
    private MultiColumnHeader _multiColumnHeader;
    private MultiColumnHeaderState.Column[] _columns;
    
    private readonly Color _lighterColor = Color.white * 0.3f;
    private readonly Color _darkerColor = Color.white * 0.1f;
    
    private Vector2 _scrollPosition;
    private bool _needsRebuild = true;
    
    /// <summary>Event invoked when data is modified.</summary>
    public event Action onDirty;

    private const int BUTTON_WIDTH = 60;
    private const int CELL_PADDING = 5;

    /// <summary>
    /// Initializes a new instance of the ScriptableObjectTableView.
    /// </summary>
    public ScriptableObjectTableView(ScriptableObjectTable table)
    {
        _table = table;
        _entries = new List<ScriptableObject>();
        RefreshEntries();
        RefreshDisplayProperties();
        Rebuild();
    }

    private void RefreshEntries()
    {
        _entries = new List<ScriptableObject>(_table.entries.Where(e => e != null));
    }

    private void RefreshDisplayProperties()
    {
        if (_table?.typeReference?.Type == null)
            return;

        var allProperties = _table.typeReference.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        _displayProperties = allProperties.Where(p => p.Name != "name" && p.Name != "hideFlags").ToArray();
    }

    /// <summary>
    /// Marks the table for rebuild on next draw.
    /// </summary>
    public void Rebuild()
    {
        _needsRebuild = true;
    }

    private void BuildColumns()
    {
        var columns = new List<MultiColumnHeaderState.Column>
        {
            // Name column
            new() {
                headerContent = new GUIContent("Name"),
                width = 150,
                minWidth = 100,
                autoResize = true,
                allowToggleVisibility = false,
                canSort = false,
                headerTextAlignment = TextAlignment.Left,
            }
        };

        // Property columns
        foreach (var prop in _displayProperties)
        {
            columns.Add(new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent(prop.Name),
                width = 100,
                minWidth = 80,
                autoResize = true,
                allowToggleVisibility = false,
                canSort = false,
                headerTextAlignment = TextAlignment.Left,
            });
        }

        // Spacer column to prevent misclicks
        columns.Add(new MultiColumnHeaderState.Column
        {
            headerContent = new GUIContent(""),
            width = 20,
            minWidth = 20,
            autoResize = false,
            allowToggleVisibility = false,
            canSort = false,
        });

        // Up/Down reorder buttons
        columns.Add(new MultiColumnHeaderState.Column
        {
            headerContent = new GUIContent(""),
            width = 50,
            minWidth = 50,
            autoResize = false,
            allowToggleVisibility = false,
            canSort = false,
            headerTextAlignment = TextAlignment.Center,
        });

        // Delete button column
        columns.Add(new MultiColumnHeaderState.Column
        {
            headerContent = new GUIContent("Delete"),
            width = BUTTON_WIDTH,
            minWidth = BUTTON_WIDTH,
            autoResize = false,
            allowToggleVisibility = false,
            canSort = false,
            headerTextAlignment = TextAlignment.Center,
        });

        _columns = columns.ToArray();
        _multiColumnHeaderState = new MultiColumnHeaderState(_columns);
        _multiColumnHeader = new MultiColumnHeader(_multiColumnHeaderState);
        _multiColumnHeader.visibleColumnsChanged += (header) => header.ResizeToFit();
        _multiColumnHeader.ResizeToFit();
        _needsRebuild = false;
    }

    /// <summary>
    /// Draws the table GUI with all rows and columns.
    /// </summary>
    public void DrawTableGUI()
    {
        if (_multiColumnHeader == null || _needsRebuild)
        {
            BuildColumns();
        }

        RefreshEntries();

        float rowHeight = EditorGUIUtility.singleLineHeight + 2;
        float rowWidth = _multiColumnHeaderState.widthOfAllVisibleColumns;

        // Draw header
        Rect headerRect = GUILayoutUtility.GetRect(rowWidth, rowHeight);
        _multiColumnHeader.OnGUI(headerRect, xScroll: 0.0f);

        float sumHeight = rowHeight * _entries.Count + GUI.skin.horizontalScrollbar.fixedHeight;
        float maxHeight = 400; // Use fixed height for the table scroll area

        // Draw scroll view with table rows
        Rect scrollViewRect = GUILayoutUtility.GetRect(0, rowWidth, 0, maxHeight);
        Rect viewRect = new(0, 0, rowWidth, sumHeight);

        _scrollPosition = GUI.BeginScrollView(
            position: scrollViewRect,
            scrollPosition: _scrollPosition,
            viewRect: viewRect,
            alwaysShowHorizontal: false,
            alwaysShowVertical: false
        );

        for (int row = 0; row < _entries.Count; row++)
        {
            Rect rowRect = new(0, rowHeight * row, rowWidth, rowHeight);

            // Draw alternating row background
            EditorGUI.DrawRect(rect: rowRect, color: row % 2 == 0 ? _darkerColor : _lighterColor);

            DrawRow(rowRect, _entries[row], row);
        }

        GUI.EndScrollView(handleScrollWheel: true);
    }

    private void DrawRow(Rect rowRect, ScriptableObject asset, int rowIndex)
    {
        int colIndex = 0;

        // Name column
        if (_multiColumnHeader.IsColumnVisible(colIndex))
        {
            int visibleColIndex = _multiColumnHeader.GetVisibleColumnIndex(colIndex);
            Rect cellRect = _multiColumnHeader.GetCellRect(visibleColIndex, rowRect);
            DrawNameCell(cellRect, asset);
        }
        colIndex++;

        // Property columns
        foreach (var prop in _displayProperties)
        {
            if (_multiColumnHeader.IsColumnVisible(colIndex))
            {
                int visibleColIndex = _multiColumnHeader.GetVisibleColumnIndex(colIndex);
                Rect cellRect = _multiColumnHeader.GetCellRect(visibleColIndex, rowRect);
                DrawPropertyCell(cellRect, asset, prop);
            }
            colIndex++;
        }

        // Spacer column (no action needed)
        colIndex++;

        // Up/Down reorder buttons
        if (_multiColumnHeader.IsColumnVisible(colIndex))
        {
            int visibleColIndex = _multiColumnHeader.GetVisibleColumnIndex(colIndex);
            Rect cellRect = _multiColumnHeader.GetCellRect(visibleColIndex, rowRect);
            
            // Split the cell into up and down buttons
            Rect upButtonRect = new Rect(cellRect.x, cellRect.y, cellRect.width / 2 - 1, cellRect.height);
            Rect downButtonRect = new Rect(cellRect.x + cellRect.width / 2 + 1, cellRect.y, cellRect.width / 2 - 1, cellRect.height);
            
            // Up button
            if (rowIndex > 0 && GUI.Button(upButtonRect, "↑"))
            {
                // Swap with previous
                _table.entries[rowIndex] = _table.entries[rowIndex - 1];
                _table.entries[rowIndex - 1] = asset;
                EditorUtility.SetDirty(_table);
                onDirty?.Invoke();
                Rebuild();
            }
            
            // Down button
            if (rowIndex < _entries.Count - 1 && GUI.Button(downButtonRect, "↓"))
            {
                // Swap with next
                _table.entries[rowIndex] = _table.entries[rowIndex + 1];
                _table.entries[rowIndex + 1] = asset;
                EditorUtility.SetDirty(_table);
                onDirty?.Invoke();
                Rebuild();
            }
        }
        colIndex++;

        // Delete button
        if (_multiColumnHeader.IsColumnVisible(colIndex))
        {
            int visibleColIndex = _multiColumnHeader.GetVisibleColumnIndex(colIndex);
            Rect cellRect = _multiColumnHeader.GetCellRect(visibleColIndex, rowRect);
            if (GUI.Button(cellRect, "Delete"))
            {
                _table.entries.Remove(asset);
                UnityEngine.Object.DestroyImmediate(asset, true);
                EditorUtility.SetDirty(_table);
                onDirty?.Invoke();
                Rebuild();
            }
        }

        // Handle row click for selection (on non-button areas)
        if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
        {
            // Get the delete button rect to exclude it from selection clicks
            int deleteColIndex = colIndex;
            if (_multiColumnHeader.IsColumnVisible(deleteColIndex))
            {
                int visibleColIndex = _multiColumnHeader.GetVisibleColumnIndex(deleteColIndex);
                Rect deleteButtonRect = _multiColumnHeader.GetCellRect(visibleColIndex, rowRect);
                
                // Only select if not clicking the delete button
                if (!deleteButtonRect.Contains(Event.current.mousePosition))
                {
                    EditorGUIUtility.PingObject(asset);
                    Selection.activeObject = asset;
                }
            }
        }
    }

    private void DrawNameCell(Rect rect, ScriptableObject asset)
    {
        Rect fieldRect = new Rect(rect.x + CELL_PADDING, rect.y + 1, rect.width - CELL_PADDING * 2, rect.height - 2);
        EditorGUI.BeginChangeCheck();
        string newName = EditorGUI.TextField(fieldRect, asset.name);
        if (EditorGUI.EndChangeCheck())
        {
            asset.name = newName;
            EditorUtility.SetDirty(asset);
            onDirty?.Invoke();
        }
    }

    private void DrawPropertyCell(Rect rect, ScriptableObject asset, PropertyInfo prop)
    {
        Rect fieldRect = new Rect(rect.x + CELL_PADDING, rect.y + 1, rect.width - CELL_PADDING * 2, rect.height - 2);

        try
        {
            var value = prop.GetValue(asset);

            if (prop.PropertyType == typeof(Color))
            {
                EditorGUI.BeginChangeCheck();
                Color newColor = EditorGUI.ColorField(fieldRect, (Color)value);
                if (EditorGUI.EndChangeCheck())
                {
                    var backingField = asset.GetType().GetField($"<{prop.Name}>k__BackingField",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (backingField != null)
                    {
                        backingField.SetValue(asset, newColor);
                        EditorUtility.SetDirty(asset);
                        onDirty?.Invoke();
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
                    EditorUtility.SetDirty(asset);
                    onDirty?.Invoke();
                }
            }
            else if (prop.PropertyType == typeof(float))
            {
                EditorGUI.BeginChangeCheck();
                float newFloat = EditorGUI.FloatField(fieldRect, (float)value);
                if (EditorGUI.EndChangeCheck() && prop.CanWrite)
                {
                    prop.SetValue(asset, newFloat);
                    EditorUtility.SetDirty(asset);
                    onDirty?.Invoke();
                }
            }
            else if (prop.PropertyType == typeof(int))
            {
                EditorGUI.BeginChangeCheck();
                int newInt = EditorGUI.IntField(fieldRect, (int)value);
                if (EditorGUI.EndChangeCheck() && prop.CanWrite)
                {
                    prop.SetValue(asset, newInt);
                    EditorUtility.SetDirty(asset);
                    onDirty?.Invoke();
                }
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(prop.PropertyType))
            {
                EditorGUI.BeginChangeCheck();
                var newObj = EditorGUI.ObjectField(fieldRect, (UnityEngine.Object)value, prop.PropertyType, false);
                if (EditorGUI.EndChangeCheck() && prop.CanWrite)
                {
                    prop.SetValue(asset, newObj);
                    EditorUtility.SetDirty(asset);
                    onDirty?.Invoke();
                }
            }
            else if (prop.PropertyType.IsArray)
            {
                var arrayValue = value as Array;
                int count = arrayValue?.Length ?? 0;
                string elementType = prop.PropertyType.GetElementType().Name;
                GUI.Label(fieldRect, $"{elementType}[{count}]");
            }
            else
            {
                GUI.Label(fieldRect, value != null ? value.ToString() : "null");
            }
        }
        catch
        {
            // Skip properties we can't render
        }
    }
}
