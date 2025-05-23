using System.Linq;
using System.Reflection;
using ArcSheets;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class ArcSheetWindow : EditorWindow
{
    private ArcSheet _sheet;
    private Vector2 _scroll;
    private FieldInfo[] _fieldInfos;

    private MultiColumnHeader multiColumnHeader;
    private MultiColumnHeaderState multiColumnHeaderState;

    private const bool allowDelete = true;

    public static void Open(ArcSheet sheet)
    {
        var window = GetWindow<ArcSheetWindow>(sheet.name);
        window._sheet = sheet;
        window._fieldInfos = sheet.typeReference.Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
            .OrderBy(f => f.MetadataToken)
            .ToArray();
        window.Show();
    }


    private void InitColumnData()
    {
        var columns = _fieldInfos.Select(fieldInfo => new MultiColumnHeaderState.Column{ headerContent = new GUIContent(fieldInfo.Name), width = 250, autoResize = true }).ToList();
        
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
        GUILayout.EndHorizontal();

        // Slight gab between toolbar and the table
        GUILayout.Space(10); // Add slight gap

        // Initialize the columns
        InitColumnData();

        // Draw the entire header
        RenderHeader(multiColumnHeader);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        {
            // For each entry...
            foreach (var entry in _sheet.entries)
            {
                // Draw the row
                RenderRow(multiColumnHeader, entry);
            }

        }
        EditorGUILayout.EndScrollView();
    }


    private void RenderHeader(MultiColumnHeader multiColumnHeader)
    {
        Rect headerRect = GUILayoutUtility.GetRect(0, 20);
        multiColumnHeader.OnGUI(headerRect, 0);
    }


    private void RenderRow(MultiColumnHeader multiColumnHeader, ScriptableObject scriptableObject)
    {
        // Draw the row
        var rowRect = GUILayoutUtility.GetRect(0, 20);
        
        // Get a serialized object from the SO
        var serializedObject = new SerializedObject(scriptableObject);
        serializedObject.Update();

        // Get each property to render
        var properties = _fieldInfos.Select(fieldInfo => serializedObject.FindProperty(fieldInfo.Name)).ToArray();

        for(var i = 0; i < properties.Length; i++)
        {
            RenderCell(multiColumnHeader.GetColumnRect(i), rowRect, properties[i], serializedObject, scriptableObject);
        }

        if (allowDelete)
        {
            var cellRect = multiColumnHeader.GetColumnRect(_fieldInfos.Length);
            cellRect.y = rowRect.y;
            cellRect.height = rowRect.height;
            cellRect.x += 5f;
            cellRect.width -= 5f;

            if (GUI.Button(cellRect, "Delete"))
            {
                _sheet.entries.Remove(scriptableObject);

                EditorUtility.SetDirty(_sheet);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                GUIUtility.ExitGUI();
            }
        }
    }


    private void RenderCell(Rect cellRect,
                            Rect rowRect,
                            SerializedProperty property,
                            SerializedObject serializedObject,
                            ScriptableObject scriptableObject,
                            float padding = 5f)
    {
        cellRect.y = rowRect.y;
        cellRect.height = rowRect.height;

        // Add padding
        cellRect.x += padding / 2f;
        cellRect.width -= padding;

        // Render the cell (editable)
        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(cellRect, property, GUIContent.none);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(scriptableObject);
        }
    }
}