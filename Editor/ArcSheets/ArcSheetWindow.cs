using System.Collections.Generic;
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

    private void RenderTableHeader()
    {
        int columnCount = _fieldInfos.Length + 1;
        float columnWidth = position.width / columnCount;

        var style = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            border = new RectOffset(1, 1, 1, 1),
            margin = new RectOffset(1, 1, 1, 1),
            padding = new RectOffset(4, 4, 2, 2),
            normal = { background = Texture2D.grayTexture, }
        };

        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField("Name", style, GUILayout.Width(columnWidth));

        foreach (var field in _fieldInfos)
            EditorGUILayout.LabelField(field.Name, style, GUILayout.Width(columnWidth));

        EditorGUILayout.EndHorizontal();
    }

    // private void RenderTableRow(ScriptableObject entry, string rowName, bool isSelected)
    // {
    //     // Setup types
    //     SerializedObject so = new(entry);
    //     so.Update();
    //     var props = _fieldInfos.Select(f => so.FindProperty(f.Name)).ToArray();
        

    //     // Start the row
    //     EditorGUILayout.BeginHorizontal("box");
    //     EditorGUILayout.LabelField(rowName);

    //     if (isSelected)
    //     {
    //         GUI.backgroundColor = Color.gray;
    //     }

    //     // For each property...
    //     foreach (var prop in props)
    //     {
    //         if (prop == null)
    //         {
    //             continue;
    //         }
    //         Debug.Log(prop.FindPropertyRelative(nameof(_sheet.typeReference.Type)));

    //         // Render 
    //         EditorGUI.BeginChangeCheck();
    //         _ = EditorGUILayout.PropertyField(prop, GUIContent.none);
    //         if (EditorGUI.EndChangeCheck())
    //         {
    //             _ = so.ApplyModifiedProperties();
    //             EditorUtility.SetDirty(entry);
    //         }
    //     }

    //     GUI.backgroundColor = Color.white;

    //     // End the row
    //     EditorGUILayout.EndHorizontal();
    // }

    private Dictionary<ScriptableObject, Dictionary<string, bool>> _expandedStates = new();

    private void RenderTableRow(ScriptableObject entry, string rowName, bool isSelected)
    {
        var so = new SerializedObject(entry);
        so.Update();

        float columnWidth = position.width / (_fieldInfos.Length + 1);

        // Start the row, add the name to begin with
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField(rowName, GUILayout.Width(columnWidth));

        if (isSelected) GUI.backgroundColor = Color.gray;


        foreach (var field in _fieldInfos)
        {
            var prop = so.FindProperty(field.Name);
            if (prop == null) continue;

            EditorGUILayout.BeginVertical(GUILayout.Width(columnWidth));

            if (!_expandedStates.TryGetValue(entry, out var fieldStates))
            {
                fieldStates = new();
                _expandedStates[entry] = fieldStates;
            }

            if (!fieldStates.ContainsKey(field.Name))
                fieldStates[field.Name] = false;

            if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
            {
                var labelName = $"{field.Name} {(prop.arraySize == 0 ? "" : $"({prop.arraySize})")}";
                fieldStates[field.Name] = EditorGUILayout.Foldout(fieldStates[field.Name], labelName);

                if (fieldStates[field.Name])
                {
                    EditorGUI.indentLevel++;

                    // Buttons at the top
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        prop.arraySize++;
                        so.ApplyModifiedProperties();
                    }
                    if (GUILayout.Button("-", GUILayout.Width(20)) && prop.arraySize > 0)
                    {
                        prop.arraySize--;
                        so.ApplyModifiedProperties();
                    }
                    EditorGUILayout.EndHorizontal();

                    // Then draw the items
                    EditorGUI.BeginChangeCheck();

                    for (int i = 0; i < prop.arraySize; i++)
                    {
                        var element = prop.GetArrayElementAtIndex(i);
                        EditorGUILayout.PropertyField(element, GUIContent.none);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(entry);
                    }

                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(prop, GUIContent.none, false);
                if (EditorGUI.EndChangeCheck())
                {
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(entry);
                }
            }

            EditorGUILayout.EndVertical();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }
}