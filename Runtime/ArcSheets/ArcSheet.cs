using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArcSheets
{
    [CreateAssetMenu(fileName = "New ArcSheet", menuName = "ArcSheet")]
    public class ArcSheet : ScriptableObject
    {
        public string type;

        /// <summary>
        /// Stores the list of objects inside the ArcSheet.
        /// </summary>
        public List<ScriptableObject> entries = new();
    }

}