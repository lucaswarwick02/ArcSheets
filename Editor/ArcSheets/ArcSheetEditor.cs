using System;
using System.Linq;
using ArcSheets;
using UnityEditor;

[CustomEditor(typeof(ArcSheet))]
public class ArcSheetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var typeRefProp = serializedObject.FindProperty("typeReference");

        var arcSheet = (ArcSheet)target;

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(typeRefProp, true);

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();  // Get the new values

            // Grab new type, and current entries type
            var newType = arcSheet.typeReference.Type;
            Type oldType = null;
            if (arcSheet.entries.Count > 0)
            {
                oldType = arcSheet.entries[0].GetType();
            }

            bool isChanged = arcSheet.entries.Count > 0 && newType != oldType;

            if (isChanged)
            {
                // Display confirmation message as changing type will delete all stored entries
                bool confirmed = EditorUtility.DisplayDialog(
                    "Change Type?",
                    $"Changing this sheet to '{newType}' (currently '{oldType}') will delete all ({arcSheet.entries.Count()}) entries. Are you sure?",
                    "Yes",
                    "Cancel"
                );

                // Cancelled, so revert
                if (!confirmed)
                {
                    arcSheet.typeReference.Type = oldType;
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
                else
                {
                    // Confirmed, so delete
                    foreach(var entry in arcSheet.entries)
                    {
                        DestroyImmediate(entry, true);
                        AssetDatabase.SaveAssets();
                    }
                    arcSheet.entries.Clear();
                }
            }

        }

        serializedObject.ApplyModifiedProperties();
    }
}
