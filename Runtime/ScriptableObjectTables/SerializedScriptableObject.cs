// Reference: https://github.com/Bunny83/UUID/blob/master/UUID.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LucasWarwick02.ScriptableObjectTables
{
    /// <summary>
    /// Wrapper around Unity's ScriptableObject to create a unique identifier that persists reliably.
    /// </summary>
    [CreateAssetMenu(fileName = "New SSO", menuName = "Lucas's Unity Assets/Serialized Scriptable Object")]
    public class SerializedScriptableObject : ScriptableObject, ISerializationCallbackReceiver
    {
        private static readonly Dictionary<SerializedScriptableObject, string> ObjectToString =
            new Dictionary<SerializedScriptableObject, string>();

        private static readonly Dictionary<string, SerializedScriptableObject> StringToObject =
            new Dictionary<string, SerializedScriptableObject>();

        [SerializeField, HideInInspector]
        private string guid;

        [SerializeField, HideInInspector]
        private long createdAtTicks;

        [NonSerialized]
        private bool _internalIdWasUpdated;

        /// <summary>
        /// Unique identifier for this ScriptableObject instance.
        /// Gets set once when the object is created and persists reliably.
        /// </summary>
        public string GUID => guid;

#if UNITY_EDITOR
        private string CreatedAt => new DateTime(createdAtTicks).ToString(CultureInfo.CurrentCulture);
#endif

        protected void OnEnable()
        {
            ProcessRegistration(this);

            // If we updated the guid during serialization, save the asset.
            if (!_internalIdWasUpdated)
            {
                return;
            }

            _internalIdWasUpdated = false;

#if UNITY_EDITOR
            // Before/After Serialize methods cannot mark dirty.
            //   Without this, the change we made in registration is not saved.
            //   If something else changed on the asset, such as the Display Name
            //   then that will cause it to be marked dirty, and saved.
            //   But, extensive testing has shown that there are cases where
            //   only the UUID is updated and never saved.
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }

        protected void OnDestroy()
        {
            Debug.LogWarning($"Unexpected object destroyed. {guid}");
            UnregisterObject(this);
            guid = null;
        }

        public void OnAfterDeserialize()
        {
            ProcessRegistration(this);
        }

        public void OnBeforeSerialize()
        {
            ProcessRegistration(this);
        }

        private static void ProcessRegistration(SerializedScriptableObject obj)
        {
            // 1. Check if we already have this specific instance tracked
            if (ObjectToString.TryGetValue(obj, out var existingId))
            {
                if (obj.guid != existingId)
                {
                    Debug.LogError($"Inconsistency: {obj.name} {obj.guid} / {existingId}");
                    obj.guid = existingId;
                }

                if (StringToObject.ContainsKey(existingId)) return;

                Debug.LogWarning("Inconsistent database tracking.");
                StringToObject.Add(existingId, obj);
                return;
            }

            // 2. Handle Empty GUIDs
            if (string.IsNullOrEmpty(obj.guid))
            {
        #if UNITY_EDITOR
                GenerateGuid(obj);
                RegisterObject(obj);
        #else
                // In a build, an empty GUID is a critical data error
                Debug.LogError($"Asset '{obj.name}' is missing a GUID in the build! Lookup will fail.");
        #endif
                return;
            }

            // 3. Handle Duplicate GUIDs
            if (StringToObject.TryGetValue(obj.guid, out var knownObject))
            {
                if (knownObject == obj)
                {
                    Debug.LogWarning("Inconsistent database tracking.");
                    ObjectToString.Add(obj, obj.guid);
                    return;
                }

                if (knownObject == null)
                {
                    Debug.LogWarning("Object in DB got destroyed, replacing with current object.");
                    RegisterObject(obj, true);
                    return;
                }

                // We found a different object with the same ID
        #if UNITY_EDITOR
                // In Editor, we generate a new one to resolve the conflict (e.g., after a Ctrl+D)
                GenerateGuid(obj);
                RegisterObject(obj);
        #else
                // In a build, NEVER change the GUID. It's better to have a duplicate 
                // than to have an ID that doesn't match your saved data.
                Debug.LogWarning($"Duplicate GUID detected in build: {obj.guid} on {obj.name}. This usually means a prefab/asset was duplicated without the GUID being cleared in Editor.");
                RegisterObject(obj, true); 
        #endif
                return;
            }

            // 4. Fresh registration
            RegisterObject(obj);
        }

        private static void RegisterObject(SerializedScriptableObject aID, bool replace = false)
        {
            if (replace)
            {
                StringToObject[aID.guid] = aID;
            }
            else
            {
                StringToObject.Add(aID.guid, aID);
            }

            ObjectToString.Add(aID, aID.guid);
        }

        private static void UnregisterObject(SerializedScriptableObject aID)
        {
            StringToObject.Remove(aID.guid);
            ObjectToString.Remove(aID);
        }

        private static void GenerateGuid(SerializedScriptableObject obj)
        {
            obj.guid = Guid.NewGuid().ToString();
            obj.createdAtTicks = DateTime.Now.Ticks;

            obj._internalIdWasUpdated = true;

            // Debug.Log($"Created GUID: {obj.guid}");
        }
    }
}