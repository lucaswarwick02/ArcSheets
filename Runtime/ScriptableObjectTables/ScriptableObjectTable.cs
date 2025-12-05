using System.Collections.Generic;
using UnityEngine;
using TypeReferences;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ScriptableObjectTables
{
    [CreateAssetMenu(fileName = "New Scriptable Object Table", menuName = "Scriptable Object Tables/Scriptable Object Table")]
    public class ScriptableObjectTable : ScriptableObject
    {
        // The TypeReference attribute will be handled in a custom editor to use the above list
        [Inherits(typeof(ScriptableObject), ShowAllTypes = true)]
        public TypeReference typeReference;

#if UNITY_EDITOR
        void OnEnable()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.lucaswarwick02.scriptable-object-tables/Editor/Icons/icon.png");
            EditorGUIUtility.SetIconForObject(this, icon);
        }
#endif

        /// <summary>
        /// Stores the list of objects inside the ScriptableObjectTable.
        /// </summary>
        public List<ScriptableObject> entries = new();
    }

}