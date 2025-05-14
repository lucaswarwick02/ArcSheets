using System.Collections.Generic;
using UnityEngine;
using TypeReferences;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ArcSheets
{
    [CreateAssetMenu(fileName = "New ArcSheet", menuName = "ArcSheet")]
    public class ArcSheet : ScriptableObject
    {
        [Inherits(typeof(ScriptableObject), IncludeAdditionalAssemblies = new[] { "Assembly-CSharp" })] public TypeReference typeReference;

#if UNITY_EDITOR
        void OnEnable()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.arcadian.ArcSheets/Editor/Icons/icon.png");
            EditorGUIUtility.SetIconForObject(this, icon);
        }
#endif

        /// <summary>
        /// Stores the list of objects inside the ArcSheet.
        /// </summary>
        public List<ScriptableObject> entries = new();
    }

}