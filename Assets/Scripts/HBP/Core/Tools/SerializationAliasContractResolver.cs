using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HBP.Core.Tools
{
    /// <summary>Read historical names through the same contracts as project and transfer JSON.</summary>
    public class SerializationAliasContractResolver : DefaultContractResolver
    {
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            JsonObjectContract contract = base.CreateObjectContract(objectType);
            ApplyAliases(contract, SerializationTypeRegistry.GetPropertyAliases(objectType));
            return contract;
        }

        // The editor generator validates the declarations against the ordinary JSON contract.
        public static void ValidateAliases(Type type, IEnumerable<SerializationPropertyAlias> aliases)
        {
            var contract = new DefaultContractResolver().ResolveContract(type) as JsonObjectContract;
            if (contract == null) throw new InvalidOperationException($"Property alias owner '{type}' is not a JSON object.");
            ApplyAliases(contract, aliases);
        }

        private static void ApplyAliases(JsonObjectContract contract, IEnumerable<SerializationPropertyAlias> aliases)
        {
            var targets = new Dictionary<JsonProperty, List<JsonProperty>>();
            foreach (var alias in aliases)
            {
                JsonProperty target = contract.Properties.GetClosestMatchProperty(alias.CurrentProperty);
                if (target == null || target.PropertyName != alias.CurrentProperty || target.Ignored || !target.Writable || !target.Readable)
                    throw new InvalidOperationException($"Unknown or unwritable alias target '{contract.UnderlyingType}.{alias.CurrentProperty}'.");
                if (contract.Properties.Any(property => string.Equals(property.PropertyName, alias.SerializedProperty, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"Ambiguous property alias '{contract.UnderlyingType}.{alias.SerializedProperty}'.");
                var migration = SerializationPropertyMigrations.Resolve(alias.Migration, target.PropertyType);
                var historical = new JsonProperty
                {
                    PropertyName = alias.SerializedProperty, DeclaringType = contract.UnderlyingType,
                    PropertyType = migration.Source, Readable = false, Writable = true,
                    ValueProvider = new AliasValueProvider(target.ValueProvider, migration.Convert),
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                    NullValueHandling = migration.Convert == null ? target.NullValueHandling : NullValueHandling.Ignore,
                    Converter = migration.Convert == null ? target.Converter : null,
                    TypeNameHandling = target.TypeNameHandling,
                    ItemConverter = target.ItemConverter, ItemTypeNameHandling = target.ItemTypeNameHandling,
                    ShouldSerialize = _ => false
                };
                contract.Properties.Add(historical);
                if (!targets.TryGetValue(target, out var names)) targets.Add(target, names = new List<JsonProperty> { target });
                names.Add(historical);
            }

            if (targets.Count == 0) return;
            // Contracts are cached/shared. Per-object weak state avoids mixing simultaneous reads.
            var seen = new ConditionalWeakTable<object, HashSet<string>>();
            contract.OnDeserializingCallbacks.Add((value, _) => seen.Remove(value));
            contract.OnDeserializedCallbacks.Add((value, _) => seen.Remove(value));
            foreach (var group in targets)
            foreach (var property in group.Value)
            {
                Predicate<object> previous = property.ShouldDeserialize;
                property.ShouldDeserialize = value =>
                {
                    if (previous != null && !previous(value)) return false;
                    if (!seen.GetValue(value, _ => new HashSet<string>(StringComparer.Ordinal)).Add(group.Key.PropertyName))
                        throw new JsonSerializationException($"Multiple JSON names supplied for '{contract.UnderlyingType}.{group.Key.PropertyName}'.");
                    return true;
                };
            }
        }

        private sealed class AliasValueProvider : IValueProvider
        {
            private readonly IValueProvider target;
            private readonly Func<object, object> convert;

            internal AliasValueProvider(IValueProvider target, Func<object, object> convert)
            {
                this.target = target;
                this.convert = convert;
            }

            public object GetValue(object value) => null;
            public void SetValue(object value, object member) => target.SetValue(value, convert == null ? member : convert(member));
        }
    }
}
