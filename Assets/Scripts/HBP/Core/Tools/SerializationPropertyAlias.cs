using System;
using HBP.Core.Data;

namespace HBP.Core.Tools
{
    /// <summary>A read-only historical member name, scoped to its registered owner type.</summary>
    public sealed class SerializationPropertyAlias
    {
        public string SerializedProperty { get; }
        public string CurrentProperty { get; }
        public string Migration { get; }

        public SerializationPropertyAlias(string serializedProperty, string currentProperty, string migration = null)
        {
            if (string.IsNullOrWhiteSpace(serializedProperty) || string.IsNullOrWhiteSpace(currentProperty))
                throw new ArgumentException("Property aliases require historical and current JSON names.");
            SerializedProperty = serializedProperty;
            CurrentProperty = currentProperty;
            Migration = migration;
        }
    }

    /// <summary>Value transformations stay in serialization code, not in the data model.</summary>
    internal static class SerializationPropertyMigrations
    {
        internal static (Type Source, Func<object, object> Convert) Resolve(string migration, Type target)
        {
            if (string.IsNullOrEmpty(migration)) return (target, null);
            if (migration == "CCEPConfiguration" && target == typeof(CCEPConfiguration))
                return (typeof(DynamicConfiguration), UpgradeCCEPConfiguration);
            throw new InvalidOperationException($"Unknown or incompatible property migration '{migration}' for '{target}'.");
        }

        private static object UpgradeCCEPConfiguration(object value)
        {
            var configuration = new CCEPConfiguration();
            configuration.Copy(value);
            return configuration;
        }
    }
}
