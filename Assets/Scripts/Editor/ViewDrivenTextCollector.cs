using System.Collections.Generic;
using AlienFarms.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace AlienFarms.Editor
{
    /// <summary>
    /// Finds TMP texts referenced by AlienFarms components (views write them from code, so they must never get a
    /// static <see cref="LocalizedText"/>). LocalizedText itself is ignored.
    /// </summary>
    public static class ViewDrivenTextCollector
    {
        private const string ProjectNamespace = "AlienFarms";

        /// <summary>Adds to <paramref name="result"/> every TMP_Text referenced by an AlienFarms component under <paramref name="root"/>.</summary>
        public static void Collect(GameObject root, HashSet<TMP_Text> result)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!IsProjectComponent(behaviour))
                {
                    continue;
                }

                var serialized = new SerializedObject(behaviour);
                SerializedProperty property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference
                        && property.objectReferenceValue is TMP_Text referenced)
                    {
                        result.Add(referenced);
                    }
                }
            }
        }

        private static bool IsProjectComponent(MonoBehaviour behaviour)
        {
            if (behaviour == null || behaviour is LocalizedText)
            {
                return false;
            }

            string ns = behaviour.GetType().Namespace;
            return ns != null && (ns == ProjectNamespace || ns.StartsWith(ProjectNamespace + "."));
        }
    }
}
