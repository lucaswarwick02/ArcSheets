using ArcSheets;
using UnityEditor;

/// <summary>
/// Asset opener for ArcSheet assets to open them in the ArcSheetWindow.
/// </summary>
public class ArcSheetAssetOpener
{
    /// <summary>
    /// Opens the ArcSheet asset in the ArcSheetWindow when double-clicked in the Unity Editor.
    /// </summary>
    /// <param name="instanceID"></param>
    /// <param name="line"></param>
    /// <returns></returns>
    [UnityEditor.Callbacks.OnOpenAsset]
    public static bool OnOpen(int instanceID, int line)
    {
        if (EditorUtility.InstanceIDToObject(instanceID) is ArcSheet sheet && sheet.typeReference.Type != null)
        {
            ArcSheetWindow.Open(sheet);
            return true;
        }

        return false;
    }
}
