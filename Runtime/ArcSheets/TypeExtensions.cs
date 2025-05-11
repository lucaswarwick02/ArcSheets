using System;
using System.Linq;
using UnityEngine;

namespace ArcSheets
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Given a string, attempt to get the actual Type.
        /// </summary>
        /// <param name="stringType">Full Name of the Type</param>
        /// <returns>Casted Type</returns>
        /// <exception cref="Exception">Invalid Full Name given</exception>
        public static Type ToType(string stringType)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == stringType);

            return type ?? throw new Exception($"Cannot convert `{stringType}` to a Type.");
        }

        /// <summary>
        /// Construct a list of all user defined Types.
        /// </summary>
        /// <returns>List of Types</returns>
        public static Type[] ListUserTypes() => AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName.StartsWith("Assembly-CSharp"))
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    typeof(ScriptableObject).IsAssignableFrom(t) &&
                    t != typeof(ScriptableObject) &&
                    !t.IsAbstract)
                .OrderBy(t => t.FullName)
                .ToArray();
    }
}