using ScriptableObjectTables;
using UnityEditor;

/// <summary>
/// Asset opener for ScriptableObjectTable assets to open them in the ScriptableObjectTableWindow.
/// </summary>
public class ScriptableObjectTableAssetOpener
{
    /// <summary>
    /// Opens the ScriptableObjectTable asset in the ScriptableObjectTableWindow when double-clicked in the Unity Editor.
    /// </summary>
    /// <param name="instanceID"></param>
    /// <param name="line"></param>
    /// <returns></returns>
    [UnityEditor.Callbacks.OnOpenAsset]
    public static bool OnOpen(int instanceID, int line)
    {
        if (EditorUtility.InstanceIDToObject(instanceID) is ScriptableObjectTable table && table.typeReference.Type != null)
        {
            ScriptableObjectTableWindow.Open(table);
            return true;
        }

        return false;
    }
}
