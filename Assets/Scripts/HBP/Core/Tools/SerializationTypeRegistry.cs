using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace HBP.Core.Tools
{
    [Preserve]
    public static class SerializationTypeRegistry
    {
        private static readonly object s_Lock = new();
        private static readonly Dictionary<string, Type> s_TypesBySerializedName = new(StringComparer.Ordinal);
        private static readonly HashSet<Type> s_RegisteredTypes = new();
        private static readonly Dictionary<Type, List<SerializationPropertyAlias>> s_PropertyAliases = new();

        static SerializationTypeRegistry()
        {
            GeneratedCoreSerializationTypes.Register();
        }

        public static void RegisterGenerated(Type type, params string[] serializedTypeNames)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (serializedTypeNames == null || serializedTypeNames.Length == 0)
            {
                throw new ArgumentException("At least one serialized type name is required.", nameof(serializedTypeNames));
            }

            lock (s_Lock)
            {
                foreach (string serializedTypeName in serializedTypeNames)
                {
                    if (string.IsNullOrWhiteSpace(serializedTypeName))
                    {
                        throw new ArgumentException("Serialized type names cannot be empty.", nameof(serializedTypeNames));
                    }

                    if (s_TypesBySerializedName.TryGetValue(serializedTypeName, out Type registeredType) && registeredType != type)
                    {
                        throw new InvalidOperationException($"Serialized type name '{serializedTypeName}' is registered for both " + $"'{registeredType.FullName}' and '{type.FullName}'.");
                    }

                    s_TypesBySerializedName[serializedTypeName] = type;
                }

                s_RegisteredTypes.Add(type);
            }
        }

        public static void RegisterGeneratedPropertyAlias(Type owner, string serializedProperty, string currentProperty, string migration = null)
        {
            var alias = new SerializationPropertyAlias(serializedProperty, currentProperty, migration);
            lock (s_Lock)
            {
                if (!s_PropertyAliases.TryGetValue(owner, out var aliases)) s_PropertyAliases.Add(owner, aliases = new());
                foreach (var existing in aliases)
                {
                    if (existing.SerializedProperty != serializedProperty) continue;
                    if (existing.CurrentProperty != currentProperty || existing.Migration != migration)
                        throw new InvalidOperationException($"Conflicting property alias '{owner}.{serializedProperty}'.");
                    return;
                }

                aliases.Add(alias);
            }
        }

        internal static SerializationPropertyAlias[] GetPropertyAliases(Type owner)
        {
            lock (s_Lock)
                return s_PropertyAliases.TryGetValue(owner, out var aliases) ? aliases.ToArray() : Array.Empty<SerializationPropertyAlias>();
        }

        public static Type Resolve(string assemblyName, string typeName)
        {
            if (TryResolve(typeName, out Type type)) return type;

            string serializedIdentity = string.IsNullOrEmpty(assemblyName) ? typeName : $"{typeName}, {assemblyName}";
            throw new JsonSerializationException($"Serialization type '{serializedIdentity}' is not present in the generated HiBoP type registry. " + "In the Unity Editor, run 'Tools/Serialization/Generate Type Registry'.");
        }

        public static bool TryResolve(string typeName, out Type type)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                type = null;
                return false;
            }

            lock (s_Lock)
            {
                return s_TypesBySerializedName.TryGetValue(typeName, out type);
            }
        }

        public static bool IsRegistered(Type type)
        {
            if (type == null) return false;
            lock (s_Lock)
            {
                return s_RegisteredTypes.Contains(type);
            }
        }

        public static void GetSerializedName(Type type, out string assemblyName, out string typeName)
        {
            if (!IsRegistered(type))
            {
                throw new JsonSerializationException($"Serialization type '{type?.FullName ?? "<null>"}' is not present in the generated HiBoP type registry. " + "In the Unity Editor, run 'Tools/Serialization/Generate Type Registry'.");
            }

            assemblyName = type.Assembly.GetName().Name;
            typeName = type.FullName;
        }
    }
}
