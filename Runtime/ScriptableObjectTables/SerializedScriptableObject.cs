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
            // See if we already know about this object.
            if (ObjectToString.TryGetValue(obj, out var existingId))
            {
                if (obj.guid != existingId)
                {
                    // Logging an error since this will change this object's ID.
                    Debug.LogError($"Inconsistency: {obj.name} {obj.guid} / {existingId}");
                    obj.guid = existingId;
                }

                // Found object instance, ensure StringToObject contains.
                if (StringToObject.ContainsKey(existingId))
                {
                    return;
                }

                // DB inconsistency
                Debug.LogWarning("Inconsistent database tracking.");
                StringToObject.Add(existingId, obj);

                return;
            }

            // See if this object's GUID is empty. Easy case, create.
            if (string.IsNullOrEmpty(obj.guid))
            {
                GenerateGuid(obj);

                RegisterObject(obj);
                return;
            }

            // Ensure we don't already have the GUID registered.
            // If not, then we don't know about the object, nor the GUID, so just register.
            if (!StringToObject.TryGetValue(obj.guid, out var knownObject))
            {
                // GUID not known to the DB, so just register it
                RegisterObject(obj);
                return;
            }

            // We DO know about the GUID, and it matches this object. Weird... just register it.
            if (knownObject == obj)
            {
                // DB inconsistency
                Debug.LogWarning("Inconsistent database tracking.");
                ObjectToString.Add(obj, obj.guid);
                return;
            }

            // We know about the GUID, but it isn't tied to any object. This object claims to
            // be that GUID.... okay, register it.
            if (knownObject == null)
            {
                // Object in DB got destroyed, replace with current object.
                Debug.LogWarning("Unexpected registration problem.");
                RegisterObject(obj, true);
                return;
            }

            // Otherwise:
            // 1) Object database did NOT contain this object.
            // 2) We did find a different object with the SAME identifier.
            // Thus, we have a duplicate.
            //
            // Through extensive testing, it appears the duplicated item will be updated.
            // The original item will not have its hash updated. Save games referencing that
            // hash should remain functional.
            //
            // Designers should never repurpose a checkpoint and expect it to not be
            // already unlocked or otherwise referenced in production.
            //

            // Debug.Log($"Duplicate Detected: {obj.guid}");
            GenerateGuid(obj);

            // Register this new item.
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