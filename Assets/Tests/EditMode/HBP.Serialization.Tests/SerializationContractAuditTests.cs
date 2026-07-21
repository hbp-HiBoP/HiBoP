using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using HBP.Core.Data;
using HBP.Data.BIDS;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class SerializationContractAuditTests
    {
        private const string ExpectedSerializedMembersSha256 = "c1e71696f7c7f1644e31d9da6aecc0b4226f86dc36d949ab62fd02cba44c80b3";
        private const int ExpectedSerializedMembersCount = 516;
        private const string ExpectedLifecycleContractsSha256 = "b75727661fe988bbd817d7b86e9dd8e018aeccd2426a98c9cc03ad45430f26f3";
        private const int ExpectedLifecycleContractsCount = 376;

        [Test]
        public void SerializedMemberManifest_MatchesApprovedContractSurface()
        {
            string[] current = DiscoverSerializedMembers().ToArray();

            AssertApprovedManifest(
                "serialized members",
                current,
                ExpectedSerializedMembersCount,
                ExpectedSerializedMembersSha256);
        }

        [Test]
        public void LifecycleContractManifest_MatchesApprovedSerializationSurface()
        {
            string[] current = DiscoverLifecycleContracts().ToArray();

            AssertApprovedManifest(
                "serialization lifecycle contracts",
                current,
                ExpectedLifecycleContractsCount,
                ExpectedLifecycleContractsSha256);
        }

        [Test]
        public void OptInJsonTypes_HaveDefaultConstructionPath()
        {
            string[] violations = DiscoverJsonTypes()
                .Where(type => GetJsonObjectMemberSerialization(type) == "OptIn")
                .Where(type => !type.IsAbstract && !type.IsInterface && !type.IsEnum)
                .Where(type => !type.IsValueType)
                .Where(type => type.GetConstructor(Type.EmptyTypes) == null)
                .Where(type => !type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Any(constructor => HasAttribute(constructor, "Newtonsoft.Json.JsonConstructorAttribute")))
                .Select(FormatType)
                .OrderBy(value => value)
                .ToArray();

            Assert.That(violations, Is.Empty,
                "Every OptIn JSON reference type must be constructible by Json.NET. " +
                "Add a default constructor, a [JsonConstructor], or document an explicit exception.");
        }

        private static IEnumerable<string> DiscoverSerializedMembers()
        {
            foreach (Type type in DiscoverJsonTypes())
            {
                string typeDescriptor = $"TYPE|{FormatType(type)}|{GetJsonObjectMemberSerialization(type)}";
                yield return typeDescriptor;

                foreach (MemberInfo member in GetSerializableMembers(type))
                {
                    string jsonName = GetJsonPropertyName(member) ?? member.Name;
                    string memberType = member switch
                    {
                        FieldInfo field => FormatType(field.FieldType),
                        PropertyInfo propertyInfo => FormatType(propertyInfo.PropertyType),
                        _ => member.MemberType.ToString()
                    };
                    yield return $"MEMBER|{FormatType(type)}|{member.MemberType}|{member.Name}|{jsonName}|{memberType}";
                }
            }
        }

        private static IEnumerable<string> DiscoverLifecycleContracts()
        {
            foreach (Type type in DiscoverJsonTypes())
            {
                foreach (MethodInfo method in GetLifecycleMethods(type))
                {
                    string callbacks = string.Join(",",
                        new[]
                        {
                            method.GetCustomAttribute<OnSerializingAttribute>() != null ? nameof(OnSerializingAttribute) : null,
                            method.GetCustomAttribute<OnSerializedAttribute>() != null ? nameof(OnSerializedAttribute) : null,
                            method.GetCustomAttribute<OnDeserializingAttribute>() != null ? nameof(OnDeserializingAttribute) : null,
                            method.GetCustomAttribute<OnDeserializedAttribute>() != null ? nameof(OnDeserializedAttribute) : null
                        }.Where(value => value != null));

                    yield return $"CALLBACK|{FormatType(type)}|{method.Name}|{callbacks}";
                }

                if (typeof(BaseData).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    foreach (string contract in new[] { nameof(BaseData.Clone), nameof(BaseData.Copy), nameof(BaseData.GenerateID), nameof(BaseData.GetAllIdentifiable) })
                    {
                        MethodInfo method = FindInstanceMethod(type, contract);
                        if (method == null) continue;

                        bool declaredHere = method.DeclaringType == type;
                        yield return $"BASEDATA|{FormatType(type)}|{contract}|declared={declaredHere}|owner={FormatType(method.DeclaringType)}";
                    }
                }
            }
        }

        private static IEnumerable<Type> DiscoverJsonTypes()
        {
            return GetProjectAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => HasAttribute(type, "Newtonsoft.Json.JsonObjectAttribute"))
                .OrderBy(FormatType);
        }

        private static IEnumerable<MemberInfo> GetSerializableMembers(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            return type.GetMembers(flags)
                .Where(member => HasAttribute(member, "Newtonsoft.Json.JsonPropertyAttribute"))
                .OrderBy(member => member.MetadataToken);
        }

        private static IEnumerable<MethodInfo> GetLifecycleMethods(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            return type.GetMethods(flags)
                .Where(method =>
                    method.GetCustomAttribute<OnSerializingAttribute>() != null ||
                    method.GetCustomAttribute<OnSerializedAttribute>() != null ||
                    method.GetCustomAttribute<OnDeserializingAttribute>() != null ||
                    method.GetCustomAttribute<OnDeserializedAttribute>() != null)
                .OrderBy(method => method.MetadataToken);
        }

        private static MethodInfo FindInstanceMethod(Type type, string name)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(method => method.Name == name && method.GetParameters().Length == ExpectedParameterCount(name));
        }

        private static int ExpectedParameterCount(string methodName)
        {
            return methodName == nameof(BaseData.Copy) ? 1 : 0;
        }

        private static Assembly[] GetProjectAssemblies()
        {
            return new[]
            {
                typeof(BaseData).Assembly,
                typeof(BIDSExportConfiguration).Assembly
            }.Distinct().ToArray();
        }

        private static string GetJsonObjectMemberSerialization(Type type)
        {
            CustomAttributeData attribute = GetAttribute(type, "Newtonsoft.Json.JsonObjectAttribute");
            if (attribute == null) return "<none>";

            if (attribute.ConstructorArguments.Count > 0)
            {
                return attribute.ConstructorArguments[0].Value?.ToString() ?? "<null>";
            }

            CustomAttributeNamedArgument namedArgument = attribute.NamedArguments
                .FirstOrDefault(argument => argument.MemberName == "MemberSerialization");
            return namedArgument.TypedValue.Value?.ToString() ?? "OptOut";
        }

        private static string GetJsonPropertyName(MemberInfo member)
        {
            CustomAttributeData attribute = GetAttribute(member, "Newtonsoft.Json.JsonPropertyAttribute");
            if (attribute == null) return null;

            if (attribute.ConstructorArguments.Count > 0)
            {
                return attribute.ConstructorArguments[0].Value as string;
            }

            CustomAttributeNamedArgument namedArgument = attribute.NamedArguments
                .FirstOrDefault(argument => argument.MemberName == "PropertyName");
            return namedArgument.TypedValue.Value as string;
        }

        private static bool HasAttribute(MemberInfo member, string fullName)
        {
            return GetAttribute(member, fullName) != null;
        }

        private static CustomAttributeData GetAttribute(MemberInfo member, string fullName)
        {
            return member.CustomAttributes.FirstOrDefault(attribute => attribute.AttributeType.FullName == fullName);
        }

        private static void AssertApprovedManifest(string label, IReadOnlyCollection<string> current, int expectedCount, string expectedSha256)
        {
            string actualSha256 = Sha256(current);
            string currentManifest = string.Join("\n", current);

            Assert.That(current.Count, Is.EqualTo(expectedCount),
                $"{label} count changed. Current count: {current.Count}. Current SHA256: {actualSha256}.\n" +
                currentManifest);
            Assert.That(actualSha256, Is.EqualTo(expectedSha256),
                $"{label} manifest changed. Current count: {current.Count}. Current SHA256: {actualSha256}.\n" +
                currentManifest);
        }

        private static string Sha256(IEnumerable<string> values)
        {
            string text = string.Join("\n", values.OrderBy(value => value));
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static string FormatType(Type type)
        {
            if (type == null) return "<null>";
            return type.FullName?.Replace("+", ".") ?? type.Name;
        }
    }
}
