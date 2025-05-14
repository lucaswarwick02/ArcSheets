using System.Collections.Generic;
using UnityEngine;
using TypeReferences;

namespace ArcSheets
{
    [CreateAssetMenu(fileName = "New ArcSheet", menuName = "ArcSheet")]
    public class ArcSheet : ScriptableObject
    {
        [Inherits(typeof(ScriptableObject), IncludeAdditionalAssemblies = new[] { "Assembly-CSharp" })] public TypeReference typeReference;

        /// <summary>
        /// Stores the list of objects inside the ArcSheet.
        /// </summary>
        public List<ScriptableObject> entries = new();
    }

}