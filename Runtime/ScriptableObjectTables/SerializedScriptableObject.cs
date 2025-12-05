using UnityEngine;

namespace ScriptableObjectTables
{
    /// <summary>
    /// Wrapper around Unity's ScriptableObject to create a unique identifier.
    /// </summary>
    [CreateAssetMenu(fileName = "New SSO", menuName = "Arcadian/Serialized Scriptable Object")]
    public class SerializedScriptableObject : ScriptableObject
    {
        [SerializeField, HideInInspector]
        private string guid = System.Guid.NewGuid().ToString();

        /// <summary>
        /// Unique identifier for this ScriptableObject instance.
        /// Gets set once when the object is created and is never shown in the inspector.
        /// </summary>
        public string GUID => guid;
    }
}