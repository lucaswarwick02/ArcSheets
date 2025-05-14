using ArcSheets;
using UnityEditor;

public class ArcSheetAssetOpener
{
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
