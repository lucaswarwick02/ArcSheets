using System;
using System.Linq;
using ScriptableObjectTables;
using UnityEditor;

/// <summary>
/// Custom editor for ScriptableObjectTable ScriptableObject to handle type changes and entry management.
/// </summary>
[CustomEditor(typeof(ScriptableObjectTable))]
public class ScriptableObjectTableEditor : UnityEditor.Editor
{
    /// <summary>
    /// Override the inspector GUI to handle type changes and entry management.
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var typeRefProp = serializedObject.FindProperty("typeReference");

        var scriptableObjectTable = (ScriptableObjectTable)target;

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(typeRefProp, true);

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();  // Get the new values

            // Grab new type, and current entries type
            var newType = scriptableObjectTable.typeReference.Type;
            Type oldType = null;
            if (scriptableObjectTable.entries.Count > 0)
            {
                oldType = scriptableObjectTable.entries[0].GetType();
            }

            bool isChanged = scriptableObjectTable.entries.Count > 0 && newType != oldType;

            if (isChanged)
            {
                // Display confirmation message as changing type will delete all stored entries
                bool confirmed = EditorUtility.DisplayDialog(
                    "Change Type?",
                    $"Changing this table to '{newType}' (currently '{oldType}') will delete all ({scriptableObjectTable.entries.Count()}) entries. Are you sure?",
                    "Yes",
                    "Cancel"
                );

                // Cancelled, so revert
                if (!confirmed)
                {
                    scriptableObjectTable.typeReference.Type = oldType;
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
                else
                {
                    // Confirmed, so delete
                    foreach(var entry in scriptableObjectTable.entries)
                    {
                        DestroyImmediate(entry, true);
                        AssetDatabase.SaveAssets();
                    }
                    scriptableObjectTable.entries.Clear();
                }
            }

        }

        serializedObject.ApplyModifiedProperties();
    }
}
